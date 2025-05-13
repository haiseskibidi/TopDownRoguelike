using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GunVault.GameEngine;

namespace GunVault.Models
{
    public class Player
    {
        private const double PLAYER_SPEED = 5.0;
        private const double PLAYER_RADIUS = 20.0;
        private const double PLAYER_ROTATION_SPEED = 10.0;
        
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Health { get; private set; }
        public double MaxHealth { get; private set; }
        public UIElement PlayerShape { get; private set; }
        public Weapon CurrentWeapon { get; private set; }
        
        public bool MovingUp { get; set; }
        public bool MovingDown { get; set; }
        public bool MovingLeft { get; set; }
        public bool MovingRight { get; set; }
        
        private double _currentAngle = 0;
        private double _targetAngle = 0;
        
        public Player(double startX, double startY, SpriteManager spriteManager = null)
        {
            X = startX;
            Y = startY;
            MaxHealth = 100;
            Health = MaxHealth;
            
            if (spriteManager != null)
            {
                PlayerShape = spriteManager.CreateSpriteImage("player", PLAYER_RADIUS * 2, PLAYER_RADIUS * 2);
            }
            else
            {
                PlayerShape = new Ellipse
                {
                    Width = PLAYER_RADIUS * 2,
                    Height = PLAYER_RADIUS * 2,
                    Fill = Brushes.Blue,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };
            }
            
            CurrentWeapon = WeaponFactory.CreateWeapon(WeaponType.Pistol);
            
            UpdatePosition();
        }

        public void AddWeaponToCanvas(Canvas canvas)
        {
            canvas.Children.Add(CurrentWeapon.WeaponShape);
        }
        
        public void ChangeWeapon(Weapon newWeapon, Canvas canvas)
        {
            canvas.Children.Remove(CurrentWeapon.WeaponShape);
            CurrentWeapon = newWeapon;
            canvas.Children.Add(CurrentWeapon.WeaponShape);
        }
        
        public void UpdatePosition()
        {
            Canvas.SetLeft(PlayerShape, X - PLAYER_RADIUS);
            Canvas.SetTop(PlayerShape, Y - PLAYER_RADIUS);
            
            if (PlayerShape is Image)
            {
                var rotateTransform = new RotateTransform(_currentAngle * 180 / Math.PI, PLAYER_RADIUS, PLAYER_RADIUS);
                PlayerShape.RenderTransform = rotateTransform;
                
                if (Math.Abs(NormalizeAngle(_currentAngle)) > Math.PI / 2)
                {
                    var scaleTransform = new ScaleTransform(1, -1, PLAYER_RADIUS, PLAYER_RADIUS);
                    TransformGroup transformGroup = new TransformGroup();
                    transformGroup.Children.Add(scaleTransform);
                    transformGroup.Children.Add(rotateTransform);
                    PlayerShape.RenderTransform = transformGroup;
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
            
            if (MovingLeft) dx -= 1;
            if (MovingRight) dx += 1;
            if (MovingUp) dy -= 1;
            if (MovingDown) dy += 1;
            
            if (dx != 0 && dy != 0)
            {
                double length = Math.Sqrt(dx * dx + dy * dy);
                dx /= length;
                dy /= length;
            }
            
            if (dx != 0 || dy != 0)
            {
                _targetAngle = Math.Atan2(dy, dx);
            }
            
            X += dx * PLAYER_SPEED;
            Y += dy * PLAYER_SPEED;
            
            UpdatePosition();
        }
        
        public void ConstrainToScreen(double screenWidth, double screenHeight)
        {
            X = Math.Max(PLAYER_RADIUS, Math.Min(X, screenWidth - PLAYER_RADIUS));
            Y = Math.Max(PLAYER_RADIUS, Math.Min(Y, screenHeight - PLAYER_RADIUS));
            
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
            CurrentWeapon.UpdatePosition(X, Y, targetPoint);
            
            if (CurrentWeapon.IsLaser)
            {
                return null;
            }
            
            if (CurrentWeapon.CanFire())
            {
                Point muzzlePosition = CurrentWeapon.GetMuzzlePosition();
                return CurrentWeapon.Fire(muzzlePosition.X, muzzlePosition.Y, targetPoint.X, targetPoint.Y);
            }
            
            return null;
        }
        
        public LaserBeam ShootLaser(Point targetPoint)
        {
            CurrentWeapon.UpdatePosition(X, Y, targetPoint);
            
            if (!CurrentWeapon.IsLaser || !CurrentWeapon.CanFire())
            {
                return null;
            }
            
            Point muzzlePosition = CurrentWeapon.GetMuzzlePosition();
            return CurrentWeapon.FireLaser(muzzlePosition.X, muzzlePosition.Y, targetPoint.X, targetPoint.Y);
        }
        
        public void UpdateWeapon(double deltaTime, Point targetPoint)
        {
            if (PlayerShape is Image)
            {
                _targetAngle = Math.Atan2(targetPoint.Y - Y, targetPoint.X - X);
                UpdateRotation(deltaTime);
            }
            
            CurrentWeapon.Update(deltaTime);
            CurrentWeapon.UpdatePosition(X, Y, targetPoint);
            CurrentWeapon.UpdateRotation(deltaTime);
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
    }
} 