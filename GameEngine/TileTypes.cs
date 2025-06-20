using System;
using System.Collections.Generic;

namespace GunVault.GameEngine
{
    public enum TileType
    {
        Grass = 0,
        Dirt = 1,
        Water = 2,
        Stone = 3,
        Sand = 4
    }
    
    public class TileInfo
    {
        public TileType Type { get; set; }
        public string SpriteName { get; set; }
        public bool IsWalkable { get; set; }
        public bool AllowsProjectiles { get; set; }
        public bool CanRandomlyRotate { get; set; }

        public TileInfo(TileType type, string spriteName, bool isWalkable, bool allowsProjectiles, bool canRandomlyRotate)
        {
            Type = type;
            SpriteName = spriteName;
            IsWalkable = isWalkable;
            AllowsProjectiles = allowsProjectiles;
            CanRandomlyRotate = canRandomlyRotate;
        }
    }
    
    public static class TileSettings
    {
        public const int TILE_SIZE = 32;
        public const double TILE_OVERLAP = 1.0;
        
        public static readonly Dictionary<TileType, TileInfo> TileInfos = new Dictionary<TileType, TileInfo>()
        {
            { TileType.Grass, new TileInfo(TileType.Grass, "grass1", true, true, true) }, // проходимость, пропуск снарядов, поворот
            { TileType.Dirt, new TileInfo(TileType.Dirt, "dirt1", true, true, true) },
            { TileType.Water, new TileInfo(TileType.Water, "water1", false, true, false) },
            { TileType.Stone, new TileInfo(TileType.Stone, "stone1", false, false, true) },
            { TileType.Sand, new TileInfo(TileType.Sand, "sand1", true, true, false) }
        };
    }
}