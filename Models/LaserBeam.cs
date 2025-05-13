using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GunVault.Models
{
    public class LaserBeam
    {
        public double StartX { get; private set; }
        public double StartY { get; private set; }
        public double EndX { get; private set; }
        public double EndY { get; private set; }
        public double Angle { get; private set; }
        public double Damage { get; private set; }
        public double MaxLength { get; private set; }
        public double TimeToLive { get; private set; }
        
        public Line LaserLine { get; private set; }
        public Ellipse LaserDot { get; private set; }
        
        public LaserBeam(double startX, double startY, double angle, double damage, double maxLength)
        {
            StartX = startX;
            StartY = startY;
            Angle = angle;
            Damage = damage;
            MaxLength = maxLength;
            TimeToLive = 0.2;
            
            EndX = StartX + Math.Cos(angle) * maxLength;
            EndY = StartY + Math.Sin(angle) * maxLength;
            
            LaserLine = new Line
            {
                X1 = StartX,
                Y1 = StartY,
                X2 = EndX,
                Y2 = EndY,
                Stroke = new SolidColorBrush(Colors.Magenta),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 1, 1 },
                Opacity = 0.8
            };
            
            LaserDot = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = Brushes.White,
                Stroke = Brushes.Magenta,
                StrokeThickness = 1
            };
            
            Canvas.SetLeft(LaserDot, EndX - 4);
            Canvas.SetTop(LaserDot, EndY - 4);
        }
        
        public bool Update(double deltaTime)
        {
            TimeToLive -= deltaTime;
            
            double opacity = TimeToLive / 0.2;
            LaserLine.Opacity = opacity;
            LaserDot.Opacity = opacity;
            
            return TimeToLive > 0;
        }
        
        public bool IntersectsWithEnemy(Enemy enemy, out double distance)
        {
            double dx = EndX - StartX;
            double dy = EndY - StartY;
            double lineLength = Math.Sqrt(dx * dx + dy * dy);
            
            if (lineLength == 0)
            {
                distance = 0;
                return false;
            }
            
            dx /= lineLength;
            dy /= lineLength;
            
            double vx = enemy.X - StartX;
            double vy = enemy.Y - StartY;
            
            double projection = vx * dx + vy * dy;
            
            if (projection < 0)
            {
                distance = double.MaxValue;
                return false;
            }
            
            if (projection > lineLength)
            {
                distance = double.MaxValue;
                return false;
            }
            
            double closestX = StartX + dx * projection;
            double closestY = StartY + dy * projection;
            
            double distanceToLine = Math.Sqrt(Math.Pow(closestX - enemy.X, 2) + Math.Pow(closestY - enemy.Y, 2));
            
            distance = projection;
            
            return distanceToLine <= enemy.Radius;
        }
        
        public void SetEndPoint(double endX, double endY)
        {
            EndX = endX;
            EndY = endY;
            
            LaserLine.X2 = EndX;
            LaserLine.Y2 = EndY;
            
            Canvas.SetLeft(LaserDot, EndX - 4);
            Canvas.SetTop(LaserDot, EndY - 4);
        }
    }
} 