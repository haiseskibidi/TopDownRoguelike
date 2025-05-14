using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GunVault.GameEngine;
using System.Windows.Media.Animation;

namespace GunVault.Models
{
    public class Enemy
    {
        private const double ENEMY_ROTATION_SPEED = 8.0;
        private const double HIT_FLASH_DURATION = 0.15;
        
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Health { get; private set; }
        public double MaxHealth { get; private set; }
        public double Speed { get; private set; }
        public double Radius { get; private set; }
        public int ScoreValue { get; private set; }
        public double DamageOnCollision { get; private set; }
        public EnemyType Type { get; private set; }
        
        public UIElement EnemyShape { get; private set; }
        public Rectangle HealthBar { get; private set; }
        
        private double _currentAngle = 0;
        private double _targetAngle = 0;
        private Brush _originalBrush;
        private bool _isFlashing = false;
        private double _originalSpeed;
        
        public Enemy(double startX, double startY, double health, double speed, double radius, int scoreValue, 
                    double damageOnCollision = 10, EnemyType type = EnemyType.Basic, string spriteName = "enemy1", 
                    SpriteManager spriteManager = null)
        {
            X = startX;
            Y = startY;
            MaxHealth = health;
            Health = MaxHealth;
            Speed = speed;
            _originalSpeed = speed;
            Radius = radius;
            ScoreValue = scoreValue;
            DamageOnCollision = damageOnCollision;
            Type = type;
            
            if (spriteManager != null)
            {
                EnemyShape = spriteManager.CreateSpriteImage(spriteName, Radius * 2, Radius * 2);
            }
            else
            {
                _originalBrush = GetEnemyColor(type);
                EnemyShape = new Ellipse
                {
                    Width = Radius * 2,
                    Height = Radius * 2,
                    Fill = _originalBrush,
                    Stroke = Brushes.DarkRed,
                    StrokeThickness = 2
                };
            }
            
            HealthBar = new Rectangle
            {
                Width = Radius * 2,
                Height = 5,
                Fill = Brushes.Green
            };
            
            UpdatePosition();
        }
        
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
        
        public void UpdatePosition()
        {
            Canvas.SetLeft(EnemyShape, X - Radius);
            Canvas.SetTop(EnemyShape, Y - Radius);
            
            Canvas.SetLeft(HealthBar, X - Radius);
            Canvas.SetTop(HealthBar, Y - Radius - 10);
            
            HealthBar.Width = (Health / MaxHealth) * (Radius * 2);
            
            if (EnemyShape is Image)
            {
                var rotateTransform = new RotateTransform(_currentAngle * 180 / Math.PI, Radius, Radius);
                EnemyShape.RenderTransform = rotateTransform;
                
                if (Math.Abs(NormalizeAngle(_currentAngle)) > Math.PI / 2)
                {
                    var scaleTransform = new ScaleTransform(1, -1, Radius, Radius);
                    
                    TransformGroup transformGroup = new TransformGroup();
                    transformGroup.Children.Add(scaleTransform);
                    transformGroup.Children.Add(rotateTransform);
                    
                    EnemyShape.RenderTransform = transformGroup;
                }
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
            
            double maxRotation = ENEMY_ROTATION_SPEED * deltaTime;
            
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
        
        public void MoveTowardsPlayer(double playerX, double playerY, double deltaTime)
        {
            double dx = playerX - X;
            double dy = playerY - Y;
            
            double length = Math.Sqrt(dx * dx + dy * dy);
            
            _targetAngle = Math.Atan2(dy, dx);
            
            UpdateRotation(deltaTime);
            
            if (length > Radius)
            {
                dx /= length;
                dy /= length;
                
                double currentSpeed = Speed;
                
                if (Type == EnemyType.Bomber && length < 100)
                {
                    currentSpeed *= 1.5;
                }
                
                X += dx * currentSpeed * deltaTime;
                Y += dy * currentSpeed * deltaTime;
                
                UpdatePosition();
            }
        }
        
        private void FlashOnHit()
        {
            if (_isFlashing)
                return;
                
            _isFlashing = true;
            
            _originalSpeed = Speed;
            Speed = Speed * 0.5;
            
            if (EnemyShape is Ellipse ellipse)
            {
                if (_originalBrush == null)
                    _originalBrush = ellipse.Fill;
                
                ellipse.Fill = Brushes.Red;
                
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(HIT_FLASH_DURATION)
                };
                
                timer.Tick += (sender, e) =>
                {
                    ellipse.Fill = _originalBrush;
                    Speed = _originalSpeed;
                    timer.Stop();
                    _isFlashing = false;
                };
                
                timer.Start();
            }
            else if (EnemyShape is Image image)
            {
                var effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Red,
                    ShadowDepth = 0,
                    BlurRadius = 20,
                    Opacity = 1
                };
                
                var currentEffect = image.Effect;
                
                image.Effect = effect;
                
                image.OpacityMask = new SolidColorBrush(Color.FromArgb(180, 255, 0, 0));
                
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(HIT_FLASH_DURATION)
                };
                
                timer.Tick += (sender, e) =>
                {
                    image.Effect = currentEffect;
                    image.OpacityMask = null;
                    Speed = _originalSpeed;
                    timer.Stop();
                    _isFlashing = false;
                };
                
                timer.Start();
            }
        }
        
        public bool TakeDamage(double damage)
        {
            Health = Math.Max(0, Health - damage);
            UpdatePosition();
            
            FlashOnHit();
            
            return Health > 0;
        }
        
        public bool CollidesWithPlayer(Player player)
        {
            double dx = X - player.X;
            double dy = Y - player.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            
            return distance < Radius + 20;
        }
    }
} 