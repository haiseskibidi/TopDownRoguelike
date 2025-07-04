using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using GunVault.GameEngine;
using GunVault.Models.PlayerClasses;

namespace GunVault.Models
{
    public class Player
    {
        private const double PLAYER_SPEED = 5.0;
        private const double PLAYER_RADIUS = 18.75; // Radius for circular collider (was 25.0)
        private const double PLAYER_ROTATION_SPEED = 10.0;
        private const double BODY_ROTATION_SPEED = 5.0; // Slower, smoother rotation for the body
        private const double ACCELERATION = 0.1; // How quickly the player gains speed
        private const double FRICTION = 0.95;    // How quickly the player slows down (lower = more friction)
        private const double BODY_SIZE = 32.0;
        
        public struct BulletParams
        {
            public double StartX, StartY, Angle, Speed, Damage, ExplosionRadius, ExplosionDamage, BulletSize;
            public string BulletSpriteName;
            public bool IsExplosive;
            public bool CanRicochet;
            public int MaxRicochets;
        }
        
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Health { get; private set; }
        public double MaxHealth { get; private set; }
        public double HealthRegen { get; private set; }
        public double BulletSpeedModifier { get; private set; }
        public double BulletDamageModifier { get; private set; }
        public double ReloadSpeedModifier { get; private set; }
        public double MovementSpeed { get; private set; }
        public double BulletSpreadModifier { get; private set; }
        public double FireRateModifier { get; private set; }
        
        // New properties for Body and Gun
        public UIElement BodyShape { get; private set; }
        public UIElement GunShape { get; private set; }

        public Weapon CurrentWeapon { get; private set; }
        public CircleCollider Collider { get; private set; } // Changed to CircleCollider
        public Rectangle ColliderVisual { get; private set; }
        
        public bool MovingUp { get; set; }
        public bool MovingDown { get; set; }
        public bool MovingLeft { get; set; }
        public bool MovingRight { get; set; }
        public double VelocityX { get; private set; }
        public double VelocityY { get; private set; }
        
        private double _gunAngle = 0;
        private double _targetGunAngle = 0;
        private double _bodyAngle = 0;
        private double _targetBodyAngle = 0; // Target angle for smooth body rotation
        private readonly SpriteManager? _spriteManager;
        private bool _isDying = false;
        private bool _assaultUseLeftBarrel = true; // For Assault class alternating fire
        
        // Class System
        public PlayerClass ChosenClass { get; private set; }
        
        // Уровни характеристик игрока
        public int HealthRegenUpgradeLevel { get; private set; } = 0;
        public int MaxHealthUpgradeLevel { get; private set; } = 0;
        public int BulletSpeedUpgradeLevel { get; private set; } = 0;
        public int BulletDamageUpgradeLevel { get; private set; } = 0;
        public int ReloadSpeedUpgradeLevel { get; private set; } = 0;
        public int MovementSpeedUpgradeLevel { get; private set; } = 0;
        public int FireRateUpgradeLevel { get; private set; } = 0;
        public const int MAX_UPGRADE_LEVEL = 10;
        
        private static readonly Dictionary<string, Tuple<double, double>> SpriteProportions = new Dictionary<string, Tuple<double, double>>
        {
            { "player_pistol", new Tuple<double, double>(46.0, 32.0) },
            { "player_shotgun", new Tuple<double, double>(60.0, 32.0) },
            { "player_assaultrifle", new Tuple<double, double>(60.0, 32.0) },
        };
        
        public Player(double startX, double startY, SpriteManager? spriteManager = null)
        {
            X = startX;
            Y = startY;
            MaxHealth = 100;
            Health = MaxHealth;
            HealthRegen = 0;
            BulletSpeedModifier = 1.0;
            BulletDamageModifier = 1.0;
            ReloadSpeedModifier = 1.0;
            MovementSpeed = 3.0;
            BulletSpreadModifier = 1.0;
            FireRateModifier = 1.0;
            _spriteManager = spriteManager;
            _isDying = false;
            
            InitializeCollider();
            
            if (spriteManager != null)
            {
                BodyShape = spriteManager.CreateSpriteImage("body", BODY_SIZE, BODY_SIZE);
                UpdateWeaponSprite();
            }
            else
            {
                // Fallback shapes if sprite manager fails
                BodyShape = new Ellipse { Width = BODY_SIZE, Height = BODY_SIZE, Fill = Brushes.DarkGray };
                GunShape = new Rectangle { Width = (ChosenClass?.GunWidth ?? 25.0), Height = (ChosenClass?.GunHeight ?? 10.0), Fill = Brushes.LightGray };
            }
            
            InitializeWeapon();
            
            UpdatePosition();
            
            Console.WriteLine($"Игрок создан с фиксированным размером спрайта: {BODY_SIZE}x{BODY_SIZE}");
        }
        
