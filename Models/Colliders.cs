using System;
using System.Collections.Generic;
using System.Windows;

namespace GunVault.Models
{
    /// <summary>
    /// Базовый класс для коллайдеров
    /// </summary>
    public abstract class Collider
    {
        public double X { get; protected set; }
        public double Y { get; protected set; }
        
        /// <summary>
        /// Обновляет позицию коллайдера
        /// </summary>
        public abstract void UpdatePosition(double x, double y);
        
        /// <summary>
        /// Проверяет пересечение с другим коллайдером
        /// </summary>
        public abstract bool Intersects(Collider other);
        
        /// <summary>
        /// Проверяет, содержит ли коллайдер указанную точку
        /// </summary>
        public abstract bool ContainsPoint(double x, double y);
        
        /// <summary>
        /// Возвращает точки для проверки коллизий
        /// </summary>
        public abstract IEnumerable<Point> GetCollisionCheckPoints();
    }

    /// <summary>
    /// Круглый коллайдер для игрока и врагов
    /// </summary>
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
                // Находим ближайшую к центру круга точку прямоугольника
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
            // Для круга проверяем центр и 8 точек по периметру
            List<Point> points = new List<Point>();
            
            // Центр
            points.Add(new Point(X, Y));
            
            // Точки по окружности (основные направления)
            points.Add(new Point(X + Radius, Y)); // Восток
            points.Add(new Point(X, Y + Radius)); // Юг
            points.Add(new Point(X - Radius, Y)); // Запад
            points.Add(new Point(X, Y - Radius)); // Север
            
            // Диагональные точки
            double diag = Radius * 0.7071; // sin(45°) * radius
            points.Add(new Point(X + diag, Y + diag)); // Юго-восток
            points.Add(new Point(X - diag, Y + diag)); // Юго-запад
            points.Add(new Point(X - diag, Y - diag)); // Северо-запад
            points.Add(new Point(X + diag, Y - diag)); // Северо-восток
            
            return points;
        }
    }

    /// <summary>
    /// Прямоугольный коллайдер для тайлов
    /// </summary>
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
                // Более надежный алгоритм проверки пересечения AABB (Axis-Aligned Bounding Box)
                // с учетом небольшого буфера для устранения проблем с граничными случаями
                const double epsilon = 0.001;
                
                // Используем систему проверки, которая менее чувствительна к ошибкам округления
                bool noOverlap = 
                    (X + Width + epsilon) <= rect.X ||   // Этот объект полностью слева от другого
                    (X >= rect.X + rect.Width + epsilon) ||   // Этот объект полностью справа от другого
                    (Y + Height + epsilon) <= rect.Y ||  // Этот объект полностью выше другого
                    (Y >= rect.Y + rect.Height + epsilon);    // Этот объект полностью ниже другого
                    
                return !noOverlap;
            }
            
            if (other is CircleCollider circle)
            {
                // Для круга используем специализированный алгоритм обнаружения пересечений
                
                // Находим ближайшую к центру круга точку на прямоугольнике
                double closestX = Math.Clamp(circle.X, X, X + Width);
                double closestY = Math.Clamp(circle.Y, Y, Y + Height);
                
                // Вычисляем квадрат расстояния от центра круга до ближайшей точки
                double distanceX = circle.X - closestX;
                double distanceY = circle.Y - closestY;
                double distanceSquared = distanceX * distanceX + distanceY * distanceY;
                
                // Круг пересекается с прямоугольником, если это расстояние меньше радиуса круга
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
            
            // Улучшенная проверка - больше точек для более точной коллизии
            const int numPointsPerSide = 4; // Количество точек на каждой стороне (не считая углы)
            const int numPointsInside = 3;  // Количество точек внутри (в каждом направлении)
            
            // Углы
            points.Add(new Point(X, Y)); // Верхний левый
            points.Add(new Point(X + Width, Y)); // Верхний правый
            points.Add(new Point(X, Y + Height)); // Нижний левый
            points.Add(new Point(X + Width, Y + Height)); // Нижний правый
            
            // Точки по периметру
            double stepX = Width / (numPointsPerSide + 1);
            double stepY = Height / (numPointsPerSide + 1);
            
            // Верхняя сторона
            for (int i = 1; i <= numPointsPerSide; i++)
            {
                points.Add(new Point(X + stepX * i, Y));
            }
            
            // Правая сторона
            for (int i = 1; i <= numPointsPerSide; i++)
            {
                points.Add(new Point(X + Width, Y + stepY * i));
            }
            
            // Нижняя сторона
            for (int i = 1; i <= numPointsPerSide; i++)
            {
                points.Add(new Point(X + stepX * i, Y + Height));
            }
            
            // Левая сторона
            for (int i = 1; i <= numPointsPerSide; i++)
            {
                points.Add(new Point(X, Y + stepY * i));
            }
            
            // Внутренние точки в сетке
            for (int i = 1; i <= numPointsInside; i++)
            {
                for (int j = 1; j <= numPointsInside; j++)
                {
                    double pointX = X + Width * i / (numPointsInside + 1);
                    double pointY = Y + Height * j / (numPointsInside + 1);
                    points.Add(new Point(pointX, pointY));
                }
            }
            
            // Центр
            points.Add(new Point(X + Width / 2, Y + Height / 2));
            
            return points;
        }
    }
} 