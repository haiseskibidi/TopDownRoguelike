using System.Collections.Generic;
using GunVault.GameEngine;
using GunVault.Models;

namespace GunVault.Models.Physics
{
    /// <summary>
    /// Класс-движок для обработки физики и столкновений в игре
    /// </summary>
    public class PhysicsEngine
    {
        private readonly LevelGenerator _levelGenerator;
        private readonly Dictionary<string, RectCollider> _staticColliders;
        
        /// <summary>
        /// Создает новый экземпляр движка физики
        /// </summary>
        /// <param name="levelGenerator">Генератор уровня с коллайдерами</param>
        public PhysicsEngine(LevelGenerator levelGenerator)
        {
            _levelGenerator = levelGenerator;
            _staticColliders = levelGenerator.GetTileColliders();
        }
        
        /// <summary>
        /// Проверяет столкновение пули со всеми тайлами на карте
        /// </summary>
        /// <param name="bullet">Проверяемая пуля</param>
        /// <returns>true если произошло столкновение с каким-либо тайлом</returns>
        public bool CheckBulletTileCollisions(Bullet bullet)
        {
            foreach (var colliderPair in _staticColliders)
            {
                string key = colliderPair.Key;
                RectCollider collider = colliderPair.Value;
                
                // Парсим ключ для получения координат тайла
                // Ключ хранится в формате "x:y"
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
        
        /// <summary>
        /// Проверяет, может ли сущность двигаться в указанную позицию
        /// </summary>
        /// <param name="collider">Коллайдер сущности</param>
        /// <param name="newX">Новая позиция X</param>
        /// <param name="newY">Новая позиция Y</param>
        /// <returns>true если движение возможно (нет коллизий)</returns>
        public bool CanMoveToPosition(Collider entityCollider, double newX, double newY)
        {
            // Создаем временную копию коллайдера в новой позиции
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
                return false; // Неизвестный тип коллайдера
            }
            
            // Проверяем пересечения с тайлами
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
        
        /// <summary>
        /// Проверяет и обрабатывает столкновения между пулями и врагами
        /// </summary>
        /// <param name="bullets">Список активных пуль</param>
        /// <param name="enemies">Список активных врагов</param>
        /// <returns>Счет, полученный за уничтожение врагов</returns>
        public int ProcessBulletEnemyCollisions(List<Bullet> bullets, List<Enemy> enemies)
        {
            int score = 0;
            
            // Копируем списки, чтобы избежать проблем с модификацией во время итерации
            List<Bullet> activeBullets = new List<Bullet>(bullets);
            List<Enemy> activeEnemies = new List<Enemy>(enemies);
            
            foreach (Bullet bullet in activeBullets)
            {
                foreach (Enemy enemy in activeEnemies)
                {
                    if (!enemy.IsDead && bullet.Collides(enemy))
                    {
                        // Наносим урон врагу
                        bool stillAlive = enemy.TakeDamage(bullet.Damage);
                        if (!stillAlive)  // Метод TakeDamage возвращает true, если враг всё ещё жив
                        {
                            score += enemy.ScoreValue;
                        }
                        
                        // Помечаем пулю для удаления
                        bullet.RemainingRange = 0;
                        
                        break; // Пуля может поразить только одного врага
                    }
                }
            }
            
            return score;
        }
        
        /// <summary>
        /// Обновляет все статические коллайдеры при изменении уровня
        /// </summary>
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