        private void InitializeCollider()
        {
            if (Collider == null)
            {
                Collider = new CircleCollider(X, Y, PLAYER_RADIUS);
                // ColliderVisual = new Rectangle
                // {
                //     Width = PLAYER_RADIUS * 2,
                //     Height = PLAYER_RADIUS * 2,
                //     Stroke = Brushes.Cyan,
                //     StrokeThickness = 1,
                //     RadiusX = PLAYER_RADIUS,
                //     RadiusY = PLAYER_RADIUS
                // };
            }
            else
            {
                Collider.UpdatePosition(X, Y);
            }
        }

        public void AddToCanvas(Canvas canvas)
        {
            if (!canvas.Children.Contains(BodyShape))
                canvas.Children.Add(BodyShape);
            if (!canvas.Children.Contains(GunShape))
                canvas.Children.Add(GunShape);
        }

        // public void AddColliderVisualToCanvas(Canvas canvas)
        // {
        //     if (!canvas.Children.Contains(ColliderVisual))
        //     {
        //         canvas.Children.Add(ColliderVisual);
        //         Panel.SetZIndex(ColliderVisual, 999); // Make sure it's visible
        //     }
        // }

        public void RemoveFromCanvas(Canvas canvas)
        {
            canvas.Children.Remove(BodyShape);
            canvas.Children.Remove(GunShape);
        }
        
        private Tuple<double, double> CalculateSpriteSize(string spriteName)
        {
            if (SpriteProportions.TryGetValue(spriteName, out var originalProportions))
            {
                double originalWidth = originalProportions.Item1;
                double originalHeight = originalProportions.Item2;
                double aspectRatio = originalWidth / originalHeight;
                double adjustedWidth = BODY_SIZE * aspectRatio;
                
                Console.WriteLine($"Стандартизированный размер для спрайта {spriteName}: {adjustedWidth:F1}x{BODY_SIZE:F1} " +
                                  $"(исходные пропорции: {originalWidth}x{originalHeight})");
                
                return new Tuple<double, double>(adjustedWidth, BODY_SIZE);
            }
            
            Console.WriteLine($"Используем стандартный размер для неизвестного спрайта {spriteName}: {BODY_SIZE}x{BODY_SIZE}");
            return new Tuple<double, double>(BODY_SIZE, BODY_SIZE);
        }

        public void ChangeWeapon(Weapon newWeapon)
        {
            CurrentWeapon = newWeapon;
            // Later, this will change the GunShape's sprite
            // For now, it just changes the weapon data
        }
        
        public void UpdatePosition()
        {
            // Update body position
            Canvas.SetLeft(BodyShape, X - BODY_SIZE / 2);
            Canvas.SetTop(BodyShape, Y - BODY_SIZE / 2);
            BodyShape.RenderTransformOrigin = new Point(0.5, 0.5);
            BodyShape.RenderTransform = new RotateTransform(_bodyAngle * 180 / Math.PI);

            // Update gun position
            double gunWidth = ChosenClass?.GunWidth ?? 25.0;
            double gunHeight = ChosenClass?.GunHeight ?? 10.0;
            Canvas.SetLeft(GunShape, X - gunWidth / 4); // Position relative to body center
            Canvas.SetTop(GunShape, Y - gunHeight / 2);
            GunShape.RenderTransformOrigin = new Point(0.25, 0.5); // Rotate around a point near the back
            GunShape.RenderTransform = new RotateTransform(_gunAngle * 180 / Math.PI);
            
            // Update collider position
            Collider.UpdatePosition(X, Y);

            // if (ColliderVisual != null)
            // {
            //     Canvas.SetLeft(ColliderVisual, X - PLAYER_RADIUS);
            //     Canvas.SetTop(ColliderVisual, Y - PLAYER_RADIUS);
            // }
        }
        
