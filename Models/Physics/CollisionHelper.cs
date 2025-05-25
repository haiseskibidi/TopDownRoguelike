using System;
using GunVault.GameEngine;

namespace GunVault.Models.Physics
{
    /// <summary>
    /// Вспомогательный класс для расчета столкновений между сущностями
    /// </summary>
    public static class CollisionHelper
    {
        /// <summary>
        /// Проверяет столкновение между пулей и врагом
        /// </summary>
        /// <param name="bulletX">Позиция пули X</param>
        /// <param name="bulletY">Позиция пули Y</param>
        /// <param name="bulletPrevX">Предыдущая позиция пули X</param>
        /// <param name="bulletPrevY">Предыдущая позиция пули Y</param>
        /// <param name="bulletRadius">Радиус пули</param>
        /// <param name="enemyX">Позиция врага X</param>
        /// <param name="enemyY">Позиция врага Y</param>
        /// <param name="enemyRadius">Радиус врага</param>
        /// <returns>true если произошло столкновение</returns>
        public static bool CheckBulletEnemyCollision(
            double bulletX, double bulletY, 
            double bulletPrevX, double bulletPrevY,
            double bulletRadius,
            double enemyX, double enemyY, 
            double enemyRadius)
        {
            // Проверяем прямое пересечение
            double dx = bulletX - enemyX;
            double dy = bulletY - enemyY;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            
            if (distance < bulletRadius + enemyRadius)
                return true;
                
            // Если движения почти нет, то ничего не делаем
            double moveDist = Math.Sqrt(Math.Pow(bulletX - bulletPrevX, 2) + Math.Pow(bulletY - bulletPrevY, 2));
            if (moveDist < bulletRadius * 0.5)
                return false;
                
            // Расчет вектора движения пули
            double vectorX = bulletX - bulletPrevX;
            double vectorY = bulletY - bulletPrevY;
            double vectorLength = Math.Sqrt(vectorX * vectorX + vectorY * vectorY);
            
            if (vectorLength > 0)
            {
                vectorX /= vectorLength;
                vectorY /= vectorLength;
            }
            
            // Расчет проекции вектора от предыдущей позиции пули до центра врага
            // на вектор движения пули
            double toPrevX = enemyX - bulletPrevX;
            double toPrevY = enemyY - bulletPrevY;
            
            double projection = toPrevX * vectorX + toPrevY * vectorY;
            
            double closestX, closestY;
            
            // Определение ближайшей к врагу точки на траектории пули
            if (projection < 0)
            {
                closestX = bulletPrevX;
                closestY = bulletPrevY;
            }
            else if (projection > vectorLength)
            {
                closestX = bulletX;
                closestY = bulletY;
            }
            else
            {
                closestX = bulletPrevX + projection * vectorX;
                closestY = bulletPrevY + projection * vectorY;
            }
            
            // Проверка расстояния от ближайшей точки до врага
            double closestDx = closestX - enemyX;
            double closestDy = closestY - enemyY;
            double closestDistance = Math.Sqrt(closestDx * closestDx + closestDy * closestDy);
            
            return closestDistance < bulletRadius + enemyRadius;
        }
        
        /// <summary>
        /// Проверяет столкновение между пулей и тайлом
        /// </summary>
        /// <param name="bulletX">Позиция пули X</param>
        /// <param name="bulletY">Позиция пули Y</param>
        /// <param name="bulletPrevX">Предыдущая позиция пули X</param>
        /// <param name="bulletPrevY">Предыдущая позиция пули Y</param>
        /// <param name="bulletRadius">Радиус пули</param>
        /// <param name="tileCollider">Коллайдер тайла</param>
        /// <returns>true если произошло столкновение</returns>
        public static bool CheckBulletTileCollision(
            double bulletX, double bulletY,
            double bulletPrevX, double bulletPrevY,
            double bulletRadius,
            RectCollider tileCollider)
        {
            // Проверяем прямое пересечение
            if (tileCollider.ContainsPoint(bulletX, bulletY))
            {
                return true;
            }
            
            // Если движения почти нет, то ничего не делаем
            double moveDist = Math.Sqrt(Math.Pow(bulletX - bulletPrevX, 2) + Math.Pow(bulletY - bulletPrevY, 2));
            if (moveDist < bulletRadius * 0.5)
                return false;
                
            // Расчет вектора движения пули
            double vectorX = bulletX - bulletPrevX;
            double vectorY = bulletY - bulletPrevY;
            
            double vectorLength = Math.Sqrt(vectorX * vectorX + vectorY * vectorY);
            if (vectorLength > 0)
            {
                vectorX /= vectorLength;
                vectorY /= vectorLength;
            }
            
            // Динамическое определение количества проверочных точек в зависимости от скорости
            int checkPoints = Math.Max(10, (int)(moveDist / (bulletRadius * 0.5)));
            
            // Проверяем точки вдоль пути пули
            for (int i = 0; i <= checkPoints; i++)
            {
                double t = i / (double)checkPoints;
                double checkX = bulletPrevX + t * (bulletX - bulletPrevX);
                double checkY = bulletPrevY + t * (bulletY - bulletPrevY);
                
                if (tileCollider.ContainsPoint(checkX, checkY))
                {
                    return true;
                }
                
                // Проверяем дополнительные точки вокруг траектории, чтобы учесть радиус пули
                for (int offset = 1; offset <= 2; offset++)
                {
                    double offsetDistance = bulletRadius * 0.7 * offset;
                    
                    // Проверки в перпендикулярных направлениях
                    double checkX1 = checkX + vectorY * offsetDistance;
                    double checkY1 = checkY - vectorX * offsetDistance;
                    
                    double checkX2 = checkX - vectorY * offsetDistance;
                    double checkY2 = checkY + vectorX * offsetDistance;
                    
                    if (tileCollider.ContainsPoint(checkX1, checkY1) || 
                        tileCollider.ContainsPoint(checkX2, checkY2))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
    }
} 