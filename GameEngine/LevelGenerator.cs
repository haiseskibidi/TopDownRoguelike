using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GunVault.Models;

namespace GunVault.GameEngine
{
    public class LevelGenerator
    {
        private Canvas _gameCanvas;
        private double _gameWidth;
        private double _gameHeight;
        private SpriteManager _spriteManager;
        private Random _random;
        private List<UIElement> _groundTiles;
        private Dictionary<TileType, List<UIElement>> _tilesByType;
        private TileType[,] _tileMap;
        private Dictionary<string, RectCollider> _tileColliders;
        private const double COLLIDER_SIZE_FACTOR = 0.98;
        private int _mapSeed;
        private bool _isFirstGeneration = true;
        private List<UIElement> _tileColliderVisuals;
        private bool _showTileColliders = false;
        
        public LevelGenerator(Canvas gameCanvas, double gameWidth, double gameHeight, SpriteManager spriteManager)
        {
            _gameCanvas = gameCanvas;
            _gameWidth = gameWidth;
            _gameHeight = gameHeight;
            _spriteManager = spriteManager;
            _random = new Random();
            _groundTiles = new List<UIElement>();
            _tilesByType = new Dictionary<TileType, List<UIElement>>();
            _tileColliders = new Dictionary<string, RectCollider>();
            _tileColliderVisuals = new List<UIElement>();
            
            foreach (TileType type in Enum.GetValues(typeof(TileType)))
            {
                _tilesByType[type] = new List<UIElement>();
            }
        }

        public void GenerateLevel()
        {
            ClearLevel();

            int mapWidth = (int)Math.Ceiling(_gameWidth / TileSettings.TILE_SIZE) + 3;
            int mapHeight = (int)Math.Ceiling(_gameHeight / TileSettings.TILE_SIZE) + 3;
            
            if (_isFirstGeneration)
            {
                _mapSeed = new Random().Next();
                _isFirstGeneration = false;
                Console.WriteLine($"Создан новый сид карты: {_mapSeed}");
            }
            else
            {
                Console.WriteLine($"Используем существующий сид карты: {_mapSeed}");
            }
            
            MapGenerator mapGenerator = new MapGenerator(mapWidth, mapHeight, _mapSeed);
            _tileMap = mapGenerator.Generate();
            
            double tilePlacementSize = TileSettings.TILE_SIZE - TileSettings.TILE_OVERLAP;
            
            _tileColliders.Clear();
            
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    double xPos = x * tilePlacementSize - TileSettings.TILE_OVERLAP;
                    double yPos = y * tilePlacementSize - TileSettings.TILE_OVERLAP;
                    
                    TileType tileType = _tileMap[x, y];
                    TileInfo tileInfo = TileSettings.TileInfos[tileType];
                    
                    UIElement tile = CreateTile(xPos, yPos, tileInfo.SpriteName);
                    if (tile != null)
                    {
                        _gameCanvas.Children.Insert(0, tile);
                        Panel.SetZIndex(tile, -10);
                        _groundTiles.Add(tile);
                        _tilesByType[tileType].Add(tile);
                        
                        if (!tileInfo.IsWalkable)
                        {
                            double colliderSize = TileSettings.TILE_SIZE * COLLIDER_SIZE_FACTOR;
                            double colliderOffset = (TileSettings.TILE_SIZE - colliderSize) / 2.0;
                            
                            RectCollider collider = new RectCollider(
                                xPos + colliderOffset,
                                yPos + colliderOffset,
                                colliderSize,
                                colliderSize);
                            
                            string colliderKey = $"{x}:{y}";
                            _tileColliders[colliderKey] = collider;
                        }
                    }
                }
            }
            
