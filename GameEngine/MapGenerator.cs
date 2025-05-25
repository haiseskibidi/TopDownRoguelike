using System;
using System.Collections.Generic;

namespace GunVault.GameEngine
{
    /// <summary>
    /// Процедурный генератор карты с использованием алгоритмов Маркова и клеточных автоматов
    /// </summary>
    public class MapGenerator
    {
        private readonly int _width;
        private readonly int _height;
        private readonly Random _random;
        private TileType[,] _map;
        
        /// <summary>
        /// Инициализирует новый генератор карты
        /// </summary>
        /// <param name="width">Ширина карты в тайлах</param>
        /// <param name="height">Высота карты в тайлах</param>
        /// <param name="seed">Сид для генерации случайных чисел (необязательно)</param>
        public MapGenerator(int width, int height, int? seed = null)
        {
            _width = width;
            _height = height;
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
            _map = new TileType[width, height];
        }
        
        /// <summary>
        /// Генерирует карту, используя комбинацию алгоритмов
        /// </summary>
        /// <param name="initialType">Начальный тип тайла</param>
        /// <returns>Сгенерированная карта тайлов</returns>
        public TileType[,] Generate(TileType initialType = TileType.Grass)
        {
            InitializeWithMarkovChain(initialType);
            ApplyCellularAutomata(MapSettings.Generation.DEFAULT_CA_ITERATIONS);
            CleanupIsolatedRegions(MapSettings.Generation.MIN_REGION_SIZE);
            
            return _map;
        }
        
        #region Генерация исходной карты
        
        /// <summary>
        /// Генерирует начальную карту с помощью цепи Маркова и волнового заполнения
        /// </summary>
        private void InitializeWithMarkovChain(TileType initialType)
        {
            // Начинаем с центрального тайла заданного типа
            _map[_width / 2, _height / 2] = initialType;
            
            // Очередь для волнового алгоритма
            Queue<(int x, int y)> queue = new Queue<(int x, int y)>();
            queue.Enqueue((_width / 2, _height / 2));
            
            // Отмечаем посещенные тайлы
            bool[,] visited = new bool[_width, _height];
            visited[_width / 2, _height / 2] = true;
            
            // 8 направлений для соседей (включая диагонали)
            int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };
            
            // Волновое заполнение с переходами Маркова
            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                TileType currentType = _map[x, y];
                
                // Обрабатываем всех соседей текущей клетки
                for (int i = 0; i < dx.Length; i++)
                {
                    int nx = x + dx[i];
                    int ny = y + dy[i];
                    
                    if (IsInBounds(nx, ny) && !visited[nx, ny])
                    {
                        visited[nx, ny] = true;
                        
                        // Выбираем следующий тип тайла на основе матрицы переходов
                        TileType nextType = GetNextTileType(currentType);
                        _map[nx, ny] = nextType;
                        
                        queue.Enqueue((nx, ny));
                    }
                }
            }
            
