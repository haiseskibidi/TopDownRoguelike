using System;
using System.Windows;

namespace GunVault.GameEngine
{
    public class Camera
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        
        public double ViewportWidth { get; private set; }
        public double ViewportHeight { get; private set; }
        
        public double WorldWidth { get; private set; }
        public double WorldHeight { get; private set; }
        
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
        
        public void UpdateViewport(double viewportWidth, double viewportHeight)
        {
            ViewportWidth = viewportWidth;
            ViewportHeight = viewportHeight;
        }
        
        public void UpdateWorldSize(double worldWidth, double worldHeight)
        {
            WorldWidth = worldWidth;
            WorldHeight = worldHeight;
            ClampPosition();
        }
        
        public void FollowTarget(double targetX, double targetY)
        {
            double scrollBoundaryLeft = X + ViewportWidth * SCROLL_BOUNDARY_PERCENT;
            double scrollBoundaryRight = X + ViewportWidth * (1 - SCROLL_BOUNDARY_PERCENT);
            double scrollBoundaryTop = Y + ViewportHeight * SCROLL_BOUNDARY_PERCENT;
            double scrollBoundaryBottom = Y + ViewportHeight * (1 - SCROLL_BOUNDARY_PERCENT);
            
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
            
            ClampPosition();
        }
        
        public void CenterOn(double x, double y)
        {
            X = x - ViewportWidth / 2;
            Y = y - ViewportHeight / 2;
            ClampPosition();
        }
        
        private void ClampPosition()
        {
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
        
        public Point WorldToScreen(double worldX, double worldY)
        {
            return new Point(worldX - X, worldY - Y);
        }
        
        public Point ScreenToWorld(double screenX, double screenY)
        {
            return new Point(screenX + X, screenY + Y);
        }
        
        public bool IsInView(double worldX, double worldY, double width, double height)
        {
            return worldX + width >= X && 
                   worldX <= X + ViewportWidth && 
                   worldY + height >= Y && 
                   worldY <= Y + ViewportHeight;
        }
        
        public bool IsInViewExtended(double worldX, double worldY, double extendedRange)
        {
            return worldX + extendedRange >= X && 
                   worldX - extendedRange <= X + ViewportWidth && 
                   worldY + extendedRange >= Y && 
                   worldY - extendedRange <= Y + ViewportHeight;
        }
    }
}