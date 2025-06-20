using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using GunVault.GameEngine;

namespace GunVault.Models
{
    public class Player
    {
        private const double PLAYER_SPEED = 5.0;
        private const double PLAYER_RADIUS = 15.0;
        private const double PLAYER_ROTATION_SPEED = 10.0;
        private const double FIXED_SPRITE_WIDTH = 60.0;
        private const double FIXED_SPRITE_HEIGHT = 40.0;
        private const double ROTATION_POINT_OFFSET = 0.25;
        private const double BASE_SPRITE_WIDTH = 46.0;
        private const double BASE_SPRITE_HEIGHT = 32.0;
        private const double TARGET_SPRITE_HEIGHT = PLAYER_RADIUS * 2.5;
        
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Health { get; private set; }
        public double MaxHealth { get; private set; }
        public double HealthRegen { get; private set; }
        public double BulletSpeedModifier { get; private set; }
        public double BulletDamageModifier { get; private set; }
        public double ReloadSpeedModifier { get; private set; }
        public double MovementSpeed { get; private set; }
        public UIElement PlayerShape { get; private set; }
        public Weapon CurrentWeapon { get; private set; }
        public RectCollider Collider { get; private set; }
        public Rectangle ColliderVisual { get; private set; }
        
        public bool MovingUp { get; set; }
        public bool MovingDown { get; set; }
        public bool MovingLeft { get; set; }
        public bool MovingRight { get; set; }
        public double VelocityX { get; private set; }
        public double VelocityY { get; private set; }
        
        private double _currentAngle = 0;
        private double _targetAngle = 0;
        
        // Уровни характеристик игрока
        public int HealthRegenUpgradeLevel { get; private set; } = 0;
        public int MaxHealthUpgradeLevel { get; private set; } = 0;
        public int BulletSpeedUpgradeLevel { get; private set; } = 0;
        public int BulletDamageUpgradeLevel { get; private set; } = 0;
        public int ReloadSpeedUpgradeLevel { get; private set; } = 0;
        public int MovementSpeedUpgradeLevel { get; private set; } = 0;
        public const int MAX_UPGRADE_LEVEL = 10;
        
        private static readonly Dictionary<string, Tuple<double, double>> SpriteProportions = new Dictionary<string, Tuple<double, double>>
        {
            { "player_pistol", new Tuple<double, double>(46.0, 32.0) },
            { "player_shotgun", new Tuple<double, double>(60.0, 32.0) },
            { "player_assaultrifle", new Tuple<double, double>(60.0, 32.0) },
        };
        
        public Player(double startX, double startY, SpriteManager spriteManager = null)
        {
            X = startX;
            Y = startY;
            MaxHealth = 100;
            Health = MaxHealth;
            HealthRegen = 0;
            BulletSpeedModifier = 1.0;
            BulletDamageModifier = 1.0;
            ReloadSpeedModifier = 1.0;
            MovementSpeed = 5.0;
            
            InitializeCollider();
            
            if (spriteManager != null)
            {
                var spriteSizes = CalculateSpriteSize("player_pistol");
                PlayerShape = spriteManager.CreateSpriteImage("player_pistol", spriteSizes.Item1, spriteSizes.Item2);
                
                if (!(PlayerShape is Image))
                {
                    Console.WriteLine("Не удалось загрузить спрайт игрока, использую запасную форму");
                    PlayerShape = CreateFallbackShape();
                }
            }
            else
            {
                PlayerShape = CreateFallbackShape();
            }
            
            CurrentWeapon = WeaponFactory.CreateWeapon(WeaponType.Pistol);
            
            UpdatePosition();
            
            Console.WriteLine($"Игрок создан с фиксированным размером спрайта: {FIXED_SPRITE_WIDTH}x{FIXED_SPRITE_HEIGHT}");
        }
        
        private void InitializeCollider()
        {
            double colliderWidth = FIXED_SPRITE_WIDTH * 0.55;
            double colliderHeight = FIXED_SPRITE_HEIGHT * 0.7;
            double offsetX = -colliderWidth / 2 - 15;
            double offsetY = -colliderHeight / 2;
            
            if (Collider == null)
            {
                Collider = new RectCollider(X + offsetX, Y + offsetY, colliderWidth, colliderHeight);
                ColliderVisual = new Rectangle
                {
                    Width = colliderWidth,
                    Height = colliderHeight,
                    Stroke = Brushes.Cyan,
                    StrokeThickness = 3,
                    Fill = new SolidColorBrush(Color.FromArgb(80, 0, 255, 255)),
                    StrokeDashArray = new DoubleCollection { 2, 2 }
                };
                
                Console.WriteLine($"Создан коллайдер размером: {colliderWidth}x{colliderHeight}");
            }
            else
            {
                Collider.UpdatePosition(X + offsetX, Y + offsetY);
                Collider.Width = colliderWidth;
                Collider.Height = colliderHeight;
            }
            
            if (ColliderVisual != null)
            {
                Canvas.SetLeft(ColliderVisual, Collider.X);
                Canvas.SetTop(ColliderVisual, Collider.Y);
                ColliderVisual.Width = Collider.Width;
                ColliderVisual.Height = Collider.Height;
            }
        }

