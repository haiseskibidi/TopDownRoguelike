using System;
using System.Collections.Generic;

namespace GunVault.GameEngine
{
    public class MapGenerator
    {
        private readonly int _width;
        private readonly int _height;
        private readonly Random _random;
        private TileType[,] _map;
        
        public MapGenerator(int width, int height, int? seed = null)
        {
            _width = width;
            _height = height;
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
            _map = new TileType[width, height];
        }
        
        public TileType[,] Generate(TileType initialType = TileType.Grass)
        {
            InitializeWithMarkovChain(initialType);
            ApplyCellularAutomata(MapSettings.Generation.DEFAULT_CA_ITERATIONS);
            CleanupIsolatedRegions(MapSettings.Generation.MIN_REGION_SIZE);
            
            return _map;
        }
        
        private void InitializeWithMarkovChain(TileType initialType)
        {
            _map[_width / 2, _height / 2] = initialType;
            
            Queue<(int x, int y)> queue = new Queue<(int x, int y)>();
            queue.Enqueue((_width / 2, _height / 2));
            
            bool[,] visited = new bool[_width, _height];
            visited[_width / 2, _height / 2] = true;
            
            int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };
            
            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                TileType currentType = _map[x, y];
                
                for (int i = 0; i < dx.Length; i++)
                {
                    int nx = x + dx[i];
                    int ny = y + dy[i];
                    
                    if (IsInBounds(nx, ny) && !visited[nx, ny])
                    {
                        visited[nx, ny] = true;
                        TileType nextType = GetNextTileType(currentType);
                        _map[nx, ny] = nextType;
                        queue.Enqueue((nx, ny));
                    }
                }
            }
            
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
            
            return currentType;
        }
        
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
        
        private Dictionary<TileType, int> CountNeighborTypes(int x, int y)
        {
            Dictionary<TileType, int> counts = new Dictionary<TileType, int>();
            
            foreach (TileType type in Enum.GetValues(typeof(TileType)))
            {
                counts[type] = 0;
            }
            
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
        
        private TileType ApplyCellularRule(TileType currentType, Dictionary<TileType, int> neighborCounts)
        {
            if (MapSettings.CellularAutomataRules.TryGetValue(currentType, out int[] survivalRule))
            {
                int sameTypeCount = neighborCounts[currentType];
                
                if (sameTypeCount >= survivalRule[0] && sameTypeCount <= survivalRule[1])
                {
                    return currentType;
                }
            }
            
            return GetDominantType(neighborCounts);
        }
        
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
        
        private void CleanupIsolatedRegions(int minRegionSize)
        {
            foreach (TileType tileType in Enum.GetValues(typeof(TileType)))
            {
                if (tileType == TileType.Grass) continue;
                
                for (int x = 0; x < _width; x++)
                {
                    for (int y = 0; y < _height; y++)
                    {
                        if (_map[x, y] == tileType)
                        {
                            HashSet<(int, int)> region = GetConnectedRegion(x, y, tileType);
                            
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
                
                CheckNeighbor(x - 1, y, targetType, queue, region);
                CheckNeighbor(x + 1, y, targetType, queue, region);
                CheckNeighbor(x, y - 1, targetType, queue, region);
                CheckNeighbor(x, y + 1, targetType, queue, region);
            }
            
            return region;
        }
        
        private void CheckNeighbor(int x, int y, TileType targetType, Queue<(int, int)> queue, HashSet<(int, int)> visited)
        {
            if (IsInBounds(x, y) && _map[x, y] == targetType && !visited.Contains((x, y)))
            {
                queue.Enqueue((x, y));
                visited.Add((x, y));
            }
        }
        
        private TileType GetDominantNeighborType(HashSet<(int, int)> region)
        {
            Dictionary<TileType, int> neighborTypeCounts = new Dictionary<TileType, int>();
            
            foreach (TileType type in Enum.GetValues(typeof(TileType)))
            {
                neighborTypeCounts[type] = 0;
            }
            
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
        
        private bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < _width && y >= 0 && y < _height;
        }
    }
} 