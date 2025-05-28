using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using GunVault.Models;

namespace GunVault.GameEngine
{
    public class Chunk
    {
        public const int CHUNK_SIZE = 16;

        public double PixelSize => CHUNK_SIZE * TileSettings.TILE_SIZE;

        public int ChunkX { get; private set; }
        public int ChunkY { get; private set; }

        private bool _isActive;
        public bool IsActive 
        { 
            get => _isActive;
            set 
            {
                if (value && !_isActive)
                {
                    LastActiveTime = DateTime.Now;
                    
                    if (_cachedEnemies.Count > 0)
                    {
                        OnChunkActivated?.Invoke(this, EventArgs.Empty);
                    }
                }
                _isActive = value;
            }
        }

        public DateTime LastActiveTime { get; private set; }

        public double WorldX => ChunkX * PixelSize;
        public double WorldY => ChunkY * PixelSize;

        private Dictionary<string, RectCollider> _tileColliders;
        private List<EnemyState> _cachedEnemies;

        public UIElement? DebugMarker { get; set; }
        public event EventHandler OnChunkActivated;

        public Chunk(int chunkX, int chunkY)
        {
            ChunkX = chunkX;
            ChunkY = chunkY;
            IsActive = false;
            LastActiveTime = DateTime.Now;
            _tileColliders = new Dictionary<string, RectCollider>();
            _cachedEnemies = new List<EnemyState>();
        }

        public bool ContainsPoint(double worldX, double worldY)
        {
            return worldX >= WorldX && worldX < WorldX + PixelSize &&
                   worldY >= WorldY && worldY < WorldY + PixelSize;
        }

        public bool IsInRangeOf(int playerChunkX, int playerChunkY, int chunkDistance)
        {
            int deltaX = Math.Abs(ChunkX - playerChunkX);
            int deltaY = Math.Abs(ChunkY - playerChunkY);

            return deltaX <= chunkDistance && deltaY <= chunkDistance;
        }

        public void AddTileCollider(string key, RectCollider collider)
        {
            if (!_tileColliders.ContainsKey(key))
            {
                _tileColliders.Add(key, collider);
            }
        }

        public Dictionary<string, RectCollider> GetTileColliders()
        {
            return _tileColliders;
        }
        
        public void CacheEnemy(EnemyState enemyState)
        {
            _cachedEnemies.Add(enemyState);
        }
        
        public List<EnemyState> GetCachedEnemies()
        {
            return new List<EnemyState>(_cachedEnemies);
        }
        
        public void ClearCachedEnemies()
        {
            _cachedEnemies.Clear();
        }

        public static (int chunkX, int chunkY) WorldToChunk(double worldX, double worldY)
        {
            int chunkX = (int)Math.Floor(worldX / (CHUNK_SIZE * TileSettings.TILE_SIZE));
            int chunkY = (int)Math.Floor(worldY / (CHUNK_SIZE * TileSettings.TILE_SIZE));
            return (chunkX, chunkY);
        }

        public static (double worldX, double worldY) ChunkToWorld(int chunkX, int chunkY)
        {
            double worldX = chunkX * CHUNK_SIZE * TileSettings.TILE_SIZE;
            double worldY = chunkY * CHUNK_SIZE * TileSettings.TILE_SIZE;
            return (worldX, worldY);
        }
    }
} 