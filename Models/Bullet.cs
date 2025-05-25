using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GunVault.GameEngine;
using GunVault.Models.Physics;

namespace GunVault.Models
{
    public class Bullet
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        private double _angle;
        public double Speed { get; private set; }
        public double Damage { get; private set; }
        public double RemainingRange { get; set; }
        
        public double PrevX => _prevX;
        public double PrevY => _prevY;
        private double _prevX;
        private double _prevY;
        
        public Ellipse BulletShape { get; private set; }
        private const double BULLET_RADIUS = 4.0; 
        
        public TileType? CollidedWithTileType { get; private set; }
        
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
            CollidedWithTileType = null;
            
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
            return CollisionHelper.CheckBulletEnemyCollision(
                X, Y, 
                _prevX, _prevY, 
                BULLET_RADIUS, 
                enemy.X, enemy.Y, 
                enemy.Radius);
        }
        
        public bool CollidesWithTile(RectCollider tileCollider, TileType tileType)
        {
            if (tileType == TileType.Water)
                return false;
            
            if (CollisionHelper.CheckBulletTileCollision(
                X, Y, 
                _prevX, _prevY, 
                BULLET_RADIUS, 
                tileCollider))
            {
                CollidedWithTileType = tileType;
                return true;
            }
            
            return false;
        }
    }
} 