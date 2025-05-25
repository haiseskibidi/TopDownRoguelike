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
        
        // Карта с типами тайлов
        private TileType[,] _tileMap;
        
        // Коллайдеры для непроходимых тайлов
        private Dictionary<string, RectCollider> _tileColliders;
        
        // Внутренний размер коллайдера тайла (практически равен визуальному размеру)
        private const double COLLIDER_SIZE_FACTOR = 0.98;
        
        // Сид для генератора карты, чтобы можно было воссоздать ту же карту
        private int _mapSeed;
        private bool _isFirstGeneration = true;
        
        private List<UIElement> _tileColliderVisuals; // Изменен тип с List<Rectangle> на List<UIElement>
        private bool _showTileColliders = false; // Скрываем коллайдеры по умолчанию
        
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
            
            // Инициализируем словарь для хранения тайлов каждого типа
            foreach (TileType type in Enum.GetValues(typeof(TileType)))
            {
                _tilesByType[type] = new List<UIElement>();
            }
        }

        /// <summary>
        /// Генерирует уровень с использованием цепей Маркова и клеточных автоматов
        /// </summary>
        public void GenerateLevel()
        {
            // Удаляем предыдущие тайлы, если есть
            ClearLevel();

            // Вычисляем размер карты в тайлах
            int mapWidth = (int)Math.Ceiling(_gameWidth / TileSettings.TILE_SIZE) + 3;  // +3 для перекрытия краев
            int mapHeight = (int)Math.Ceiling(_gameHeight / TileSettings.TILE_SIZE) + 3;
            
            // При первой генерации создаем случайный сид для карты
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
            
            // Генерируем новую карту с использованием сохраненного сида
            MapGenerator mapGenerator = new MapGenerator(mapWidth, mapHeight, _mapSeed);
            _tileMap = mapGenerator.Generate();
            
            // Размер одиночного тайла с учетом перекрытия
            double tilePlacementSize = TileSettings.TILE_SIZE - TileSettings.TILE_OVERLAP;
            
            // Очищаем коллайдеры
            _tileColliders.Clear();
            
            // Размещаем тайлы на карте согласно сгенерированной карте типов тайлов
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    // Вычисляем позицию с перекрытием
                    double xPos = x * tilePlacementSize - TileSettings.TILE_OVERLAP;
                    double yPos = y * tilePlacementSize - TileSettings.TILE_OVERLAP;
                    
                    // Получаем тип тайла из карты
                    TileType tileType = _tileMap[x, y];
                    TileInfo tileInfo = TileSettings.TileInfos[tileType];
                    
                    // Создаем тайл нужного типа
                    UIElement tile = CreateTile(xPos, yPos, tileInfo.SpriteName);
                    if (tile != null)
                    {
                        // Добавляем тайл на canvas и в соответствующие списки
                        _gameCanvas.Children.Insert(0, tile);
                        Panel.SetZIndex(tile, -10);
                        _groundTiles.Add(tile);
                        _tilesByType[tileType].Add(tile);
                        
                        // Если тайл непроходимый, создаем для него коллайдер
                        if (!tileInfo.IsWalkable)
                        {
                            // Размер коллайдера практически равен визуальному тайлу для предотвращения проскакивания пуль
                            double colliderSize = TileSettings.TILE_SIZE * COLLIDER_SIZE_FACTOR;
                            double colliderOffset = (TileSettings.TILE_SIZE - colliderSize) / 2.0;
                            
                            // Создаем коллайдер, центрированный в середине тайла
                            RectCollider collider = new RectCollider(
                                xPos + colliderOffset,
                                yPos + colliderOffset,
                                colliderSize,
                                colliderSize);
                            
                            // Сохраняем коллайдер с уникальным ключом по координатам
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
            
            // Убираем код отображения коллайдеров полностью
        }

        /// <summary>
        /// Создает тайл с указанным спрайтом и позицией
        /// </summary>
        private UIElement CreateTile(double x, double y, string spriteName)
        {
            try
            {
                UIElement tile = _spriteManager.CreateSpriteImage(spriteName, TileSettings.TILE_SIZE, TileSettings.TILE_SIZE);
                
                Canvas.SetLeft(tile, x);
                Canvas.SetTop(tile, y);
                
                // Случайное отзеркаливание по горизонтали для разнообразия
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

        /// <summary>
        /// Включает или отключает отображение коллайдеров тайлов
        /// </summary>
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
        
        /// <summary>
        /// Показывает коллайдеры тайлов для отладки
        /// </summary>
        private void ShowTileColliders()
        {
            // Удаляем предыдущие визуализации, если есть
            HideTileColliders();
            
            // Создаем визуализации для каждого коллайдера
            foreach (var collider in _tileColliders.Values)
            {
                Rectangle visual = new Rectangle
                {
                    Width = collider.Width,
                    Height = collider.Height,
                    Stroke = Brushes.Red,
                    StrokeThickness = 2.5,  // Увеличиваем толщину обводки
                    Fill = new SolidColorBrush(Color.FromArgb(120, 255, 0, 0)) // Более яркий и прозрачный красный
                };
                
                // Отображаем центральную точку коллайдера для визуализации симметричности
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
        
        /// <summary>
        /// Скрывает коллайдеры тайлов
        /// </summary>
        private void HideTileColliders()
        {
            foreach (var visual in _tileColliderVisuals)
            {
                _gameCanvas.Children.Remove(visual);
            }
            _tileColliderVisuals.Clear();
        }
        
        /// <summary>
        /// Очистка уровня
        /// </summary>
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
            
            // Скрываем коллайдеры тайлов при очистке уровня
            HideTileColliders();
        }
        
        /// <summary>
        /// Обновление размеров уровня при изменении размера окна
        /// </summary>
        public void ResizeLevel(double newWidth, double newHeight)
        {
            // Запоминаем текущее состояние видимости коллайдеров
            bool wasVisible = _showTileColliders;

            // Если карта уже сгенерирована и изменения размера небольшие, просто обновляем размеры
            if (_tileMap != null && Math.Abs(_gameWidth - newWidth) < 50 && Math.Abs(_gameHeight - newHeight) < 50)
            {
                Console.WriteLine("Незначительное изменение размера, сохраняем текущую карту");
                _gameWidth = newWidth;
                _gameHeight = newHeight;
                return;
            }
            
            _gameWidth = newWidth;
            _gameHeight = newHeight;
            
            // Перегенерируем уровень с новыми размерами только при значительных изменениях
            GenerateLevel();
            
            // Если коллайдеры не должны быть видны, скрываем их
            if (!wasVisible)
            {
                HideTileColliders();
                _showTileColliders = false;
            }
        }
        
        /// <summary>
        /// Проверяет, является ли тайл с указанными координатами проходимым
        /// </summary>
        public bool IsTileWalkable(double x, double y)
        {
            // Получаем индексы тайла по мировым координатам
            int tileX = (int)Math.Floor(x / TileSettings.TILE_SIZE);
            int tileY = (int)Math.Floor(y / TileSettings.TILE_SIZE);
            
            // Проверяем выход за границы карты
            if (tileX < 0 || tileX >= _tileMap.GetLength(0) || tileY < 0 || tileY >= _tileMap.GetLength(1))
            {
                return false;
            }
            
            // Проверяем проходимость тайла по его типу
            TileType tileType = _tileMap[tileX, tileY];
            
            // Если тайл непроходимый по своему типу, сразу возвращаем false
            if (!TileSettings.TileInfos[tileType].IsWalkable)
            {
                // Для отладки
                // Console.WriteLine($"Тайл {tileType} в ({tileX}, {tileY}) непроходим по типу");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Более точная проверка коллизии с коллайдером игрока
        /// </summary>
        public bool IsAreaWalkable(RectCollider playerCollider)
        {
            // Оптимизация: проверяем только те тайлы, которые находятся рядом с игроком
            // Это значительно снижает нагрузку на CPU при большом количестве тайлов
            
            // Рассчитываем индексы тайлов, близких к игроку
            double expandedRadius = Math.Max(playerCollider.Width, playerCollider.Height) + TileSettings.TILE_SIZE;
            int minTileX = (int)Math.Floor((playerCollider.X - expandedRadius) / TileSettings.TILE_SIZE);
            int maxTileX = (int)Math.Ceiling((playerCollider.X + playerCollider.Width + expandedRadius) / TileSettings.TILE_SIZE);
            int minTileY = (int)Math.Floor((playerCollider.Y - expandedRadius) / TileSettings.TILE_SIZE);
            int maxTileY = (int)Math.Ceiling((playerCollider.Y + playerCollider.Height + expandedRadius) / TileSettings.TILE_SIZE);
            
            // Ограничиваем индексы размерами карты тайлов
            minTileX = Math.Max(0, minTileX);
            maxTileX = Math.Min(_tileMap.GetLength(0) - 1, maxTileX);
            minTileY = Math.Max(0, minTileY);
            maxTileY = Math.Min(_tileMap.GetLength(1) - 1, maxTileY);
            
            // Проверяем только коллайдеры тех тайлов, которые находятся рядом с игроком
            for (int y = minTileY; y <= maxTileY; y++)
            {
                for (int x = minTileX; x <= maxTileX; x++)
                {
                    string colliderKey = $"{x}:{y}";
                    if (_tileColliders.TryGetValue(colliderKey, out var tileCollider))
                    {
                        if (playerCollider.Intersects(tileCollider))
                        {
                            // Для отладки
                            // Console.WriteLine($"Коллизия между коллайдером игрока ({playerCollider.X:F1}, {playerCollider.Y:F1}) " +
                            //     $"и коллайдером тайла ({tileCollider.X:F1}, {tileCollider.Y:F1})");
                            return false;
                        }
                    }
                }
            }
            
            return true;
        }

        /// <summary>
        /// Проверяет, видимы ли коллайдеры тайлов
        /// </summary>
        public bool AreColliderVisible()
        {
            return _showTileColliders;
        }
        
        /// <summary>
        /// Возвращает словарь с ближайшими коллайдерами тайлов к указанной позиции
        /// </summary>
        /// <param name="x">Координата X</param>
        /// <param name="y">Координата Y</param>
        /// <returns>Словарь с ключами тайлов и их коллайдерами</returns>
        public Dictionary<string, RectCollider> GetNearbyTileColliders(double x, double y)
        {
            // Создаем словарь для возврата результатов
            Dictionary<string, RectCollider> nearbyColliders = new Dictionary<string, RectCollider>();
            
            // Определяем радиус поиска (немного больше размера тайла)
            double searchRadius = TileSettings.TILE_SIZE * 1.5;
            
            // Вычисляем индексы тайлов в области поиска
            int minTileX = (int)Math.Floor((x - searchRadius) / TileSettings.TILE_SIZE);
            int maxTileX = (int)Math.Ceiling((x + searchRadius) / TileSettings.TILE_SIZE);
            int minTileY = (int)Math.Floor((y - searchRadius) / TileSettings.TILE_SIZE);
            int maxTileY = (int)Math.Ceiling((y + searchRadius) / TileSettings.TILE_SIZE);
            
            // Ограничиваем индексы размерами карты тайлов
            minTileX = Math.Max(0, minTileX);
            maxTileX = Math.Min(_tileMap.GetLength(0) - 1, maxTileX);
            minTileY = Math.Max(0, minTileY);
            maxTileY = Math.Min(_tileMap.GetLength(1) - 1, maxTileY);
            
            // Собираем коллайдеры в указанной области
            for (int tileY = minTileY; tileY <= maxTileY; tileY++)
            {
                for (int tileX = minTileX; tileX <= maxTileX; tileX++)
                {
                    string colliderKey = $"{tileX}:{tileY}";
                    if (_tileColliders.TryGetValue(colliderKey, out var tileCollider))
                    {
                        nearbyColliders.Add(colliderKey, tileCollider);
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
        
        /// <summary>
        /// Возвращает словарь всех коллайдеров тайлов на карте
        /// </summary>
        /// <returns>Словарь коллайдеров с ключами в формате "x:y"</returns>
        public Dictionary<string, RectCollider> GetTileColliders()
        {
            return new Dictionary<string, RectCollider>(_tileColliders);
        }
    }
} 