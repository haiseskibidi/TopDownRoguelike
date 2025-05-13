using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GunVault.Models
{
    public class Bullet
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        private double _angle;
        public double Speed { get; private set; }
        public double Damage { get; private set; }
        public double RemainingRange { get; private set; }
        
        private double _prevX;
        private double _prevY;
        
        public Ellipse BulletShape { get; private set; }
        private const double BULLET_RADIUS = 4.0; 
        
        public Bullet(double startX, double startY, double angle, double speed, double damage, double range, WeaponType weaponType = WeaponType.Pistol)
        {
            X = startX;
            Y = startY;
            _prevX = startX;
            _prevY = startY;
            _angle = angle;
            Speed = speed;
            Damage = damage;
            RemainingRange = range;
            
            SolidColorBrush bulletFill = GetBulletColor(weaponType);
            
            BulletShape = new Ellipse
            {
                Width = BULLET_RADIUS * 2,
                Height = BULLET_RADIUS * 2,
                Fill = bulletFill,
                Stroke = Brushes.White,
                StrokeThickness = 1
            };
            
            UpdatePosition();
        }
        
        private SolidColorBrush GetBulletColor(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Pistol:
                    return new SolidColorBrush(Colors.Yellow);
                case WeaponType.Shotgun:
                    return new SolidColorBrush(Colors.Orange);
                case WeaponType.AssaultRifle:
                    return new SolidColorBrush(Colors.LimeGreen);
                case WeaponType.Sniper:
                    return new SolidColorBrush(Colors.DeepSkyBlue);
                case WeaponType.MachineGun:
                    return new SolidColorBrush(Colors.LightGreen);
                case WeaponType.RocketLauncher:
                    return new SolidColorBrush(Colors.Red);
                case WeaponType.Laser:
                    return new SolidColorBrush(Colors.Magenta);
                default:
                    return new SolidColorBrush(Colors.White);
            }
        }
        
        public void UpdatePosition()
        {
            Canvas.SetLeft(BulletShape, X - BULLET_RADIUS);
            Canvas.SetTop(BulletShape, Y - BULLET_RADIUS);
        }
        
        public bool Move(double deltaTime)
        {
            _prevX = X;
            _prevY = Y;
            
            double moveDistance = Speed * deltaTime;
            
            X += Math.Cos(_angle) * moveDistance;
            Y += Math.Sin(_angle) * moveDistance;
            
            RemainingRange -= moveDistance;
            
            UpdatePosition();
            
            return RemainingRange > 0;
        }
        
        public bool Collides(Enemy enemy)
        {
            double dx = X - enemy.X;
            double dy = Y - enemy.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            
            if (distance < BULLET_RADIUS + enemy.Radius)
                return true;
                
            double moveDist = Math.Sqrt(Math.Pow(X - _prevX, 2) + Math.Pow(Y - _prevY, 2));
            if (moveDist < BULLET_RADIUS)
                return false;
                
            double vectorX = X - _prevX;
            double vectorY = Y - _prevY;
            double vectorLength = Math.Sqrt(vectorX * vectorX + vectorY * vectorY);
            
            if (vectorLength > 0)
            {
                vectorX /= vectorLength;
                vectorY /= vectorLength;
            }
            
            double toPrevX = enemy.X - _prevX;
            double toPrevY = enemy.Y - _prevY;
            
            double projection = toPrevX * vectorX + toPrevY * vectorY;
            
            double closestX, closestY;
            
            if (projection < 0)
            {
                closestX = _prevX;
                closestY = _prevY;
            }
            else if (projection > vectorLength)
            {
                closestX = X;
                closestY = Y;
            }
            else
            {
                closestX = _prevX + projection * vectorX;
                closestY = _prevY + projection * vectorY;
            }
            
            double closestDx = closestX - enemy.X;
            double closestDy = closestY - enemy.Y;
            double closestDistance = Math.Sqrt(closestDx * closestDx + closestDy * closestDy);
            
            return closestDistance < BULLET_RADIUS + enemy.Radius;
        }
    }
} 