using System;
using GunVault.Models;

namespace GunVault.Models
{
    public class EnemyState
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Health { get; set; }
        public double MaxHealth { get; set; }
        public double Speed { get; set; }
        public double Radius { get; set; }
        public int ScoreValue { get; set; }
        public double DamageOnCollision { get; set; }
        public EnemyType Type { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime DespawnTime { get; set; }
        public string SpriteName { get; set; }
        public Guid Id { get; set; }
        public int ChunkX { get; set; }
        public int ChunkY { get; set; }
        
        public static EnemyState CreateFromEnemy(Enemy enemy, string spriteName = "enemy1")
        {
            var (chunkX, chunkY) = GameEngine.Chunk.WorldToChunk(enemy.X, enemy.Y);
            
            return new EnemyState
            {
                X = enemy.X,
                Y = enemy.Y,
                Health = enemy.Health,
                MaxHealth = enemy.MaxHealth,
                Speed = enemy.Speed,
                Radius = enemy.Radius,
                ScoreValue = enemy.ScoreValue,
                DamageOnCollision = enemy.DamageOnCollision,
                Type = enemy.Type,
                CreationTime = enemy.CreationTime,
                DespawnTime = DateTime.Now,
                SpriteName = spriteName,
                Id = Guid.NewGuid(),
                ChunkX = chunkX,
                ChunkY = chunkY
            };
        }
    }
} 