        private double NormalizeAngle(double angle)
        {
            while (angle > Math.PI)
                angle -= 2 * Math.PI;
            while (angle < -Math.PI)
                angle += 2 * Math.PI;
            return angle;
        }
        
        private void UpdateBodyRotation(double deltaTime)
        {
            // Smoothly rotate the BODY towards the target angle (movement direction)
            double angleDifference = NormalizeAngle(_targetBodyAngle - _bodyAngle);
            double maxRotation = BODY_ROTATION_SPEED * deltaTime;
            
            if (Math.Abs(angleDifference) <= maxRotation)
            {
                _bodyAngle = _targetBodyAngle;
            }
            else
            {
                _bodyAngle += Math.Sign(angleDifference) * maxRotation;
            }
        }
        
        private void UpdateRotation(double deltaTime)
        {
            // Smoothly rotate the GUN towards the target angle (mouse cursor)
            double angleDifference = NormalizeAngle(_targetGunAngle - _gunAngle);
            double maxRotation = PLAYER_ROTATION_SPEED * deltaTime;

            if (Math.Abs(angleDifference) <= maxRotation)
            {
                _gunAngle = _targetGunAngle;
            }
            else
            {
                _gunAngle += Math.Sign(angleDifference) * maxRotation;
            }
        }
        
        public void Move(Func<RectCollider, bool> isAreaWalkableCallback)
        {
            double targetVelX = 0;
            double targetVelY = 0;

            if (MovingLeft) targetVelX -= 1;
            if (MovingRight) targetVelX += 1;
            if (MovingUp) targetVelY -= 1;
            if (MovingDown) targetVelY += 1;

            double length = Math.Sqrt(targetVelX * targetVelX + targetVelY * targetVelY);
            if (length > 0)
            {
                // Normalize and set target velocity based on input
                targetVelX = (targetVelX / length) * MovementSpeed;
                targetVelY = (targetVelY / length) * MovementSpeed;
            }

            // Apply acceleration towards the target velocity
            VelocityX += (targetVelX - VelocityX) * ACCELERATION;
            VelocityY += (targetVelY - VelocityY) * ACCELERATION;
            
            // Set target body angle based on current velocity, if moving
            if (Math.Abs(VelocityX) > 0.1 || Math.Abs(VelocityY) > 0.1)
            {
                _targetBodyAngle = Math.Atan2(VelocityY, VelocityX);
            }

            // Apply friction when there is no input to slow down
            if (Math.Abs(targetVelX) < 0.01 && Math.Abs(targetVelY) < 0.01)
            {
                VelocityX *= FRICTION;
                VelocityY *= FRICTION;
            }

            double nextX = X + VelocityX;
            double nextY = Y + VelocityY;

            // Create a temporary collider to test new positions
            var testCollider = new RectCollider(0, 0, Collider.Radius * 2, Collider.Radius * 2);

            // Check X-axis movement
            testCollider.UpdatePosition(nextX - Collider.Radius, Y - Collider.Radius);
            if (isAreaWalkableCallback(testCollider))
            {
                X = nextX; // Apply X movement
            }

            // Check Y-axis movement, using the (potentially updated) X position for the check
            testCollider.UpdatePosition(X - Collider.Radius, nextY - Collider.Radius);
            if (isAreaWalkableCallback(testCollider))
            {
                Y = nextY; // Apply Y movement
            }
        }
        
        /// <summary>
        /// Ограничивает игрока размерами экрана
        /// </summary>
        public void ConstrainToScreen(double screenWidth, double screenHeight)
        {
            X = Math.Max(PLAYER_RADIUS, Math.Min(X, screenWidth - PLAYER_RADIUS));
            Y = Math.Max(PLAYER_RADIUS, Math.Min(Y, screenHeight - PLAYER_RADIUS));
            
            UpdatePosition();
        }
        
        /// <summary>
        /// Ограничивает игрока размерами игрового мира
        /// </summary>
        public void ConstrainToWorldBounds(double minX, double minY, double maxX, double maxY)
        {
            // Используем радиус игрока для правильного ограничения
            X = Math.Max(minX + PLAYER_RADIUS, Math.Min(X, maxX - PLAYER_RADIUS));
            Y = Math.Max(minY + PLAYER_RADIUS, Math.Min(Y, maxY - PLAYER_RADIUS));
            
            UpdatePosition();
        }
        
