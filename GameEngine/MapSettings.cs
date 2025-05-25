using System;
using System.Collections.Generic;

namespace GunVault.GameEngine
{
    /// <summary>
    /// Статический класс с настройками и константами для генерации карты
    /// </summary>
    public static class MapSettings
    {
        /// <summary>
        /// Матрица переходов Маркова - вероятности перехода от одного типа тайла к другому
        /// </summary>
        public static readonly Dictionary<TileType, Dictionary<TileType, double>> MarkovTransitions = 
            new Dictionary<TileType, Dictionary<TileType, double>>()
        {
            { 
                TileType.Grass, new Dictionary<TileType, double>() {
                    { TileType.Grass, 0.7 },
                    { TileType.Dirt, 0.2 },
                    { TileType.Water, 0.05 },
                    { TileType.Stone, 0.0 },
                    { TileType.Sand, 0.05 }
                } 
            },
            { 
                TileType.Dirt, new Dictionary<TileType, double>() {
                    { TileType.Grass, 0.3 },
                    { TileType.Dirt, 0.5 },
                    { TileType.Water, 0.05 },
                    { TileType.Stone, 0.1 },
                    { TileType.Sand, 0.05 }
                } 
            },
            { 
                TileType.Water, new Dictionary<TileType, double>() {
                    { TileType.Grass, 0.1 },
                    { TileType.Dirt, 0.05 },
                    { TileType.Water, 0.7 },
                    { TileType.Stone, 0.05 },
                    { TileType.Sand, 0.1 }
                } 
            },
            { 
                TileType.Stone, new Dictionary<TileType, double>() {
                    { TileType.Grass, 0.05 },
                    { TileType.Dirt, 0.2 },
                    { TileType.Water, 0.05 },
                    { TileType.Stone, 0.65 },
                    { TileType.Sand, 0.05 }
                } 
            },
            { 
                TileType.Sand, new Dictionary<TileType, double>() {
                    { TileType.Grass, 0.1 },
                    { TileType.Dirt, 0.1 },
                    { TileType.Water, 0.2 },
                    { TileType.Stone, 0.05 },
                    { TileType.Sand, 0.55 }
                } 
            }
        };
        
        /// <summary>
        /// Правила клеточного автомата: для каждого типа тайла указывается,
        /// сколько соседей этого же типа нужно, чтобы он остался этим же типом
        /// </summary>
        public static readonly Dictionary<TileType, int[]> CellularAutomataRules = 
            new Dictionary<TileType, int[]>()
        {
            // Минимальное и максимальное количество соседей для "выживания" клетки
            { TileType.Grass, new int[] { 4, 8 } },
            { TileType.Dirt, new int[] { 3, 7 } },
            { TileType.Water, new int[] { 5, 8 } },
            { TileType.Stone, new int[] { 4, 8 } },
            { TileType.Sand, new int[] { 3, 7 } }
        };
        
        /// <summary>
        /// Настройки генерации карты
        /// </summary>
        public static class Generation
        {
            /// <summary>
            /// Минимальный размер региона для сохранения
            /// </summary>
            public const int MIN_REGION_SIZE = 4;
            
            /// <summary>
            /// Число итераций клеточного автомата
            /// </summary>
            public const int DEFAULT_CA_ITERATIONS = 4;
        }
    }
} 