using System;
using System.Collections.Generic;
using System.Windows;

namespace GunVault.Models
{
    public abstract class Collider
    {
        public double X { get; protected set; }
        public double Y { get; protected set; }
        
        public abstract void UpdatePosition(double x, double y);
        public abstract bool Intersects(Collider other);
        public abstract bool ContainsPoint(double x, double y);
        public abstract IEnumerable<Point> GetCollisionCheckPoints();
    }

    public class CircleCollider : Collider
    {
        public double Radius { get; private set; }
        
        public CircleCollider(double x, double y, double radius)
        {
            X = x;
            Y = y;
            Radius = radius;
        }
        
        public override void UpdatePosition(double x, double y)
        {
            X = x;
            Y = y;
        }
        
        public override bool Intersects(Collider other)
        {
            if (other is CircleCollider circle)
            {
                double dx = X - circle.X;
                double dy = Y - circle.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                return distance < Radius + circle.Radius;
            }
            
            if (other is RectCollider rect)
            {
                double closestX = Math.Max(rect.X, Math.Min(X, rect.X + rect.Width));
                double closestY = Math.Max(rect.Y, Math.Min(Y, rect.Y + rect.Height));
                
                double dx = X - closestX;
                double dy = Y - closestY;
                double distanceSquared = dx * dx + dy * dy;
                
                return distanceSquared < Radius * Radius;
            }
            
            return false;
        }
        
        public override bool ContainsPoint(double x, double y)
        {
            double dx = X - x;
            double dy = Y - y;
            double distanceSquared = dx * dx + dy * dy;
            return distanceSquared <= Radius * Radius;
        }
        
        public override IEnumerable<Point> GetCollisionCheckPoints()
        {
            List<Point> points = new List<Point>();
            
            points.Add(new Point(X, Y));
            points.Add(new Point(X + Radius, Y));
            points.Add(new Point(X, Y + Radius));
            points.Add(new Point(X - Radius, Y));
            points.Add(new Point(X, Y - Radius));
            
            double diag = Radius * 0.7071;
            points.Add(new Point(X + diag, Y + diag));
            points.Add(new Point(X - diag, Y + diag));
            points.Add(new Point(X - diag, Y - diag));
            points.Add(new Point(X + diag, Y - diag));
            
            return points;
        }
    }

    public class RectCollider : Collider
    {
        public double Width { get; set; }
        public double Height { get; set; }
        
        public RectCollider(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
        
        public override void UpdatePosition(double x, double y)
        {
            X = x;
            Y = y;
        }
        
        public override bool Intersects(Collider other)
        {
            if (other is RectCollider rect)
            {
                const double epsilon = 0.001;
                
                bool noOverlap = 
                    (X + Width + epsilon) <= rect.X ||
                    (X >= rect.X + rect.Width + epsilon) ||
                    (Y + Height + epsilon) <= rect.Y ||
                    (Y >= rect.Y + rect.Height + epsilon);
                    
                return !noOverlap;
            }
            
            if (other is CircleCollider circle)
            {
                double closestX = Math.Clamp(circle.X, X, X + Width);
                double closestY = Math.Clamp(circle.Y, Y, Y + Height);
                
                double distanceX = circle.X - closestX;
                double distanceY = circle.Y - closestY;
                double distanceSquared = distanceX * distanceX + distanceY * distanceY;
                
                return distanceSquared <= circle.Radius * circle.Radius;
            }
            
            return false;
        }
        
        public override bool ContainsPoint(double x, double y)
        {
            return x >= X && x <= X + Width &&
                   y >= Y && y <= Y + Height;
        }
        
        public override IEnumerable<Point> GetCollisionCheckPoints()
        {
            List<Point> points = new List<Point>();
            
            const int numPointsPerSide = 4;
            const int numPointsInside = 3;
            
            points.Add(new Point(X, Y));
            points.Add(new Point(X + Width, Y));
            points.Add(new Point(X, Y + Height));
            points.Add(new Point(X + Width, Y + Height));
            
            double stepX = Width / (numPointsPerSide + 1);
            double stepY = Height / (numPointsPerSide + 1);
            
            for (int i = 1; i <= numPointsPerSide; i++)
            {
                points.Add(new Point(X + stepX * i, Y));
            }
            
            for (int i = 1; i <= numPointsPerSide; i++)
            {
                points.Add(new Point(X + Width, Y + stepY * i));
            }
            
            for (int i = 1; i <= numPointsPerSide; i++)
            {
                points.Add(new Point(X + stepX * i, Y + Height));
            }
            
            for (int i = 1; i <= numPointsPerSide; i++)
            {
                points.Add(new Point(X, Y + stepY * i));
            }
            
            for (int i = 1; i <= numPointsInside; i++)
            {
                for (int j = 1; j <= numPointsInside; j++)
                {
                    double pointX = X + Width * i / (numPointsInside + 1);
                    double pointY = Y + Height * j / (numPointsInside + 1);
                    points.Add(new Point(pointX, pointY));
                }
            }
            
            points.Add(new Point(X + Width / 2, Y + Height / 2));
            
            return points;
        }
    }
}