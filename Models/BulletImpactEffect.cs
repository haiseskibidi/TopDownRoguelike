using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GunVault.GameEngine;

namespace GunVault.Models
{
    public class BulletImpactEffect
    {
        private List<UIElement> _particles;
        private double _lifetime;
        private double _maxLifetime;
        private Canvas _canvas;
        
        private const int PARTICLE_COUNT = 10;
        private const double PARTICLE_MAX_SPEED = 60.0;
        private const double PARTICLE_MIN_SPEED = 20.0;
        private const double PARTICLE_MAX_SIZE = 3.0;
        private const double PARTICLE_MIN_SIZE = 1.0;
        private const double EFFECT_LIFETIME = 0.5;
        
        public BulletImpactEffect(double x, double y, double angle, TileType tileType, Canvas canvas)
        {
            _particles = new List<UIElement>();
            _lifetime = 0;
            _maxLifetime = EFFECT_LIFETIME;
            _canvas = canvas;
            
            Color particleColor = GetParticleColor(tileType);
            CreateParticles(x, y, angle, particleColor);
        }
        
        private void CreateParticles(double x, double y, double angle, Color color)
        {
            Random random = new Random();
            
            double baseAngle = angle + Math.PI;
            double spreadAngle = Math.PI / 3;
            
            for (int i = 0; i < PARTICLE_COUNT; i++)
            {
                double particleAngle = baseAngle - spreadAngle / 2 + random.NextDouble() * spreadAngle;
                double speed = PARTICLE_MIN_SPEED + random.NextDouble() * (PARTICLE_MAX_SPEED - PARTICLE_MIN_SPEED);
                double size = PARTICLE_MIN_SIZE + random.NextDouble() * (PARTICLE_MAX_SIZE - PARTICLE_MIN_SIZE);
                double vx = Math.Cos(particleAngle) * speed;
                double vy = Math.Sin(particleAngle) * speed;
                
                Ellipse particle = new Ellipse
                {
                    Width = size,
                    Height = size,
                    Fill = new SolidColorBrush(color)
                };
                
                Canvas.SetLeft(particle, x - size / 2);
                Canvas.SetTop(particle, y - size / 2);
                _canvas.Children.Add(particle);
                Panel.SetZIndex(particle, 100);
                particle.Tag = new ParticleData { VelocityX = vx, VelocityY = vy };
                _particles.Add(particle);
            }
            
            Ellipse flash = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = new RadialGradientBrush
                {
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Colors.White, 0),
                        new GradientStop(Color.FromArgb(0, 255, 255, 255), 1)
                    }
                }
            };
            
            Canvas.SetLeft(flash, x - 10);
            Canvas.SetTop(flash, y - 10);
            _canvas.Children.Add(flash);
            _particles.Add(flash);
        }
        
        public bool Update(double deltaTime)
        {
            _lifetime += deltaTime;
            
            if (_lifetime >= _maxLifetime)
            {
                foreach (UIElement particle in _particles)
                {
                    _canvas.Children.Remove(particle);
                }
                _particles.Clear();
                return false;
            }
            
            double fadeRatio = 1 - (_lifetime / _maxLifetime);
            foreach (UIElement element in _particles)
            {
                if (element is Ellipse ellipse)
                {
                    if (ellipse.Tag is ParticleData data)
                    {
                        double left = Canvas.GetLeft(ellipse);
                        double top = Canvas.GetTop(ellipse);
                        Canvas.SetLeft(ellipse, left + data.VelocityX * deltaTime);
                        Canvas.SetTop(ellipse, top + data.VelocityY * deltaTime);
                        data.VelocityY += 98.0 * deltaTime;
                        data.VelocityX *= 0.95;
                        data.VelocityY *= 0.95;
                        
                        if (ellipse.Fill is SolidColorBrush brush)
                        {
                            Color color = brush.Color;
                            byte alpha = (byte)(color.A * fadeRatio);
                            ellipse.Fill = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
                        }
                        else if (ellipse.Fill is RadialGradientBrush radialBrush)
                        {
                            ellipse.Opacity = fadeRatio;
                        }
                    }
                    else
                    {
                        ellipse.Opacity = fadeRatio;
                    }
                }
            }
            
            return true;
        }
        
        private Color GetParticleColor(TileType tileType)
        {
            switch (tileType)
            {
                case TileType.Stone:
                    return Colors.DarkGray;
                case TileType.Dirt:
                    return Colors.SaddleBrown;
                case TileType.Grass:
                    return Colors.DarkGreen;
                case TileType.Sand:
                    return Colors.Tan;
                case TileType.Water:
                    return Colors.LightBlue;
                default:
                    return Colors.White;
            }
        }
        
        private class ParticleData
        {
            public double VelocityX { get; set; }
            public double VelocityY { get; set; }
        }
    }
}