        public void TakeDamage(double damage)
        {
            Health -= damage;
            if (Health < 0)
            {
                Health = 0;
            }
        }
        
        public void Heal(double amount)
        {
            if (_isDying) return; // Нельзя лечиться в процессе смерти
            Health += amount;
            if (Health > MaxHealth)
            {
                Health = MaxHealth;
            }
        }
        
        public List<BulletParams> Shoot(Point targetPoint)
        {
            if (CurrentWeapon.CanShoot())
            {
                CurrentWeapon.Shoot();
                var bulletParamsList = new List<BulletParams>();

                // Muzzle position should be at the tip of the gun barrel
                double muzzleDistance = (ChosenClass?.GunWidth ?? 25.0) * 0.75; // A bit out from the gun's rotation origin
                double muzzleX = X + Math.Cos(_gunAngle) * muzzleDistance;
                double muzzleY = Y + Math.Sin(_gunAngle) * muzzleDistance;

                // Assault-specific alternating fire logic
                if (ChosenClass?.ClassType == PlayerClassType.Assault)
                {
                    double barrelOffset = 5; // Distance of each barrel from the center
                    double angleOffset = _assaultUseLeftBarrel ? -Math.PI / 2 : Math.PI / 2;
                    
                    double perpendicularAngle = _gunAngle + angleOffset;

                    muzzleX += Math.Cos(perpendicularAngle) * barrelOffset;
                    muzzleY += Math.Sin(perpendicularAngle) * barrelOffset;

                    _assaultUseLeftBarrel = !_assaultUseLeftBarrel; // Switch barrel for next shot
                }

                for (int i = 0; i < CurrentWeapon.BulletsPerShot; i++)
                {
                    double spreadAngle = _gunAngle + (new Random().NextDouble() - 0.5) * (CurrentWeapon.Spread * BulletSpreadModifier);

                    var bulletParams = new BulletParams
                    {
                        StartX = muzzleX,
                        StartY = muzzleY,
                        Angle = spreadAngle,
                        Speed = CurrentWeapon.BulletSpeed * BulletSpeedModifier,
                        Damage = CurrentWeapon.Damage * BulletDamageModifier,
                        IsExplosive = CurrentWeapon.IsExplosive,
                        ExplosionRadius = CurrentWeapon.ExplosionRadius,
                        ExplosionDamage = CurrentWeapon.Damage * CurrentWeapon.ExplosionDamageMultiplier,
                        BulletSize = CurrentWeapon.BulletSize, // Use weapon-specific size
                        BulletSpriteName = ChosenClass?.BulletSpriteName ?? "",
                        CanRicochet = ChosenClass?.CanRicochet ?? false,
                        MaxRicochets = ChosenClass?.MaxRicochets ?? 0
                    };
                    
                    bulletParamsList.Add(bulletParams);
                }
                return bulletParamsList;
            }
            return null;
        }
        
        public void UpdateWeapon(double deltaTime, Point targetPoint)
        {
            CurrentWeapon.Update(deltaTime);
            // Set the target angle for the GUN
            _targetGunAngle = Math.Atan2(targetPoint.Y - Y, targetPoint.X - X);
            UpdateRotation(deltaTime);
            UpdateBodyRotation(deltaTime); // Update body rotation here as well
        }
        
        public void StartReload()
        {
            CurrentWeapon.StartReload(ReloadSpeedModifier);
        }
        
        public WeaponType GetWeaponType()
        {
            return CurrentWeapon.Type;
        }
        
        public string GetWeaponName()
        {
            return CurrentWeapon.Name;
        }
        
        public string GetAmmoInfo()
        {
            return CurrentWeapon.GetAmmoInfo();
        }
        
        public Weapon GetCurrentWeapon()
        {
            return CurrentWeapon;
        }

