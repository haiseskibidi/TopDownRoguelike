using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Shapes;
using System.Windows;

namespace GunVault.GameEngine
{
    public class SpriteManager
    {
        private Dictionary<string, BitmapImage> _spriteCache;
        private string _spritesFolder;
        private bool _initialized;
        
        public SpriteManager(string spritesFolder = "Sprites")
        {
            _spriteCache = new Dictionary<string, BitmapImage>();
            _initialized = false;
            
            try
            {
                string projectDir = Directory.GetCurrentDirectory();
                _spritesFolder = System.IO.Path.Combine(projectDir, spritesFolder);
                
                if (!Directory.Exists(_spritesFolder))
                {
                    string exePath = AppDomain.CurrentDomain.BaseDirectory;
                    _spritesFolder = System.IO.Path.Combine(exePath, spritesFolder);
                    
                    if (!Directory.Exists(_spritesFolder))
                    {
                        string parentDir = Directory.GetParent(exePath)?.FullName ?? exePath;
                        _spritesFolder = System.IO.Path.Combine(parentDir, spritesFolder);
                        
                        if (!Directory.Exists(_spritesFolder))
                        {
                            string grandParentDir = Directory.GetParent(parentDir)?.FullName ?? parentDir;
                            _spritesFolder = System.IO.Path.Combine(grandParentDir, spritesFolder);
                            
                            if (!Directory.Exists(_spritesFolder))
                            {
                                _spritesFolder = System.IO.Path.Combine(exePath, spritesFolder);
                                Console.WriteLine($"Предупреждение: Папка со спрайтами не найдена: {spritesFolder}");
                                return;
                            }
                        }
                    }
                }
                
                _initialized = true;
                Console.WriteLine($"Папка со спрайтами найдена: {_spritesFolder}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при инициализации менеджера спрайтов: {ex.Message}");
            }
        }
        
        public BitmapImage LoadSprite(string spriteName)
        {
            try
            {
                if (_spriteCache.ContainsKey(spriteName))
                {
                    return _spriteCache[spriteName];
                }
                
                if (!_initialized)
                {
                    Console.WriteLine($"Менеджер спрайтов не инициализирован, невозможно загрузить: {spriteName}");
                    return null;
                }
                
                string filePath = System.IO.Path.Combine(_spritesFolder, $"{spriteName}.png");
                
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Файл спрайта не найден: {filePath}");
                    return null;
                }
                
                Console.WriteLine($"Загружаю спрайт: {filePath}");
                
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                
                _spriteCache[spriteName] = bitmap;
                
                return bitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке спрайта {spriteName}: {ex.Message}");
                return null;
            }
        }
        
        public UIElement CreateSpriteImage(string spriteName, double width, double height)
        {
            try
            {
                BitmapImage sprite = LoadSprite(spriteName);
                
                if (sprite == null)
                {
                    Console.WriteLine($"Создаю запасную форму для спрайта {spriteName} (размеры: {width}x{height})");
                    return CreateFallbackShape(width, height, spriteName);
                }
                
                Image image = new Image
                {
                    Source = sprite,
                    Width = width,
                    Height = height,
                    Stretch = Stretch.Uniform
                };
                
                return image;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании спрайта {spriteName}: {ex.Message}");
                return CreateFallbackShape(width, height, spriteName);
            }
        }
        
        private UIElement CreateFallbackShape(double width, double height, string spriteName = null)
        {
            Brush fillBrush = Brushes.DarkGray;
            
            if (spriteName != null)
            {
                switch (spriteName.ToLower())
                {
                    // Оружие
                    case "pistol": fillBrush = Brushes.DarkGray; break;
                    case "shotgun": fillBrush = Brushes.Brown; break;
                    case "assault_rifle": fillBrush = Brushes.Green; break;
                    case "sniper": fillBrush = Brushes.DarkBlue; break;
                    case "machine_gun": fillBrush = Brushes.DarkGreen; break;
                    case "rocket_launcher": fillBrush = Brushes.Red; break;
                    case "laser": fillBrush = Brushes.Purple; break;
                    
                    // Персонажи
                    case "enemy1": fillBrush = Brushes.Red; break;
                    case "player": fillBrush = Brushes.Blue; break;
                    
                    // Тайлы
                    case "grass1": fillBrush = Brushes.ForestGreen; break;
                    case "dirt1": fillBrush = Brushes.SaddleBrown; break;
                    case "water1": fillBrush = Brushes.DodgerBlue; break;
                    case "stone1": fillBrush = Brushes.Gray; break;
                    case "sand1": fillBrush = Brushes.Khaki; break;
                }
            }
            
            return new Rectangle
            {
                Width = width,
                Height = height,
                Fill = fillBrush,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
        }
    }
} 