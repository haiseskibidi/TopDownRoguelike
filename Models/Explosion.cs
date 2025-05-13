using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GunVault.Models
{
    public class Explosion
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public double CurrentRadius { get; private set; }
        public double MaxRadius { get; private set; }
        public double ExpansionSpeed { get; private set; }
        public double Damage { get; private set; }
        public bool IsActive { get; private set; }
        
        public Ellipse ExplosionShape { get; private set; }
        
        public Explosion(double x, double y, double maxRadius, double expansionSpeed, double damage)
        {
            X = x;
            Y = y;
            CurrentRadius = 0;
            MaxRadius = maxRadius;
            ExpansionSpeed = expansionSpeed;
            Damage = damage;
            IsActive = true;
            
            ExplosionShape = new Ellipse
            {
                Width = CurrentRadius * 2,
                Height = CurrentRadius * 2,
                Fill = new SolidColorBrush(Color.FromArgb(128, 255, 165, 0)),
                Stroke = Brushes.Red,
                StrokeThickness = 2
            };
            
            UpdatePosition();
        }
        
        private void UpdatePosition()
        {
            Canvas.SetLeft(ExplosionShape, X - CurrentRadius);
            Canvas.SetTop(ExplosionShape, Y - CurrentRadius);
            ExplosionShape.Width = CurrentRadius * 2;
            ExplosionShape.Height = CurrentRadius * 2;
        }
        
        public bool Update(double deltaTime)
        {
            if (!IsActive)
                return false;
            
            CurrentRadius += ExpansionSpeed * deltaTime;
            
            UpdatePosition();
            
            if (CurrentRadius >= MaxRadius)
            {
                IsActive = false;
                return false;
            }
            
            return true;
        }
        
        public bool AffectsEnemy(Enemy enemy)
        {
            double dx = X - enemy.X;
            double dy = Y - enemy.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            
            return distance <= CurrentRadius + enemy.Radius;
        }
    }
} 