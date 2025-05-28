using System.Collections.Generic;
using GunVault.GameEngine;
using GunVault.Models;

namespace GunVault.Models.Physics
{
    public class PhysicsEngine
    {
        private readonly LevelGenerator _levelGenerator;
        private readonly Dictionary<string, RectCollider> _staticColliders;
        
        public PhysicsEngine(LevelGenerator levelGenerator)
        {
            _levelGenerator = levelGenerator;
            _staticColliders = levelGenerator.GetTileColliders();
        }
        
        public bool CheckBulletTileCollisions(Bullet bullet)
        {
            foreach (var colliderPair in _staticColliders)
            {
                string key = colliderPair.Key;
                RectCollider collider = colliderPair.Value;
                
                string[] parts = key.Split(':');
                if (parts.Length == 2 && 
                    int.TryParse(parts[0], out int tileX) && 
                    int.TryParse(parts[1], out int tileY))
                {
                    TileType tileType = _levelGenerator.GetTileType(tileX, tileY);
                    
                    if (bullet.CollidesWithTile(collider, tileType))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        public bool CanMoveToPosition(Collider entityCollider, double newX, double newY)
        {
            Collider tempCollider;
            if (entityCollider is CircleCollider circle)
            {
                tempCollider = new CircleCollider(newX, newY, circle.Radius);
            }
            else if (entityCollider is RectCollider rect)
            {
                tempCollider = new RectCollider(newX, newY, rect.Width, rect.Height);
            }
            else
            {
                return false;
            }
            
            foreach (var colliderPair in _staticColliders)
            {
                RectCollider tileCollider = colliderPair.Value;
                
                string[] parts = colliderPair.Key.Split(':');
                if (parts.Length == 2 && 
                    int.TryParse(parts[0], out int tileX) && 
                    int.TryParse(parts[1], out int tileY))
                {
                    TileType tileType = _levelGenerator.GetTileType(tileX, tileY);
                    TileInfo tileInfo = TileSettings.TileInfos[tileType];
                    
                    if (!tileInfo.IsWalkable && tempCollider.Intersects(tileCollider))
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        public int ProcessBulletEnemyCollisions(List<Bullet> bullets, List<Enemy> enemies)
        {
            int score = 0;
            
            List<Bullet> activeBullets = new List<Bullet>(bullets);
            List<Enemy> activeEnemies = new List<Enemy>(enemies);
            
            foreach (Bullet bullet in activeBullets)
            {
                foreach (Enemy enemy in activeEnemies)
                {
                    if (!enemy.IsDead && bullet.Collides(enemy))
                    {
                        bool stillAlive = enemy.TakeDamage(bullet.Damage);
                        if (!stillAlive)
                        {
                            score += enemy.ScoreValue;
                        }
                        
                        bullet.RemainingRange = 0;
                        
                        break;
                    }
                }
            }
            
            return score;
        }
        
        public void RefreshStaticColliders()
        {
            _staticColliders.Clear();
            foreach (var colliderPair in _levelGenerator.GetTileColliders())
            {
                _staticColliders.Add(colliderPair.Key, colliderPair.Value);
            }
        }
    }
} 