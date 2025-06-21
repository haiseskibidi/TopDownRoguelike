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
        
        public double PrevX => _prevX;
        public double PrevY => _prevY;
        private double _prevX;
        private double _prevY;
        
        public UIElement BulletShape { get; private set; }
        private const double BULLET_RADIUS = 4.0; 
        
        public TileType? CollidedWithTileType { get; private set; }
        
        // Свойства для взрывных пуль (ракет)
        public bool IsExplosive { get; set; }
        public double ExplosionRadius { get; set; }
        public double ExplosionDamage { get; set; }
        
        public bool IsActive { get; private set; }
        
        public Bullet(double startX, double startY, double angle, double speed, double damage, string spriteName, SpriteManager spriteManager)
        {
            X = startX;
            Y = startY;
            _prevX = startX;
            _prevY = startY;
            _angle = angle;
            Speed = speed;
            Damage = damage;
            CollidedWithTileType = null;
            
            // По умолчанию пуля не взрывается
            IsExplosive = false;
            ExplosionRadius = 0;
            ExplosionDamage = 0;
            IsActive = true;
            
            if (spriteManager != null && !string.IsNullOrEmpty(spriteName))
            {
                BulletShape = spriteManager.CreateSpriteImage(spriteName, BULLET_RADIUS * 2, BULLET_RADIUS * 2);
            }
            else
            {
                BulletShape = new Ellipse
                {
                    Width = BULLET_RADIUS * 2,
                    Height = BULLET_RADIUS * 2,
                    Fill = Brushes.Yellow,
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };
            }
            
            UpdatePosition();
        }
        
        public void UpdatePosition()
        {
            Canvas.SetLeft(BulletShape, X - BULLET_RADIUS);
            Canvas.SetTop(BulletShape, Y - BULLET_RADIUS);
        }
        
        public bool Move(double deltaTime)
        {
            if (!IsActive) return false;

            _prevX = X;
            _prevY = Y;
            
            double moveDistance = Speed * deltaTime;
            
            X += Math.Cos(_angle) * moveDistance;
            Y += Math.Sin(_angle) * moveDistance;
            
            UpdatePosition();
            
            return true; // Bullets now have infinite range, lifetime managed by GameManager
        }
        
        public bool Collides(Enemy enemy)
        {
            if (!IsActive) return false;

            return CollisionHelper.CheckBulletEnemyCollision(
                X, Y, 
                _prevX, _prevY, 
                BULLET_RADIUS, 
                enemy.X, enemy.Y, 
                enemy.Radius);
        }
        
        public bool CollidesWithTile(RectCollider tileCollider, TileType tileType)
        {
            if (!IsActive) return false;

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

        public void Init(double startX, double startY, double angle, double speed, double damage, string spriteName, SpriteManager spriteManager, bool isExplosive, double explosionRadius, double explosionDamage)
        {
            X = startX;
            Y = startY;
            _prevX = startX;
            _prevY = startY;
            _angle = angle;
            Speed = speed;
            Damage = damage;
            IsExplosive = isExplosive;
            ExplosionRadius = explosionRadius;
            ExplosionDamage = explosionDamage;
            CollidedWithTileType = null;

            // Here we assume the BulletShape (UIElement) is already created and just needs to be updated.
            UpdatePosition();
            Activate();
        }

        public void Activate()
        {
            IsActive = true;
            BulletShape.Visibility = Visibility.Visible;
        }

        public void Deactivate()
        {
            IsActive = false;
            BulletShape.Visibility = Visibility.Collapsed;
        }
    }
} 