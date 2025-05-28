using System;
using GunVault.GameEngine;

namespace GunVault.Models.Physics
{
    public static class CollisionHelper
    {
        public static bool CheckBulletEnemyCollision(
            double bulletX, double bulletY, 
            double bulletPrevX, double bulletPrevY,
            double bulletRadius,
            double enemyX, double enemyY, 
            double enemyRadius)
        {
            double dx = bulletX - enemyX;
            double dy = bulletY - enemyY;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            
            if (distance < bulletRadius + enemyRadius)
                return true;
                
            double moveDist = Math.Sqrt(Math.Pow(bulletX - bulletPrevX, 2) + Math.Pow(bulletY - bulletPrevY, 2));
            if (moveDist < bulletRadius * 0.5)
                return false;
                
            double vectorX = bulletX - bulletPrevX;
            double vectorY = bulletY - bulletPrevY;
            double vectorLength = Math.Sqrt(vectorX * vectorX + vectorY * vectorY);
            
            if (vectorLength > 0)
            {
                vectorX /= vectorLength;
                vectorY /= vectorLength;
            }
            
            double toPrevX = enemyX - bulletPrevX;
            double toPrevY = enemyY - bulletPrevY;
            
            double projection = toPrevX * vectorX + toPrevY * vectorY;
            
            double closestX, closestY;
            
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
            
            double closestDx = closestX - enemyX;
            double closestDy = closestY - enemyY;
            double closestDistance = Math.Sqrt(closestDx * closestDx + closestDy * closestDy);
            
            return closestDistance < bulletRadius + enemyRadius;
        }
        
        public static bool CheckBulletTileCollision(
            double bulletX, double bulletY,
            double bulletPrevX, double bulletPrevY,
            double bulletRadius,
            RectCollider tileCollider)
        {
            if (tileCollider.ContainsPoint(bulletX, bulletY))
            {
                return true;
            }
            
            double moveDist = Math.Sqrt(Math.Pow(bulletX - bulletPrevX, 2) + Math.Pow(bulletY - bulletPrevY, 2));
            if (moveDist < bulletRadius * 0.5)
                return false;
                
            double vectorX = bulletX - bulletPrevX;
            double vectorY = bulletY - bulletPrevY;
            
            double vectorLength = Math.Sqrt(vectorX * vectorX + vectorY * vectorY);
            if (vectorLength > 0)
            {
                vectorX /= vectorLength;
                vectorY /= vectorLength;
            }
            
            int checkPoints = Math.Max(10, (int)(moveDist / (bulletRadius * 0.5)));
            
            for (int i = 0; i <= checkPoints; i++)
            {
                double t = i / (double)checkPoints;
                double checkX = bulletPrevX + t * (bulletX - bulletPrevX);
                double checkY = bulletPrevY + t * (bulletY - bulletPrevY);
                
                if (tileCollider.ContainsPoint(checkX, checkY))
                {
                    return true;
                }
                
                for (int offset = 1; offset <= 2; offset++)
                {
                    double offsetDistance = bulletRadius * 0.7 * offset;
                    
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