        private void UpdateColliderPosition(bool isFlipped)
        {
            InitializeCollider();
            Console.WriteLine($"Позиция игрока: ({X:F1}, {Y:F1}), позиция коллайдера: ({Collider.X:F1}, {Collider.Y:F1})");
            Console.WriteLine($"Фиксированный размер коллайдера: {Collider.Width:F1}x{Collider.Height:F1}");
        }
        
        private Tuple<double, double> CalculateSpriteSize(string spriteName)
        {
            if (SpriteProportions.TryGetValue(spriteName, out var originalProportions))
            {
                double originalWidth = originalProportions.Item1;
                double originalHeight = originalProportions.Item2;
                double aspectRatio = originalWidth / originalHeight;
                double adjustedWidth = FIXED_SPRITE_HEIGHT * aspectRatio;
                
                Console.WriteLine($"Стандартизированный размер для спрайта {spriteName}: {adjustedWidth:F1}x{FIXED_SPRITE_HEIGHT:F1} " +
                                  $"(исходные пропорции: {originalWidth}x{originalHeight})");
                
                return new Tuple<double, double>(adjustedWidth, FIXED_SPRITE_HEIGHT);
            }
            
            Console.WriteLine($"Используем стандартный размер для неизвестного спрайта {spriteName}: {FIXED_SPRITE_WIDTH}x{FIXED_SPRITE_HEIGHT}");
            return new Tuple<double, double>(FIXED_SPRITE_WIDTH, FIXED_SPRITE_HEIGHT);
        }

        public void AddWeaponToCanvas(Canvas canvas)
        {
        }
        
