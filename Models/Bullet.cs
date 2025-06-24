using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GunVault.GameEngine;
using GunVault.Models.Physics;
using System.Collections.Generic;

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
        public double Radius { get; private set; }
        
        private string _currentSpriteName;
        private readonly SpriteManager _spriteManager;
        
        public TileType? CollidedWithTileType { get; private set; }
        
        // Свойства для взрывных пуль (ракет)
        public bool IsExplosive { get; set; }
        public double ExplosionRadius { get; set; }
        public double ExplosionDamage { get; set; }
        
        // Свойства для рикошета
        public bool CanRicochet { get; private set; }
        public int RicochetCount { get; private set; }
        public int MaxRicochets { get; private set; }
        private readonly List<Enemy> _hitEnemies;
        
        public bool IsActive { get; private set; }
        
        public Bullet(double startX, double startY, double angle, double speed, double damage, double bulletSize, string spriteName, SpriteManager spriteManager)
        {
            X = startX;
            Y = startY;
            _prevX = startX;
            _prevY = startY;
            _angle = angle;
            Speed = speed;
            Damage = damage;
            Radius = bulletSize / 2.0;
            _spriteManager = spriteManager;
            _currentSpriteName = spriteName;
            CollidedWithTileType = null;
            
            // По умолчанию пуля не взрывается
            IsExplosive = false;
            ExplosionRadius = 0;
            ExplosionDamage = 0;
            IsActive = true;
            
            // Настройки рикошета по умолчанию
            CanRicochet = false;
            RicochetCount = 0;
            MaxRicochets = 0;
            _hitEnemies = new List<Enemy>();
            
            // Create a generic shape that can be restyled later. A Rectangle is good for this.
            BulletShape = new Rectangle
            {
                Width = bulletSize,
                Height = bulletSize,
                RadiusX = bulletSize / 2, // Make it a circle by default
                RadiusY = bulletSize / 2
            };
            
            UpdateShapeStyle(spriteName, bulletSize);
            
            UpdatePosition();
        }
        
        public void UpdatePosition()
        {
            Canvas.SetLeft(BulletShape, X - Radius);
            Canvas.SetTop(BulletShape, Y - Radius);
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
                Radius, 
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
                Radius, 
                tileCollider))
            {
                CollidedWithTileType = tileType;
                return true;
            }
            
            return false;
        }

        public void Init(double startX, double startY, double angle, double speed, double damage, double bulletSize, string spriteName, bool isExplosive, double explosionRadius, double explosionDamage, bool canRicochet, int maxRicochets)
        {
            X = startX;
            Y = startY;
            _prevX = startX;
            _prevY = startY;
            _angle = angle;
            Speed = speed;
            Damage = damage;
            Radius = bulletSize / 2.0;
            IsExplosive = isExplosive;
            ExplosionRadius = explosionRadius;
            ExplosionDamage = explosionDamage;
            CollidedWithTileType = null;
            
            // Инициализация рикошета
            CanRicochet = canRicochet;
            MaxRicochets = maxRicochets;
            RicochetCount = 0;
            _hitEnemies.Clear();

            if (BulletShape is Shape shape)
            {
                shape.Width = bulletSize;
                shape.Height = bulletSize;

                // Only update style if the sprite name has changed
                if (_currentSpriteName != spriteName)
                {
                    UpdateShapeStyle(spriteName, bulletSize);
                    _currentSpriteName = spriteName;
                }
            }
            
            UpdatePosition();
            Activate();
        }

        private void UpdateShapeStyle(string spriteName, double bulletSize)
        {
            if (BulletShape is not Rectangle shape) return;

            if (_spriteManager != null && !string.IsNullOrEmpty(spriteName))
            {
                // Use sprite
                var sprite = _spriteManager.LoadSprite(spriteName);
                if (sprite != null)
                {
                    shape.Fill = new ImageBrush(sprite);
                    shape.Stroke = Brushes.Transparent;
                    shape.StrokeThickness = 0;
                    shape.RadiusX = 0; // Make it a square for the sprite
                    shape.RadiusY = 0;
                }
                else
                {
                    // Fallback if sprite fails to load
                    shape.Fill = Brushes.MediumPurple; // Use a distinct color for debugging
                    shape.Stroke = Brushes.White;
                    shape.StrokeThickness = 1;
                    shape.RadiusX = bulletSize / 2;
                    shape.RadiusY = bulletSize / 2;
                }
            }
            else
            {
                // Use default circle
                shape.Fill = Brushes.Yellow;
                shape.Stroke = Brushes.White;
                shape.StrokeThickness = 1;
                shape.RadiusX = bulletSize / 2; // Make it a circle
                shape.RadiusY = bulletSize / 2;
            }
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

        public void Redirect(double newAngle)
        {
            _angle = newAngle;
            RicochetCount++;
        }

        public void AddHitEnemy(Enemy enemy)
        {
            if (!_hitEnemies.Contains(enemy))
            {
                _hitEnemies.Add(enemy);
            }
        }

        public bool HasHitEnemy(Enemy enemy)
        {
            return _hitEnemies.Contains(enemy);
        }
    }
} 