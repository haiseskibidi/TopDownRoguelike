using System;
using System.Windows;

namespace GunVault.GameEngine
{
    public class Camera
    {
        // Позиция камеры (левый верхний угол видимой области)
        public double X { get; private set; }
        public double Y { get; private set; }
        
        // Ширина и высота области просмотра
        public double ViewportWidth { get; private set; }
        public double ViewportHeight { get; private set; }
        
        // Размер всего игрового мира
        public double WorldWidth { get; private set; }
        public double WorldHeight { get; private set; }
        
        // Процент от края экрана, при котором начинается скроллинг
        private const double SCROLL_BOUNDARY_PERCENT = 0.4;
        
        public Camera(double viewportWidth, double viewportHeight, double worldWidth, double worldHeight)
        {
            ViewportWidth = viewportWidth;
            ViewportHeight = viewportHeight;
            WorldWidth = worldWidth;
            WorldHeight = worldHeight;
            X = 0;
            Y = 0;
        }
        
        /// <summary>
        /// Обновляет размеры области просмотра
        /// </summary>
        public void UpdateViewport(double viewportWidth, double viewportHeight)
        {
            ViewportWidth = viewportWidth;
            ViewportHeight = viewportHeight;
        }
        
        /// <summary>
        /// Обновляет размеры игрового мира
        /// </summary>
        public void UpdateWorldSize(double worldWidth, double worldHeight)
        {
            WorldWidth = worldWidth;
            WorldHeight = worldHeight;
            
            // Ограничиваем позицию камеры, чтобы не выходить за пределы мира
            ClampPosition();
        }
        
        /// <summary>
        /// Следует за целью, скроллируя камеру при приближении к краям
        /// </summary>
        public void FollowTarget(double targetX, double targetY)
        {
            // Вычисляем границы скроллинга
            double scrollBoundaryLeft = X + ViewportWidth * SCROLL_BOUNDARY_PERCENT;
            double scrollBoundaryRight = X + ViewportWidth * (1 - SCROLL_BOUNDARY_PERCENT);
            double scrollBoundaryTop = Y + ViewportHeight * SCROLL_BOUNDARY_PERCENT;
            double scrollBoundaryBottom = Y + ViewportHeight * (1 - SCROLL_BOUNDARY_PERCENT);
            
            // Перемещаем камеру, если цель приближается к границам
            if (targetX < scrollBoundaryLeft)
            {
                X = targetX - ViewportWidth * SCROLL_BOUNDARY_PERCENT;
            }
            else if (targetX > scrollBoundaryRight)
            {
                X = targetX - ViewportWidth * (1 - SCROLL_BOUNDARY_PERCENT);
            }
            
            if (targetY < scrollBoundaryTop)
            {
                Y = targetY - ViewportHeight * SCROLL_BOUNDARY_PERCENT;
            }
            else if (targetY > scrollBoundaryBottom)
            {
                Y = targetY - ViewportHeight * (1 - SCROLL_BOUNDARY_PERCENT);
            }
            
            // Ограничиваем позицию камеры границами мира
            ClampPosition();
        }
        
        /// <summary>
        /// Центрирует камеру вокруг указанной точки
        /// </summary>
        public void CenterOn(double x, double y)
        {
            X = x - ViewportWidth / 2;
            Y = y - ViewportHeight / 2;
            ClampPosition();
        }
        
        /// <summary>
        /// Ограничивает позицию камеры границами мира
        /// </summary>
        private void ClampPosition()
        {
            // Если мир меньше области просмотра, центрируем камеру
            if (WorldWidth <= ViewportWidth)
            {
                X = (WorldWidth - ViewportWidth) / 2;
            }
            else
            {
                X = Math.Max(0, Math.Min(X, WorldWidth - ViewportWidth));
            }
            
            if (WorldHeight <= ViewportHeight)
            {
                Y = (WorldHeight - ViewportHeight) / 2;
            }
            else
            {
                Y = Math.Max(0, Math.Min(Y, WorldHeight - ViewportHeight));
            }
        }
        
        /// <summary>
        /// Преобразует мировые координаты в координаты холста
        /// </summary>
        public Point WorldToScreen(double worldX, double worldY)
        {
            return new Point(worldX - X, worldY - Y);
        }
        
        /// <summary>
        /// Преобразует координаты холста в мировые координаты
        /// </summary>
        public Point ScreenToWorld(double screenX, double screenY)
        {
            return new Point(screenX + X, screenY + Y);
        }
        
        /// <summary>
        /// Проверяет, находится ли объект в поле зрения камеры
        /// </summary>
        public bool IsInView(double worldX, double worldY, double width, double height)
        {
            return worldX + width >= X && 
                   worldX <= X + ViewportWidth && 
                   worldY + height >= Y && 
                   worldY <= Y + ViewportHeight;
        }
    }
} 