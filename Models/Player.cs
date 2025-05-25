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
        
        // Определяем фиксированные размеры для всех спрайтов игрока
        private const double FIXED_SPRITE_WIDTH = 60.0;
        private const double FIXED_SPRITE_HEIGHT = 40.0;
        
        // Константа для смещения точки вращения спрайта (в процентах от ширины)
        // 0.0 - крайняя левая точка, 0.5 - центр, 1.0 - крайняя правая точка
        private const double ROTATION_POINT_OFFSET = 0.25;
        
        // Оставляем эти константы, но они будут использоваться только для базовой формы
        private const double BASE_SPRITE_WIDTH = 46.0;
        private const double BASE_SPRITE_HEIGHT = 32.0;
        private const double TARGET_SPRITE_HEIGHT = PLAYER_RADIUS * 2.5;
        
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Health { get; private set; }
        public double MaxHealth { get; private set; }
        public UIElement PlayerShape { get; private set; }
        public Weapon CurrentWeapon { get; private set; }
        public RectCollider Collider { get; private set; } // Изменено с CircleCollider на RectCollider
        public Rectangle ColliderVisual { get; private set; } // Визуализация коллайдера для отладки
        
        public bool MovingUp { get; set; }
        public bool MovingDown { get; set; }
        public bool MovingLeft { get; set; }
        public bool MovingRight { get; set; }
        
        private double _currentAngle = 0;
        private double _targetAngle = 0;
        
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
            
            // Инициализируем коллайдер
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
        
        /// <summary>
        /// Инициализирует коллайдер игрока с фиксированным размером
        /// </summary>
        private void InitializeCollider()
        {
            // Используем фиксированный размер коллайдера
            double colliderWidth = FIXED_SPRITE_WIDTH * 0.55; // Уменьшаем размер коллайдера на 30% от размера спрайта
            double colliderHeight = FIXED_SPRITE_HEIGHT * 0.7;
            
            // Смещение для центрирования коллайдера относительно позиции игрока
            double offsetX = -colliderWidth / 2 - 15;
            double offsetY = -colliderHeight / 2;
            
            if (Collider == null)
            {
                // Создаем коллайдер при первой инициализации
                Collider = new RectCollider(X + offsetX, Y + offsetY, colliderWidth, colliderHeight);
                
                // Создаем визуализацию коллайдера для отладки
                ColliderVisual = new Rectangle
                {
                    Width = colliderWidth,
                    Height = colliderHeight,
                    Stroke = Brushes.Cyan,
                    StrokeThickness = 3,
                    Fill = new SolidColorBrush(Color.FromArgb(80, 0, 255, 255)),
                    StrokeDashArray = new DoubleCollection { 2, 2 } // Пунктирная линия для отличия от других коллайдеров
                };
                
                Console.WriteLine($"Создан коллайдер размером: {colliderWidth}x{colliderHeight}");
            }
            else
            {
                // Обновляем размеры и позицию существующего коллайдера
                Collider.UpdatePosition(X + offsetX, Y + offsetY);
                Collider.Width = colliderWidth;
                Collider.Height = colliderHeight;
            }
            
            // Обновляем визуализацию коллайдера
            if (ColliderVisual != null)
            {
                Canvas.SetLeft(ColliderVisual, Collider.X);
                Canvas.SetTop(ColliderVisual, Collider.Y);
                ColliderVisual.Width = Collider.Width;
                ColliderVisual.Height = Collider.Height;
            }
        }

        /// <summary>
        /// Обновляет позицию коллайдера с учетом поворота спрайта
        /// </summary>
        private void UpdateColliderPosition(bool isFlipped)
        {
            // Используем общий метод для инициализации/обновления коллайдера
            InitializeCollider();
            
            // Для отладки
            Console.WriteLine($"Позиция игрока: ({X:F1}, {Y:F1}), позиция коллайдера: ({Collider.X:F1}, {Collider.Y:F1})");
            Console.WriteLine($"Фиксированный размер коллайдера: {Collider.Width:F1}x{Collider.Height:F1}");
        }
        
        private Tuple<double, double> CalculateSpriteSize(string spriteName)
        {
            // Всегда возвращаем фиксированные размеры спрайта, сохраняя при этом пропорции
            if (SpriteProportions.TryGetValue(spriteName, out var originalProportions))
            {
                double originalWidth = originalProportions.Item1;
                double originalHeight = originalProportions.Item2;
                
                // Сохраняем соотношение сторон, но используем фиксированную высоту
                double aspectRatio = originalWidth / originalHeight;
                double adjustedWidth = FIXED_SPRITE_HEIGHT * aspectRatio;
                
                Console.WriteLine($"Стандартизированный размер для спрайта {spriteName}: {adjustedWidth:F1}x{FIXED_SPRITE_HEIGHT:F1} " +
                                  $"(исходные пропорции: {originalWidth}x{originalHeight})");
                
                return new Tuple<double, double>(adjustedWidth, FIXED_SPRITE_HEIGHT);
            }
            
            // Для неизвестных спрайтов используем фиксированные размеры
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
                
                // Вычисляем смещенную точку вращения (1/4 ширины от левого края)
                double rotationPointX = actualWidth * ROTATION_POINT_OFFSET;
                double rotationPointY = actualHeight / 2; // Вертикально центрируем
                
                var rotateTransform = new RotateTransform(_currentAngle * 180 / Math.PI, rotationPointX, rotationPointY);
                
                // Определяем, когда спрайт отражается по вертикали
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
                
                // Обновляем коллайдер с учетом поворота и возможного отражения
                UpdateColliderPosition(isFlipped);
            }
            else
            {
                Canvas.SetLeft(PlayerShape, X - PLAYER_RADIUS);
                Canvas.SetTop(PlayerShape, Y - PLAYER_RADIUS);
                
                // Для не-Image формы используем более простое смещение,
                // но все равно вызываем UpdateColliderPosition для консистентности
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
            
            if (MovingUp)
                dy -= PLAYER_SPEED;
            if (MovingDown)
                dy += PLAYER_SPEED;
            if (MovingLeft)
                dx -= PLAYER_SPEED;
            if (MovingRight)
                dx += PLAYER_SPEED;
            
            if (dx != 0 || dy != 0)
            {
                // Нормализуем движение по диагонали
                if (dx != 0 && dy != 0)
                {
                    double length = Math.Sqrt(dx * dx + dy * dy);
                    dx = dx / length * PLAYER_SPEED;
                    dy = dy / length * PLAYER_SPEED;
                }
                
                // Реализуем продвинутую проверку с коллизиями и скользящими столкновениями
                MoveWithSlidingCollisions(dx, dy);
                
                // Обновляем визуальную позицию
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
            Health = Math.Max(0, Health - damage);
        }
        
        public void Heal(double amount)
        {
            Health = Math.Min(MaxHealth, Health + amount);
        }
        
        public List<Bullet> Shoot(Point targetPoint)
        {
            if (CurrentWeapon.IsLaser)
            {
                return null;
            }
            
            if (CurrentWeapon.CanFire())
            {
                var muzzleParams = WeaponMuzzleConfig.GetMuzzleParams(CurrentWeapon.Type);
                
                double spriteWidth = PLAYER_RADIUS * 2;
                double spriteHeight = PLAYER_RADIUS * 2;
                
                if (PlayerShape is Image image)
                {
                    spriteWidth = image.Width;
                    spriteHeight = image.Height;
                }
                
                double muzzleDistance = WeaponMuzzleConfig.GetMuzzleDistance(CurrentWeapon.Type, spriteHeight / 2.5);
                
                double offsetY = muzzleParams.OffsetY;
                bool isFlipped = Math.Abs(NormalizeAngle(_currentAngle)) > Math.PI / 2;
                if (isFlipped)
                {
                    offsetY = -offsetY;
                }
                
                double offsetXRotated = muzzleParams.OffsetX * Math.Cos(_currentAngle) - offsetY * Math.Sin(_currentAngle);
                double offsetYRotated = offsetY * Math.Cos(_currentAngle) + muzzleParams.OffsetX * Math.Sin(_currentAngle);
                
                double muzzleX = X + Math.Cos(_currentAngle) * muzzleDistance + offsetXRotated;
                double muzzleY = Y + Math.Sin(_currentAngle) * muzzleDistance + offsetYRotated;
                
                string flipped = isFlipped ? "да" : "нет";
                Console.WriteLine($"Выстрел из {CurrentWeapon.Name}, угол: {_currentAngle * 180 / Math.PI:F1}°, отражение: {flipped}");
                Console.WriteLine($"Смещения: дистанция={muzzleDistance:F1}, Y_исходный={muzzleParams.OffsetY:F1}, Y_итоговый={offsetY:F1}");
                Console.WriteLine($"Позиция игрока: ({X:F1}, {Y:F1}), позиция дула: ({muzzleX:F1}, {muzzleY:F1})");
                
                return CurrentWeapon.Fire(muzzleX, muzzleY, targetPoint.X, targetPoint.Y);
            }
            
            return null;
        }
        
        public LaserBeam ShootLaser(Point targetPoint)
        {
            if (!CurrentWeapon.IsLaser || !CurrentWeapon.CanFire())
            {
                return null;
            }
            
            var muzzleParams = WeaponMuzzleConfig.GetMuzzleParams(CurrentWeapon.Type);
            
            double spriteWidth = PLAYER_RADIUS * 2;
            double spriteHeight = PLAYER_RADIUS * 2;
            
            if (PlayerShape is Image image)
            {
                spriteWidth = image.Width;
                spriteHeight = image.Height;
            }
            
            double muzzleDistance = WeaponMuzzleConfig.GetMuzzleDistance(CurrentWeapon.Type, spriteHeight / 2.5);
            
            double offsetY = muzzleParams.OffsetY;
            bool isFlipped = Math.Abs(NormalizeAngle(_currentAngle)) > Math.PI / 2;
            if (isFlipped)
            {
                offsetY = -offsetY;
            }
            
            double offsetXRotated = muzzleParams.OffsetX * Math.Cos(_currentAngle) - offsetY * Math.Sin(_currentAngle);
            double offsetYRotated = offsetY * Math.Cos(_currentAngle) + muzzleParams.OffsetX * Math.Sin(_currentAngle);
            
            double muzzleX = X + Math.Cos(_currentAngle) * muzzleDistance + offsetXRotated;
            double muzzleY = Y + Math.Sin(_currentAngle) * muzzleDistance + offsetYRotated;
            
            string flipped = isFlipped ? "да" : "нет";
            Console.WriteLine($"Лазерный выстрел, угол: {_currentAngle * 180 / Math.PI:F1}°, отражение: {flipped}");
            Console.WriteLine($"Смещения: дистанция={muzzleDistance:F1}, Y_исходный={muzzleParams.OffsetY:F1}, Y_итоговый={offsetY:F1}");
            Console.WriteLine($"Позиция игрока: ({X:F1}, {Y:F1}), позиция дула: ({muzzleX:F1}, {muzzleY:F1})");
            
            return CurrentWeapon.FireLaser(muzzleX, muzzleY, targetPoint.X, targetPoint.Y);
        }
        
        public void UpdateWeapon(double deltaTime, Point targetPoint)
        {
            if (PlayerShape is Image)
            {
                _targetAngle = Math.Atan2(targetPoint.Y - Y, targetPoint.X - X);
                UpdateRotation(deltaTime);
            }
            
            CurrentWeapon.Update(deltaTime);
        }
        
        public void StartReload()
        {
            CurrentWeapon.StartReload();
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
    }
} 