            Console.WriteLine($"Сгенерировано {_groundTiles.Count} тайлов, из них:");
            foreach (var tileType in _tilesByType.Keys)
            {
                Console.WriteLine($"- {tileType}: {_tilesByType[tileType].Count} штук");
            }
            Console.WriteLine($"Создано {_tileColliders.Count} коллайдеров для непроходимых тайлов");
        }

        private UIElement CreateTile(double x, double y, string spriteName)
        {
            try
            {
                UIElement tile = _spriteManager.CreateSpriteImage(spriteName, TileSettings.TILE_SIZE, TileSettings.TILE_SIZE);
                
                Canvas.SetLeft(tile, x);
                Canvas.SetTop(tile, y);
                
                if (_random.NextDouble() > 0.5)
                {
                    ScaleTransform flipTransform = new ScaleTransform(-1, 1);
                    tile.RenderTransform = flipTransform;
                    tile.RenderTransformOrigin = new Point(0.5, 0.5);
                }
                
                return tile;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании тайла {spriteName}: {ex.Message}");
                return null;
            }
        }

        public void ToggleTileColliders(bool show)
        {
            _showTileColliders = show;
            if (show)
            {
                ShowTileColliders();
            }
            else
            {
                HideTileColliders();
            }
        }
        
        private void ShowTileColliders()
        {
            HideTileColliders();
            
            foreach (var collider in _tileColliders.Values)
            {
                Rectangle visual = new Rectangle
                {
                    Width = collider.Width,
                    Height = collider.Height,
                    Stroke = Brushes.Red,
                    StrokeThickness = 2.5,
                    Fill = new SolidColorBrush(Color.FromArgb(120, 255, 0, 0))
                };
                
                Ellipse centerPoint = new Ellipse
                {
                    Width = 4,
                    Height = 4,
                    Fill = Brushes.Yellow,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                
                Canvas.SetLeft(visual, collider.X);
                Canvas.SetTop(visual, collider.Y);
                Canvas.SetLeft(centerPoint, collider.X + collider.Width / 2 - 2);
                Canvas.SetTop(centerPoint, collider.Y + collider.Height / 2 - 2);
                
                Panel.SetZIndex(visual, 9000); 
                Panel.SetZIndex(centerPoint, 9001);
                
                _gameCanvas.Children.Add(visual);
                _gameCanvas.Children.Add(centerPoint);
                _tileColliderVisuals.Add(visual);
                _tileColliderVisuals.Add(centerPoint);
            }
            
            Console.WriteLine($"Отображено {_tileColliders.Count} коллайдеров тайлов");
        }
        
        private void HideTileColliders()
        {
            foreach (var visual in _tileColliderVisuals)
            {
                _gameCanvas.Children.Remove(visual);
            }
            _tileColliderVisuals.Clear();
        }
        
        public void ClearLevel()
        {
            foreach (UIElement tile in _groundTiles)
            {
                _gameCanvas.Children.Remove(tile);
            }
            _groundTiles.Clear();
            
            foreach (var tileList in _tilesByType.Values)
            {
                tileList.Clear();
            }
            
            _tileColliders.Clear();
            
            HideTileColliders();
        }
        
        public void ResizeLevel(double newWidth, double newHeight)
        {
            bool wasVisible = _showTileColliders;

            if (_tileMap != null && Math.Abs(_gameWidth - newWidth) < 50 && Math.Abs(_gameHeight - newHeight) < 50)
            {
                Console.WriteLine("Незначительное изменение размера, сохраняем текущую карту");
                _gameWidth = newWidth;
                _gameHeight = newHeight;
                return;
            }
            
            _gameWidth = newWidth;
            _gameHeight = newHeight;
            
            GenerateLevel();
            
            if (!wasVisible)
            {
                HideTileColliders();
                _showTileColliders = false;
            }
        }
        
        public Dictionary<string, RectCollider> GetTileColliders()
        {
            return _tileColliders;
        }
        
        public RectCollider GetTileCollider(string key)
        {
            if (_tileColliders.TryGetValue(key, out RectCollider collider))
            {
                return collider;
            }
            return null;
        }

        public bool IsTileWalkable(double x, double y)
        {
            int tileX = (int)Math.Floor(x / TileSettings.TILE_SIZE) + 1;
            int tileY = (int)Math.Floor(y / TileSettings.TILE_SIZE) + 1;
            
            if (tileX < 0 || tileX >= _tileMap.GetLength(0) || tileY < 0 || tileY >= _tileMap.GetLength(1))
            {
                return false;
                return false; // За пределами карты считается непроходимым
            }
            
            // Получаем тип тайла и проверяем его проходимость
            TileType tileType = _tileMap[tileX, tileY];
            if (TileSettings.TileInfos.TryGetValue(tileType, out TileInfo tileInfo))
            {
                return tileInfo.IsWalkable;
            }
            
            return false; // Неизвестный тип тайла считается непроходимым
        }
        
        /// <summary>
        /// Проверяет, является ли вся указанная область проходимой
        /// </summary>
        public bool IsAreaWalkable(RectCollider playerCollider)
        {
            // Для оптимизации используем только коллайдеры из ближайших чанков
            Dictionary<string, RectCollider> nearbyColliders = GetNearbyTileColliders(playerCollider.X, playerCollider.Y);
            
            foreach (var collider in nearbyColliders.Values)
            {
                // Проверяем пересечение с каждым коллайдером тайла
                if (playerCollider.Intersects(collider))
                {
                    return false; // Если есть пересечение, область непроходима
                }
            }
            
            return true; // Нет пересечений, область проходима
        }

        /// <summary>
        /// Проверяет, видимы ли коллайдеры тайлов
        /// </summary>
        public bool AreColliderVisible()
        {
            return _showTileColliders;
        }
        
        /// <summary>
        /// Возвращает коллайдеры тайлов вблизи указанной точки
        /// </summary>
        public Dictionary<string, RectCollider> GetNearbyTileColliders(double x, double y)
        {
            // Рассчитываем границы поиска (примерно 3-4 тайла вокруг точки)
            double searchDistance = TileSettings.TILE_SIZE * 4;
            int minTileX = (int)Math.Floor((x - searchDistance) / TileSettings.TILE_SIZE);
            int maxTileX = (int)Math.Ceiling((x + searchDistance) / TileSettings.TILE_SIZE);
            int minTileY = (int)Math.Floor((y - searchDistance) / TileSettings.TILE_SIZE);
            int maxTileY = (int)Math.Ceiling((y + searchDistance) / TileSettings.TILE_SIZE);
            
            // Ограничиваем границы поиска размерами карты
            minTileX = Math.Max(0, minTileX);
            maxTileX = Math.Min(_tileMap.GetLength(0) - 1, maxTileX);
            minTileY = Math.Max(0, minTileY);
            maxTileY = Math.Min(_tileMap.GetLength(1) - 1, maxTileY);
            
            // Собираем коллайдеры в указанном диапазоне
            Dictionary<string, RectCollider> nearbyColliders = new Dictionary<string, RectCollider>();
            
            for (int y1 = minTileY; y1 <= maxTileY; y1++)
            {
                for (int x1 = minTileX; x1 <= maxTileX; x1++)
                {
                    string key = $"{x1}:{y1}";
                    if (_tileColliders.TryGetValue(key, out RectCollider collider))
                    {
                        nearbyColliders.Add(key, collider);
                    }
                }
            }
            
            return nearbyColliders;
        }
        
        /// <summary>
        /// Возвращает тип тайла по ключу коллайдера
        /// </summary>
        /// <param name="colliderKey">Ключ коллайдера в формате "x:y"</param>
        /// <returns>Тип тайла</returns>
        public TileType GetTileTypeAt(string colliderKey)
        {
            // Разбираем ключ коллайдера для получения координат тайла
            string[] parts = colliderKey.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
            {
                // Проверяем, что индексы в пределах размеров карты
                if (x >= 0 && x < _tileMap.GetLength(0) && y >= 0 && y < _tileMap.GetLength(1))
                {
                    // Возвращаем тип тайла из карты
                    return _tileMap[x, y];
                }
            }
            
            // По умолчанию возвращаем пустой тайл (проходимый)
            return TileType.Grass;
        }

        /// <summary>
        /// Возвращает тип тайла по координатам в тайловой сетке
        /// </summary>
        /// <param name="tileX">Координата X в сетке тайлов</param>
        /// <param name="tileY">Координата Y в сетке тайлов</param>
        /// <returns>Тип тайла или Grass по умолчанию</returns>
        public TileType GetTileType(int tileX, int tileY)
        {
            if (_tileMap == null || tileX < 0 || tileY < 0 || tileX >= _tileMap.GetLength(0) || tileY >= _tileMap.GetLength(1))
            {
                return TileType.Grass; // Возвращаем траву по умолчанию, если координаты вне карты
            }
            
            return _tileMap[tileX, tileY];
        }
    }
} 