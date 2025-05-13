using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GunVault.GameEngine;

namespace GunVault.Models
{
    public class Enemy
    {
        // Константы
        private const double ENEMY_ROTATION_SPEED = 8.0; // Скорость поворота врага в радианах в секунду
        
        // Свойства
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Health { get; private set; }
        public double MaxHealth { get; private set; }
        public double Speed { get; private set; }
        public double Radius { get; private set; }
        public int ScoreValue { get; private set; }
        public double DamageOnCollision { get; private set; } // Урон, наносимый игроку при столкновении
        public EnemyType Type { get; private set; } // Тип врага
        
        // Визуальное представление
        public UIElement EnemyShape { get; private set; }
        public Rectangle HealthBar { get; private set; }
        
        // Угол поворота
        private double _currentAngle = 0; // Текущий угол в радианах
        private double _targetAngle = 0;  // Целевой угол в радианах
        
        // Расширенный конструктор с поддержкой типов и дополнительных параметров
        public Enemy(double startX, double startY, double health, double speed, double radius, int scoreValue, 
                    double damageOnCollision = 10, EnemyType type = EnemyType.Basic, string spriteName = "enemy1", 
                    SpriteManager spriteManager = null)
        {
            X = startX;
            Y = startY;
            MaxHealth = health;
            Health = MaxHealth;
            Speed = speed;
            Radius = radius;
            ScoreValue = scoreValue;
            DamageOnCollision = damageOnCollision;
            Type = type;
            
            // Создаем визуальное представление врага
            if (spriteManager != null)
            {
                // Используем спрайт
                EnemyShape = spriteManager.CreateSpriteImage(spriteName, Radius * 2, Radius * 2);
            }
            else
            {
                // Резервный вариант - круг с цветом в зависимости от типа
                EnemyShape = new Ellipse
                {
                    Width = Radius * 2,
                    Height = Radius * 2,
                    Fill = GetEnemyColor(type),
                    Stroke = Brushes.DarkRed,
                    StrokeThickness = 2
                };
            }
            
            // Полоса здоровья
            HealthBar = new Rectangle
            {
                Width = Radius * 2,
                Height = 5,
                Fill = Brushes.Green
            };
            
            UpdatePosition();
        }
        
        // Получение цвета врага в зависимости от типа (для резервного варианта отображения)
        private SolidColorBrush GetEnemyColor(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Basic:
                    return Brushes.Red;
                case EnemyType.Runner:
                    return Brushes.LimeGreen;
                case EnemyType.Tank:
                    return Brushes.DarkBlue;
                case EnemyType.Bomber:
                    return Brushes.Orange;
                case EnemyType.Boss:
                    return Brushes.Purple;
                default:
                    return Brushes.Red;
            }
        }
        
        // Обновление позиции и внешнего вида врага
        public void UpdatePosition()
        {
            Canvas.SetLeft(EnemyShape, X - Radius);
            Canvas.SetTop(EnemyShape, Y - Radius);
            
            Canvas.SetLeft(HealthBar, X - Radius);
            Canvas.SetTop(HealthBar, Y - Radius - 10);
            
            HealthBar.Width = (Health / MaxHealth) * (Radius * 2);
            
            // Если это спрайт, применяем поворот
            if (EnemyShape is Image)
            {
                // Поворачиваем спрайт в текущем направлении
                var rotateTransform = new RotateTransform(_currentAngle * 180 / Math.PI, Radius, Radius);
                EnemyShape.RenderTransform = rotateTransform;
                
                // Если спрайт смотрит влево (180°±90°), то отражаем его по вертикали
                if (Math.Abs(NormalizeAngle(_currentAngle)) > Math.PI / 2)
                {
                    // Отражаем спрайт по вертикали для корректного отображения
                    var scaleTransform = new ScaleTransform(1, -1, Radius, Radius);
                    
                    // Применяем трансформации: сначала отражение, затем поворот
                    TransformGroup transformGroup = new TransformGroup();
                    transformGroup.Children.Add(scaleTransform);
                    transformGroup.Children.Add(rotateTransform);
                    
                    EnemyShape.RenderTransform = transformGroup;
                }
            }
        }
        
        // Нормализация угла в диапазоне [-π, π]
        private double NormalizeAngle(double angle)
        {
            while (angle > Math.PI)
                angle -= 2 * Math.PI;
            while (angle < -Math.PI)
                angle += 2 * Math.PI;
            return angle;
        }
        
        // Плавный поворот врага к целевому углу
        private void UpdateRotation(double deltaTime)
        {
            // Находим кратчайший путь поворота
            double angleDifference = NormalizeAngle(_targetAngle - _currentAngle);
            
            // Определяем максимальное изменение угла за этот кадр
            double maxRotation = ENEMY_ROTATION_SPEED * deltaTime;
            
            // Если разница меньше максимальной скорости поворота, сразу устанавливаем целевой угол
            if (Math.Abs(angleDifference) <= maxRotation)
            {
                _currentAngle = _targetAngle;
            }
            else
            {
                // Иначе поворачиваем в сторону цели с ограниченной скоростью
                double sign = Math.Sign(angleDifference);
                _currentAngle += sign * maxRotation;
                _currentAngle = NormalizeAngle(_currentAngle); // Нормализуем угол
            }
            
            // Обновляем визуальное представление врага
            UpdatePosition();
        }
        
        // Движение врага к игроку
        public void MoveTowardsPlayer(double playerX, double playerY, double deltaTime)
        {
            double dx = playerX - X;
            double dy = playerY - Y;
            
            double length = Math.Sqrt(dx * dx + dy * dy);
            
            // Обновляем целевой угол поворота врага к игроку
            _targetAngle = Math.Atan2(dy, dx);
            
            // Плавно поворачиваем врага
            UpdateRotation(deltaTime);
            
            if (length > Radius)
            {
                dx /= length;
                dy /= length;
                
                // Скорость может различаться в зависимости от типа врага
                double currentSpeed = Speed;
                
                // Для бомбера увеличиваем скорость, когда он близко к игроку
                if (Type == EnemyType.Bomber && length < 100)
                {
                    currentSpeed *= 1.5; // Увеличиваем скорость на 50% при сближении
                }
                
                X += dx * currentSpeed * deltaTime;
                Y += dy * currentSpeed * deltaTime;
                
                UpdatePosition();
            }
        }
        
        // Получение урона
        public bool TakeDamage(double damage)
        {
            Health = Math.Max(0, Health - damage);
            UpdatePosition();
            
            return Health > 0;
        }
        
        // Проверка столкновения с игроком
        public bool CollidesWithPlayer(Player player)
        {
            double dx = X - player.X;
            double dy = Y - player.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            
            return distance < Radius + 20;
        }
    }
} 