        public void ChangeWeapon(Weapon newWeapon, Canvas canvas)
        {
            CurrentWeapon = newWeapon;
            Console.WriteLine($"Оружие изменено на {newWeapon.Name}");
            
            try
            {
                var mainWindow = Application.Current.MainWindow as GunVault.MainWindow;
                SpriteManager spriteManager = null;
                
                if (mainWindow != null)
                {
                    var field = mainWindow.GetType().GetField("_spriteManager", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (field != null)
                    {
                        spriteManager = field.GetValue(mainWindow) as SpriteManager;
                    }
                }
                
                UpdatePlayerSprite(spriteManager, canvas);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении спрайта игрока: {ex.Message}");
            }
        }
        
        public void UpdatePosition()
        {
            if (PlayerShape is Image image)
            {
                double actualWidth = image.Width;
                double actualHeight = image.Height;
                
                Canvas.SetLeft(PlayerShape, X - actualWidth / 2);
                Canvas.SetTop(PlayerShape, Y - actualHeight / 2);
                
                double rotationPointX = actualWidth * ROTATION_POINT_OFFSET;
                double rotationPointY = actualHeight / 2;
                
                var rotateTransform = new RotateTransform(_currentAngle * 180 / Math.PI, rotationPointX, rotationPointY);
                
                bool isFlipped = Math.Abs(NormalizeAngle(_currentAngle)) > Math.PI / 2;
                
                if (isFlipped)
                {
                    var scaleTransform = new ScaleTransform(1, -1, rotationPointX, rotationPointY);
                    TransformGroup transformGroup = new TransformGroup();
                    transformGroup.Children.Add(scaleTransform);
                    transformGroup.Children.Add(rotateTransform);
                    
                    PlayerShape.RenderTransform = transformGroup;
                }
                else
                {
                    PlayerShape.RenderTransform = rotateTransform;
                }
                
                UpdateColliderPosition(isFlipped);
            }
            else
            {
                Canvas.SetLeft(PlayerShape, X - PLAYER_RADIUS);
                Canvas.SetTop(PlayerShape, Y - PLAYER_RADIUS);
                UpdateColliderPosition(false);
            }
        }
        
        private double NormalizeAngle(double angle)
        {
            while (angle > Math.PI)
                angle -= 2 * Math.PI;
            while (angle < -Math.PI)
                angle += 2 * Math.PI;
            return angle;
        }
        
        private void UpdateRotation(double deltaTime)
        {
            double angleDifference = NormalizeAngle(_targetAngle - _currentAngle);
            double maxRotation = PLAYER_ROTATION_SPEED * deltaTime;
            
            if (Math.Abs(angleDifference) <= maxRotation)
            {
                _currentAngle = _targetAngle;
            }
            else
            {
                double sign = Math.Sign(angleDifference);
                _currentAngle += sign * maxRotation;
                _currentAngle = NormalizeAngle(_currentAngle);
            }
            
            UpdatePosition();
        }
        
        public void Move()
        {
            double dx = 0;
            double dy = 0;
            
            if (MovingUp) dy -= MovementSpeed;
            if (MovingDown) dy += MovementSpeed;
            if (MovingLeft) dx -= MovementSpeed;
            if (MovingRight) dx += MovementSpeed;
            
            VelocityX = dx;
            VelocityY = dy;
            
            if (dx != 0 || dy != 0)
            {
                if (dx != 0 && dy != 0)
                {
                    double length = Math.Sqrt(dx * dx + dy * dy);
                    dx = dx / length * MovementSpeed;
                    dy = dy / length * MovementSpeed;
                    
                    VelocityX = dx;
                    VelocityY = dy;
                }
                
                MoveWithSlidingCollisions(dx, dy);
                
                UpdatePosition();
            }
        }
        
        /// <summary>
        /// Продвинутое перемещение с обработкой скользящих столкновений
        /// </summary>
        private void MoveWithSlidingCollisions(double dx, double dy)
        {
            // Получаем доступ к LevelGenerator через GameManager
            var gameManager = GetGameManager();
            if (gameManager == null) 
            {
                // Если нет GameManager, просто перемещаем напрямую
                X += dx;
                Y += dy;
                return;
            }
            
            // Константа для микро-перемещений (шаг итерации)
            const double microStep = 0.5;
            
            // Шаг 1: Сначала проверим, можно ли двигаться напрямую
            if (TryMove(dx, dy, gameManager))
            {
                // Можем двигаться напрямую без коллизий
                return;
            }
            
            // Шаг 2: Если нет, пробуем скользящие движения по отдельным осям
            
            // Пытаемся двигаться по горизонтали
            bool movedHorizontally = false;
            if (dx != 0 && TryMove(dx, 0, gameManager))
            {
                movedHorizontally = true;
            }
            
            // Пытаемся двигаться по вертикали
            bool movedVertically = false;
            if (dy != 0 && TryMove(0, dy, gameManager))
            {
                movedVertically = true;
            }
            
            // Шаг 3: Если ни одно из движений не удалось, пробуем микро-движения
            if (!movedHorizontally && !movedVertically && (Math.Abs(dx) > microStep || Math.Abs(dy) > microStep))
            {
                // Разбиваем движение на более мелкие шаги
                double stepRatio = microStep / Math.Max(Math.Abs(dx), Math.Abs(dy));
                double microDx = dx * stepRatio;
                double microDy = dy * stepRatio;
                
                // Рекурсивно вызываем с уменьшенным шагом
                MoveWithSlidingCollisions(microDx, microDy);
                
                // После микрошага пытаемся сделать оставшееся перемещение
                MoveWithSlidingCollisions(dx - microDx, dy - microDy);
            }
        }
        
        /// <summary>
        /// Пытается переместить игрока с проверкой коллизий
        /// </summary>
        private bool TryMove(double dx, double dy, GameManager gameManager)
        {
            // Сохраняем текущие координаты
            double oldX = X;
            double oldY = Y;
            
            // Временно перемещаем игрока и его коллайдер в новую позицию
            X += dx;
            Y += dy;
            
            // Обновляем позицию коллайдера
            UpdateColliderPosition(Math.Abs(NormalizeAngle(_currentAngle)) > Math.PI / 2);
            
            // Проверяем коллизии
            bool canMove = gameManager.IsAreaWalkable(Collider);
            
            if (!canMove)
            {
                // Возвращаем игрока и коллайдер в исходную позицию если есть коллизия
                X = oldX;
                Y = oldY;
                UpdateColliderPosition(Math.Abs(NormalizeAngle(_currentAngle)) > Math.PI / 2);
            }
            
            return canMove;
        }
        
        /// <summary>
        /// Получает GameManager из MainWindow
        /// </summary>
        private GameManager GetGameManager()
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as GunVault.MainWindow;
                if (mainWindow == null) return null;
                
                var gameManagerField = mainWindow.GetType().GetField("_gameManager", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (gameManagerField == null) return null;
                
                return gameManagerField.GetValue(mainWindow) as GameManager;
            }
            catch
            {
                return null;
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
        }
        
        public void Heal(double amount)
        {
            Health += amount;
            if (Health > MaxHealth)
            {
                Health = MaxHealth;
            }
        }
        
        public List<Bullet> Shoot(Point targetPoint)
        {
            if (CurrentWeapon.CanShoot())
            {
                CurrentWeapon.Shoot();
                var bullets = new List<Bullet>();

                double angle = Math.Atan2(targetPoint.Y - Y, targetPoint.X - X);

                // This is a simplified muzzle position calculation.
                // A more robust solution would be needed for complex sprites.
                double muzzleOffsetX = 30; 
                double muzzleX = X + Math.Cos(angle) * muzzleOffsetX;
                double muzzleY = Y + Math.Sin(angle) * muzzleOffsetX;

                var spriteManager = GetSpriteManager();

                for (int i = 0; i < CurrentWeapon.BulletsPerShot; i++)
                {
                    double spreadAngle = angle + (new Random().NextDouble() - 0.5) * CurrentWeapon.Spread;

                    var bullet = new Bullet(
                        muzzleX,
                        muzzleY,
                        spreadAngle,
                        CurrentWeapon.BulletSpeed * BulletSpeedModifier,
                        CurrentWeapon.Damage * BulletDamageModifier,
                        CurrentWeapon.BulletSpriteName,
                        spriteManager
                    );
                    
                    // Устанавливаем свойства взрывной пули для ракетницы
                    if (CurrentWeapon.Type == WeaponType.RocketLauncher)
                    {
                        bullet.IsExplosive = true;
                        bullet.ExplosionRadius = 100;
                        bullet.ExplosionDamage = CurrentWeapon.Damage * 0.85 * BulletDamageModifier;
                    }
                    
                    bullets.Add(bullet);
                }
                return bullets;
            }
            return null;
        }
        
        public LaserBeam ShootLaser(Point targetPoint)
        {
            if (CurrentWeapon.CanShoot())
            {
                CurrentWeapon.Shoot();

                double angle = Math.Atan2(targetPoint.Y - Y, targetPoint.X - X);
                
                double muzzleOffsetX = 30;
                double muzzleX = X + Math.Cos(angle) * muzzleOffsetX;
                double muzzleY = Y + Math.Sin(angle) * muzzleOffsetX;

                var laser = new LaserBeam(
                    muzzleX,
                    muzzleY,
                    angle,
                    CurrentWeapon.Damage * BulletDamageModifier,
                    CurrentWeapon.Range
                );
                return laser;
            }
            return null;
        }
        
        public void UpdateWeapon(double deltaTime, Point targetPoint)
        {
            CurrentWeapon.Update(deltaTime);
            _targetAngle = Math.Atan2(targetPoint.Y - Y, targetPoint.X - X);
            UpdateRotation(deltaTime);
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

        private void UpdatePlayerSprite(SpriteManager spriteManager, Canvas parentCanvas)
        {
            if (spriteManager == null || CurrentWeapon == null)
                return;
            
            string spriteName;
            
            switch (CurrentWeapon.Type)
            {
                case WeaponType.Pistol:
                    spriteName = "player_pistol";
                    break;
                case WeaponType.Shotgun:
                    spriteName = "player_shotgun";
                    break;
                case WeaponType.AssaultRifle:
                    spriteName = "player_assaultrifle";
                    break;
                case WeaponType.Sniper:
                    spriteName = "player_pistol";
                    break;
                case WeaponType.MachineGun:
                    spriteName = "player_pistol";
                    break;
                case WeaponType.RocketLauncher:
                    spriteName = "player_pistol";
                    break;
                case WeaponType.Laser:
                    spriteName = "player_pistol";
                    break;
                default:
                    spriteName = "player_pistol";
                    break;
            }
            
            if (parentCanvas != null)
            {
                parentCanvas.Children.Remove(PlayerShape);
                
                Console.WriteLine($"Обновляю спрайт игрока для оружия {CurrentWeapon.Name}, использую спрайт {spriteName}");
                
                var spriteSizes = CalculateSpriteSize(spriteName);
                PlayerShape = spriteManager.CreateSpriteImage(spriteName, spriteSizes.Item1, spriteSizes.Item2);
                
                if (PlayerShape is Image image)
                {
                    // Размер коллайдера не меняем, он теперь фиксированный
                    Console.WriteLine($"Спрайт загружен, размер: {image.Width}x{image.Height}, коллайдер сохраняет фиксированный размер");
                }
                else
                {
                    Console.WriteLine("Не удалось загрузить спрайт игрока, использую запасную форму");
                    PlayerShape = CreateFallbackShape();
                }
                
                parentCanvas.Children.Add(PlayerShape);
                UpdatePosition();
            }
        }

        // Метод для добавления визуализации коллайдера на канвас
        public void AddColliderVisualToCanvas(Canvas canvas)
        {
            // Оставляем метод пустым, чтобы полностью отключить визуализацию коллайдера
            // Даже не добавляем коллайдер на канвас
            // Это необходимо для работы с существующими вызовами этого метода из других частей кода
        }
        
        // Метод для скрытия/отображения визуализации коллайдера
        public void ToggleColliderVisibility(bool isVisible)
        {
            // Оставляем метод пустым, поскольку коллайдеры больше не отображаются
            // Это сохранит совместимость с существующими вызовами
        }

        // Вспомогательный метод для создания формы по умолчанию
        private UIElement CreateFallbackShape()
        {
            return new Ellipse
            {
                Width = FIXED_SPRITE_WIDTH,
                Height = FIXED_SPRITE_HEIGHT,
                Fill = Brushes.Blue,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
        }

        public void UpgradeHealthRegen() { HealthRegen += 0.5; HealthRegenUpgradeLevel = Math.Min(MAX_UPGRADE_LEVEL, HealthRegenUpgradeLevel + 1); }
        public void UpgradeMaxHealth() { MaxHealth += 20; Health += 20; MaxHealthUpgradeLevel = Math.Min(MAX_UPGRADE_LEVEL, MaxHealthUpgradeLevel + 1); }
        public void UpgradeBulletSpeed() { BulletSpeedModifier += 0.1; BulletSpeedUpgradeLevel = Math.Min(MAX_UPGRADE_LEVEL, BulletSpeedUpgradeLevel + 1); }
        public void UpgradeBulletDamage() { BulletDamageModifier += 0.1; BulletDamageUpgradeLevel = Math.Min(MAX_UPGRADE_LEVEL, BulletDamageUpgradeLevel + 1); }
        public void UpgradeReloadSpeed() { ReloadSpeedModifier += 0.1; ReloadSpeedUpgradeLevel = Math.Min(MAX_UPGRADE_LEVEL, ReloadSpeedUpgradeLevel + 1); }
        public void UpgradeMovementSpeed() { MovementSpeed += 0.5; MovementSpeedUpgradeLevel = Math.Min(MAX_UPGRADE_LEVEL, MovementSpeedUpgradeLevel + 1); }

        private SpriteManager GetSpriteManager()
        {
            var mainWindow = Application.Current.MainWindow as GunVault.MainWindow;
            if (mainWindow == null) return null;
            
            var spriteManagerField = mainWindow.GetType().GetField("_spriteManager", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (spriteManagerField == null) return null;
            
            return spriteManagerField.GetValue(mainWindow) as SpriteManager;
        }

        // Методы для временных бонусов
        public void ModifyBulletSpeed(double amount)
        {
            BulletSpeedModifier += amount;
            if (BulletSpeedModifier < 1.0) BulletSpeedModifier = 1.0;
            Console.WriteLine($"Модификатор скорости пули изменен на {amount:F2}, текущее значение: {BulletSpeedModifier:F2}");
        }
        
        public void ModifyBulletDamage(double amount)
        {
            BulletDamageModifier += amount;
            if (BulletDamageModifier < 1.0) BulletDamageModifier = 1.0;
            Console.WriteLine($"Модификатор урона пули изменен на {amount:F2}, текущее значение: {BulletDamageModifier:F2}");
        }
        
        public void ModifyReloadSpeed(double amount)
        {
            ReloadSpeedModifier += amount;
            if (ReloadSpeedModifier < 1.0) ReloadSpeedModifier = 1.0;
            if (CurrentWeapon != null)
            {
                CurrentWeapon.UpdateReloadSpeed(ReloadSpeedModifier);
            }
            Console.WriteLine($"Модификатор скорости перезарядки изменен на {amount:F2}, текущее значение: {ReloadSpeedModifier:F2}");
        }
        
        public void ModifyMovementSpeed(double amount)
        {
            MovementSpeed += amount;
            if (MovementSpeed < PLAYER_SPEED) MovementSpeed = PLAYER_SPEED;
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
            double oldMaxHealth = MaxHealth;
            MaxHealth -= amount;
            if (MaxHealth < 100) MaxHealth = 100;
            
            // Уменьшаем текущее здоровье, если оно превышает максимальное
            if (Health > MaxHealth)
            {
                Health = MaxHealth;
            }
            
            Console.WriteLine($"Максимальное здоровье уменьшено на {amount:F0}, текущее значение: {MaxHealth:F0}");
        }
    }
} 