        public void UpgradeHealthRegen() { HealthRegen += 0.5; HealthRegenUpgradeLevel = Math.Min(MAX_UPGRADE_LEVEL, HealthRegenUpgradeLevel + 1); }
        public void UpgradeMaxHealth() { MaxHealth += 20; Health += 20; MaxHealthUpgradeLevel = Math.Min(MAX_UPGRADE_LEVEL, MaxHealthUpgradeLevel + 1); }
        public void UpgradeBulletSpeed() { BulletSpeedModifier += 0.1; BulletSpeedUpgradeLevel = Math.Min(MAX_UPGRADE_LEVEL, BulletSpeedUpgradeLevel + 1); }
        public void UpgradeBulletDamage() { BulletDamageModifier += 0.1; BulletDamageUpgradeLevel = Math.Min(MAX_UPGRADE_LEVEL, BulletDamageUpgradeLevel + 1); }
        public void UpgradeReloadSpeed() { ReloadSpeedModifier += 0.1; ReloadSpeedUpgradeLevel = Math.Min(MAX_UPGRADE_LEVEL, ReloadSpeedUpgradeLevel + 1); CurrentWeapon?.UpdateReloadSpeed(ReloadSpeedModifier); }
        public void UpgradeMovementSpeed() { MovementSpeed += 0.12; MovementSpeedUpgradeLevel = Math.Min(MAX_UPGRADE_LEVEL, MovementSpeedUpgradeLevel + 1); }
        public void UpgradeFireRate() { FireRateModifier += 0.075; FireRateUpgradeLevel = Math.Min(MAX_UPGRADE_LEVEL, FireRateUpgradeLevel + 1); CurrentWeapon?.UpdateFireRate(FireRateModifier); }
        public void UpgradeBulletSpread(double amount) { BulletSpreadModifier += amount; if (BulletSpreadModifier < 1.0) BulletSpreadModifier = 1.0; Console.WriteLine($"Модификатор разброса пуль изменен на {amount:F2}, текущее значение: {BulletSpreadModifier:F2}"); }

        private SpriteManager GetSpriteManager()
        {
            var mainWindow = Application.Current.MainWindow as GunVault.MainWindow;
            if (mainWindow != null)
            {
                var field = mainWindow.GetType().GetField("_spriteManager", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    return field.GetValue(mainWindow) as SpriteManager;
                }
            }
            return null;
        }
        
        public void ModifyBulletSpeed(double amount)
        {
            BulletSpeedModifier += amount;
            if (BulletSpeedModifier < 0.1) BulletSpeedModifier = 0.1;
            Console.WriteLine($"Модификатор скорости пули изменен на {amount:F2}, текущее значение: {BulletSpeedModifier:F2}");
        }
        
        public void ModifyBulletDamage(double amount)
        {
            BulletDamageModifier += amount;
            if (BulletDamageModifier < 0.1) BulletDamageModifier = 0.1;
            Console.WriteLine($"Модификатор урона пули изменен на {amount:F2}, текущее значение: {BulletDamageModifier:F2}");
        }
        
        public void ModifyBulletSpread(double amount)
        {
            BulletSpreadModifier += amount;
            // No minimum check, spread can be high.
            Console.WriteLine($"Модификатор разброса пуль изменен на {amount:F2}, текущее значение: {BulletSpreadModifier:F2}");
        }
        
        public void ModifyReloadSpeed(double amount)
        {
            ReloadSpeedModifier += amount;
            if (ReloadSpeedModifier < 0.1) ReloadSpeedModifier = 0.1;
            CurrentWeapon?.UpdateReloadSpeed(ReloadSpeedModifier);
            Console.WriteLine($"Модификатор скорости перезарядки изменен на {amount:F2}, текущее значение: {ReloadSpeedModifier:F2}");
        }
        
        public void ModifyMovementSpeed(double amount)
        {
            Console.WriteLine($"ModifyMovementSpeed вызван. Текущая скорость: {MovementSpeed:F2}, Изменение: {amount:F2}");
            MovementSpeed += amount;
            if (MovementSpeed < 1.0) MovementSpeed = 1.0; // Prevent player from stopping
            Console.WriteLine($"Скорость движения изменена на {amount:F2}, текущее значение: {MovementSpeed:F2}");
        }
        
        public void ModifyHealthRegen(double amount)
        {
            HealthRegen += amount;
            if (HealthRegen < 0) HealthRegen = 0;
            Console.WriteLine($"Регенерация здоровья изменена на {amount:F2}, текущее значение: {HealthRegen:F2}");
        }
        