            // Заполняем оставшиеся непосещенные тайлы
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (!visited[x, y])
                    {
                        _map[x, y] = GetRandomNeighborTypeOrDefault(x, y, initialType);
                    }
                }
            }
        }
        
        /// <summary>
        /// Определяет следующий тип тайла на основе вероятностей перехода
        /// </summary>
        private TileType GetNextTileType(TileType currentType)
        {
            var transitions = MapSettings.MarkovTransitions[currentType];
            double roll = _random.NextDouble();
            double cumulativeProbability = 0;
            
            foreach (var transition in transitions)
            {
                cumulativeProbability += transition.Value;
                if (roll < cumulativeProbability)
                {
                    return transition.Key;
                }
            }
            
            return currentType; // Резервный вариант
        }
        
        /// <summary>
        /// Возвращает случайный тип тайла от соседей или значение по умолчанию
        /// </summary>
        private TileType GetRandomNeighborTypeOrDefault(int x, int y, TileType defaultType)
        {
            List<TileType> neighborTypes = new List<TileType>();
            
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    
                    if (IsInBounds(nx, ny) && (dx != 0 || dy != 0))
                    {
                        neighborTypes.Add(_map[nx, ny]);
                    }
                }
            }
            
            return neighborTypes.Count > 0 
                ? neighborTypes[_random.Next(neighborTypes.Count)] 
                : defaultType;
        }
        
        #endregion
        
        #region Клеточные автоматы
        
        /// <summary>
        /// Применяет клеточный автомат для сглаживания и улучшения карты
        /// </summary>
        private void ApplyCellularAutomata(int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                TileType[,] newMap = new TileType[_width, _height];
                
                for (int x = 0; x < _width; x++)
                {
                    for (int y = 0; y < _height; y++)
                    {
                        Dictionary<TileType, int> neighborCounts = CountNeighborTypes(x, y);
                        newMap[x, y] = ApplyCellularRule(_map[x, y], neighborCounts);
                    }
                }
                
                _map = newMap;
            }
        }
        
        /// <summary>
        /// Подсчитывает количество соседей каждого типа тайла
        /// </summary>
        private Dictionary<TileType, int> CountNeighborTypes(int x, int y)
        {
            Dictionary<TileType, int> counts = new Dictionary<TileType, int>();
            
            // Инициализируем счетчики для всех типов тайлов
            foreach (TileType type in Enum.GetValues(typeof(TileType)))
            {
                counts[type] = 0;
            }
            
            // Считаем соседей во всех 8 направлениях
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    int nx = x + dx;
                    int ny = y + dy;
                    
                    if (IsInBounds(nx, ny))
                    {
                        counts[_map[nx, ny]]++;
                    }
                }
            }
            
            return counts;
        }
        
        /// <summary>
        /// Применяет правило клеточного автомата к конкретному тайлу
        /// </summary>
        private TileType ApplyCellularRule(TileType currentType, Dictionary<TileType, int> neighborCounts)
        {
            // Правила выживания для текущего типа
            if (MapSettings.CellularAutomataRules.TryGetValue(currentType, out int[] survivalRule))
            {
                int sameTypeCount = neighborCounts[currentType];
                
                // Если количество соседей того же типа в пределах правила выживания,
                // то клетка остается того же типа
                if (sameTypeCount >= survivalRule[0] && sameTypeCount <= survivalRule[1])
                {
                    return currentType;
                }
            }
            
            // Если текущий тип не выжил, выбираем доминирующий тип среди соседей
            return GetDominantType(neighborCounts);
        }
        
        /// <summary>
        /// Возвращает тип тайла с наибольшим количеством соседей
        /// </summary>
        private TileType GetDominantType(Dictionary<TileType, int> neighborCounts)
        {
            TileType dominantType = TileType.Grass;
            int maxCount = 0;
            
            foreach (var pair in neighborCounts)
            {
                if (pair.Value > maxCount)
                {
                    maxCount = pair.Value;
                    dominantType = pair.Key;
                }
            }
            
            return dominantType;
        }
        
        #endregion
        
        #region Очистка и оптимизация
        
        /// <summary>
        /// Удаляет изолированные тайлы и мелкие области
        /// </summary>
        private void CleanupIsolatedRegions(int minRegionSize)
        {
            // Для каждого типа тайла проверяем и удаляем маленькие изолированные области
            foreach (TileType tileType in Enum.GetValues(typeof(TileType)))
            {
                if (tileType == TileType.Grass) continue; // Траву не трогаем, она основной тип
                
                for (int x = 0; x < _width; x++)
                {
                    for (int y = 0; y < _height; y++)
                    {
                        if (_map[x, y] == tileType)
                        {
                            // Получаем все соединенные тайлы этого типа
                            HashSet<(int, int)> region = GetConnectedRegion(x, y, tileType);
                            
                            // Если область слишком маленькая, заменяем её доминирующим типом соседей
                            if (region.Count < minRegionSize)
                            {
                                TileType replacementType = GetDominantNeighborType(region);
                                foreach (var (rx, ry) in region)
                                {
                                    _map[rx, ry] = replacementType;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Находит все тайлы, соединенные с указанной точкой и имеющие тот же тип
        /// </summary>
        private HashSet<(int, int)> GetConnectedRegion(int startX, int startY, TileType targetType)
        {
            HashSet<(int, int)> region = new HashSet<(int, int)>();
            Queue<(int, int)> queue = new Queue<(int, int)>();
            
            if (_map[startX, startY] != targetType)
                return region;
                
            queue.Enqueue((startX, startY));
            region.Add((startX, startY));
            
            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                
                // Проверяем соседей в 4 основных направлениях
                CheckNeighbor(x - 1, y, targetType, queue, region);
                CheckNeighbor(x + 1, y, targetType, queue, region);
                CheckNeighbor(x, y - 1, targetType, queue, region);
                CheckNeighbor(x, y + 1, targetType, queue, region);
            }
            
            return region;
        }
        
        /// <summary>
        /// Проверяет соседа и добавляет его в очередь, если он подходит
        /// </summary>
        private void CheckNeighbor(int x, int y, TileType targetType, Queue<(int, int)> queue, HashSet<(int, int)> visited)
        {
            if (IsInBounds(x, y) && _map[x, y] == targetType && !visited.Contains((x, y)))
            {
                queue.Enqueue((x, y));
                visited.Add((x, y));
            }
        }
        
        /// <summary>
        /// Определяет доминирующий тип тайла среди соседей указанной области
        /// </summary>
        private TileType GetDominantNeighborType(HashSet<(int, int)> region)
        {
            Dictionary<TileType, int> neighborTypeCounts = new Dictionary<TileType, int>();
            
            // Инициализируем счетчики для всех типов тайлов
            foreach (TileType type in Enum.GetValues(typeof(TileType)))
            {
                neighborTypeCounts[type] = 0;
            }
            
            // Подсчитываем соседей для всей области
            foreach (var (x, y) in region)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        
                        int nx = x + dx;
                        int ny = y + dy;
                        
                        if (IsInBounds(nx, ny) && !region.Contains((nx, ny)))
                        {
                            neighborTypeCounts[_map[nx, ny]]++;
                        }
                    }
                }
            }
            
            return GetDominantType(neighborTypeCounts);
        }
        
        #endregion
        
        #region Вспомогательные методы
        
        /// <summary>
        /// Проверяет, находится ли точка в пределах карты
        /// </summary>
        private bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < _width && y >= 0 && y < _height;
        }
        
        #endregion
    }
} 