using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GunVault.Models
{
    public class Enemy
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Health { get; private set; }
        public double MaxHealth { get; private set; }
        public double Speed { get; private set; }
        public double Radius { get; private set; }
        public int ScoreValue { get; private set; }
        
        public Ellipse EnemyShape { get; private set; }
        public Rectangle HealthBar { get; private set; }
        
        public Enemy(double startX, double startY, double health, double speed, double radius, int scoreValue)
        {
            X = startX;
            Y = startY;
            MaxHealth = health;
            Health = MaxHealth;
            Speed = speed;
            Radius = radius;
            ScoreValue = scoreValue;
            
            EnemyShape = new Ellipse
            {
                Width = Radius * 2,
                Height = Radius * 2,
                Fill = Brushes.Red,
                Stroke = Brushes.DarkRed,
                StrokeThickness = 2
            };
            
            HealthBar = new Rectangle
            {
                Width = Radius * 2,
                Height = 5,
                Fill = Brushes.Green
            };
            
            UpdatePosition();
        }
        
        public void UpdatePosition()
        {
            Canvas.SetLeft(EnemyShape, X - Radius);
            Canvas.SetTop(EnemyShape, Y - Radius);
            
            Canvas.SetLeft(HealthBar, X - Radius);
            Canvas.SetTop(HealthBar, Y - Radius - 10);
            
            HealthBar.Width = (Health / MaxHealth) * (Radius * 2);
        }
        
        public void MoveTowardsPlayer(double playerX, double playerY, double deltaTime)
        {
            double dx = playerX - X;
            double dy = playerY - Y;
            
            double length = Math.Sqrt(dx * dx + dy * dy);
            
            if (length > Radius)
            {
                dx /= length;
                dy /= length;
                
                X += dx * Speed * deltaTime;
                Y += dy * Speed * deltaTime;
                
                UpdatePosition();
            }
        }
        
        public bool TakeDamage(double damage)
        {
            Health = Math.Max(0, Health - damage);
            UpdatePosition();
            
            return Health > 0;
        }
        
        public bool CollidesWithPlayer(Player player)
        {
            double dx = X - player.X;
            double dy = Y - player.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            
            return distance < Radius + 20;
        }
    }
} 