        public void UpgradeMaxHealth(double amount)
        {
            double oldMaxHealth = MaxHealth;
            MaxHealth += amount;
            
            // Увеличиваем текущее здоровье пропорционально
            double healthPercentage = Health / oldMaxHealth;
            Health = MaxHealth * healthPercentage;
            
            Console.WriteLine($"Максимальное здоровье увеличено на {amount:F0}, текущее значение: {MaxHealth:F0}");
        }
        
        public void ReduceMaxHealth(double amount)
        {
            Console.WriteLine($"ReduceMaxHealth вызван. Текущее макс. здоровье: {MaxHealth:F0}, Уменьшение: {amount:F0}");
            double oldMaxHealth = MaxHealth;
            MaxHealth -= amount;
            if (MaxHealth < 50) MaxHealth = 50;
            
            // Уменьшаем текущее здоровье, если оно превышает максимальное
            if (Health > MaxHealth)
            {
                Health = MaxHealth;
            }
            
            Console.WriteLine($"Максимальное здоровье уменьшено на {amount:F0}, текущее значение: {MaxHealth:F0}");
        }

        public void Kill()
        {
            _isDying = true;
            Health = 0;
            Console.WriteLine("Player has been killed via suicide action.");
        }

        public void SetPlayerClass(PlayerClass playerClass)
        {
            if (ChosenClass == null)
            {
                ChosenClass = playerClass;
                Console.WriteLine($"Класс игрока установлен на: {playerClass.Name}");
                ChosenClass.ApplyPassiveBonuses(this);
                UpdateWeaponSprite();
                InitializeWeapon(); // Re-initialize weapon with class stats
            }
        }

        public void SetPosition(double newX, double newY)
        {
            X = newX;
            Y = newY;
            UpdatePosition();
            Console.WriteLine($"Player position set to: ({X:F1}, {Y:F1})");
        }

        private void UpdateWeaponSprite()
        {
            if (_spriteManager == null) return;

            string spriteName = "guns/defolt"; // Default sprite

            if (ChosenClass != null)
            {
                spriteName = ChosenClass.GunSpriteName;
            }

            var existingGun = GunShape;
            double gunWidth = ChosenClass?.GunWidth ?? 25.0;
            double gunHeight = ChosenClass?.GunHeight ?? 10.0;
            GunShape = _spriteManager.CreateSpriteImage(spriteName, gunWidth, gunHeight);

            // If the gun is already on a canvas, replace it
            if (existingGun is FrameworkElement oldGunElement && oldGunElement.Parent is Canvas canvas)
            {
                canvas.Children.Remove(oldGunElement);
                canvas.Children.Add(GunShape);
                UpdatePosition(); // Re-apply transforms
            }
        }

        private void InitializeWeapon()
        {
            // Default pistol values
            string name = "Пистолет";
            WeaponType type = WeaponType.Pistol;
            double damage = 10;
            double fireRate = 2;
            double range = 500;
            double bulletSpeed = 175;
            int maxAmmo = 12;
            double reloadTime = 1.5;
            double spread = 0.05;
            int bulletsPerShot = 1;
            string bulletSpriteName = "bullet_pistol";
            double bulletSize = 6.0; // Default bullet size

            // If a class is chosen, override the stats
            if (ChosenClass != null)
            {
                name = ChosenClass.Name;
                fireRate = ChosenClass.FireRate;
                damage = ChosenClass.Damage;
                bulletSpeed = ChosenClass.BulletSpeed;
                spread = ChosenClass.Spread;
                bulletSize = ChosenClass.BulletSize;
                bulletSpriteName = ChosenClass.BulletSpriteName;
            }

            CurrentWeapon = new Weapon(
                name: name,
                type: type,
                damage: damage,
                fireRate: fireRate,
                range: range,
                bulletSpeed: bulletSpeed,
                maxAmmo: maxAmmo,
                reloadTime: reloadTime,
                spread: spread,
                bulletsPerShot: bulletsPerShot,
                bulletSpriteName: bulletSpriteName,
                bulletSize: bulletSize
            );
            
            CurrentWeapon.UpdateFireRate(FireRateModifier);
            CurrentWeapon.UpdateReloadSpeed(ReloadSpeedModifier);
        }
    }
} 