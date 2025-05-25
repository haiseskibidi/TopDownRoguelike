using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GunVault.GameEngine;

namespace GunVault.Models
{
    /// <summary>
    /// Класс для создания эффекта попадания пули в препятствие
    /// </summary>
    public class BulletImpactEffect
    {
        private List<UIElement> _particles;
        private double _lifetime;
        private double _maxLifetime;
        private Canvas _canvas;
        
        // Настраиваемые параметры эффекта
        private const int PARTICLE_COUNT = 10; // Количество частиц
        private const double PARTICLE_MAX_SPEED = 60.0; // Максимальная скорость частиц
        private const double PARTICLE_MIN_SPEED = 20.0; // Минимальная скорость частиц
        private const double PARTICLE_MAX_SIZE = 3.0; // Максимальный размер частиц
        private const double PARTICLE_MIN_SIZE = 1.0; // Минимальный размер частиц
        private const double EFFECT_LIFETIME = 0.5; // Время жизни эффекта в секундах
        
        public BulletImpactEffect(double x, double y, double angle, TileType tileType, Canvas canvas)
        {
            _particles = new List<UIElement>();
            _lifetime = 0;
            _maxLifetime = EFFECT_LIFETIME;
            _canvas = canvas;
            
            // Создаем цвет частиц в зависимости от типа тайла
            Color particleColor = GetParticleColor(tileType);
            
            // Создаем частицы
            CreateParticles(x, y, angle, particleColor);
        }
        
        /// <summary>
        /// Создает частицы для эффекта
        /// </summary>
        private void CreateParticles(double x, double y, double angle, Color color)
        {
            Random random = new Random();
            
            // Угол разброса частиц: в обратном направлении от угла попадания +/- 60 градусов
            double baseAngle = angle + Math.PI; // Базовый угол - в обратном направлении от попадания
            double spreadAngle = Math.PI / 3; // +/- 60 градусов
            
            for (int i = 0; i < PARTICLE_COUNT; i++)
            {
                // Выбираем случайный угол в диапазоне разброса
                double particleAngle = baseAngle - spreadAngle / 2 + random.NextDouble() * spreadAngle;
                
                // Выбираем случайную скорость и размер
                double speed = PARTICLE_MIN_SPEED + random.NextDouble() * (PARTICLE_MAX_SPEED - PARTICLE_MIN_SPEED);
                double size = PARTICLE_MIN_SIZE + random.NextDouble() * (PARTICLE_MAX_SIZE - PARTICLE_MIN_SIZE);
                
                // Скорость частицы по осям
                double vx = Math.Cos(particleAngle) * speed;
                double vy = Math.Sin(particleAngle) * speed;
                
                // Создаем частицу
                Ellipse particle = new Ellipse
                {
                    Width = size,
                    Height = size,
                    Fill = new SolidColorBrush(color)
                };
                
                // Позиционируем частицу
                Canvas.SetLeft(particle, x - size / 2);
                Canvas.SetTop(particle, y - size / 2);
                
                // Добавляем частицу на canvas
                _canvas.Children.Add(particle);
                Panel.SetZIndex(particle, 100); // Частицы над остальными объектами
                
                // Сохраняем данные о частице
                particle.Tag = new ParticleData { VelocityX = vx, VelocityY = vy };
                
                _particles.Add(particle);
            }
            
            // Добавляем световую вспышку
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
        
        /// <summary>
        /// Обновляет состояние эффекта
        /// </summary>
        /// <param name="deltaTime">Прошедшее время в секундах</param>
        /// <returns>true, если эффект все еще активен</returns>
        public bool Update(double deltaTime)
        {
            _lifetime += deltaTime;
            
            if (_lifetime >= _maxLifetime)
            {
                // Время жизни истекло - удаляем все частицы
                foreach (UIElement particle in _particles)
                {
                    _canvas.Children.Remove(particle);
                }
                _particles.Clear();
                return false;
            }
            
            // Обновляем позиции частиц и применяем затухание
            double fadeRatio = 1 - (_lifetime / _maxLifetime);
            foreach (UIElement element in _particles)
            {
                if (element is Ellipse ellipse)
                {
                    if (ellipse.Tag is ParticleData data)
                    {
                        // Получаем текущую позицию
                        double left = Canvas.GetLeft(ellipse);
                        double top = Canvas.GetTop(ellipse);
                        
                        // Обновляем позицию
                        Canvas.SetLeft(ellipse, left + data.VelocityX * deltaTime);
                        Canvas.SetTop(ellipse, top + data.VelocityY * deltaTime);
                        
                        // Применяем гравитацию
                        data.VelocityY += 98.0 * deltaTime;
                        
                        // Постепенно уменьшаем скорость (трение)
                        data.VelocityX *= 0.95;
                        data.VelocityY *= 0.95;
                        
                        // Применяем затухание прозрачности
                        if (ellipse.Fill is SolidColorBrush brush)
                        {
                            Color color = brush.Color;
                            byte alpha = (byte)(color.A * fadeRatio);
                            ellipse.Fill = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
                        }
                        else if (ellipse.Fill is RadialGradientBrush radialBrush)
                        {
                            // Для вспышки просто меняем прозрачность
                            ellipse.Opacity = fadeRatio;
                        }
                    }
                    else
                    {
                        // Для элементов без данных о скорости (вспышка) просто затухаем
                        ellipse.Opacity = fadeRatio;
                    }
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Возвращает цвет частиц в зависимости от типа тайла
        /// </summary>
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
        
        /// <summary>
        /// Вспомогательный класс для хранения данных о частице
        /// </summary>
        private class ParticleData
        {
            public double VelocityX { get; set; }
            public double VelocityY { get; set; }
        }
    }
} 