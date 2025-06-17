using System;
using System.Collections.Generic;
using GunVault.GameEngine;

namespace GunVault.Models
{
    public enum EnemyType
    {
        Basic,
        Runner,
        Tank,
        Bomber,
        Boss
    }
    
    public class EnemyConfig
    {
        public double BaseHealth { get; set; }
        public double BaseSpeed { get; set; }
        public double Radius { get; set; }
        public int ScoreValue { get; set; }
        public string SpriteName { get; set; }
        public double DamageOnCollision { get; set; }
        
        public EnemyConfig(double health, double speed, double radius, int score, string sprite, double damage = 10)
        {
            BaseHealth = health;
            BaseSpeed = speed;
            Radius = radius;
            ScoreValue = score;
            SpriteName = sprite;
            DamageOnCollision = damage;
        }
    }
    
    public static class EnemyFactory
    {
        private static readonly Dictionary<EnemyType, EnemyConfig> EnemyConfigs = new Dictionary<EnemyType, EnemyConfig>
        {
            { EnemyType.Basic, new EnemyConfig(health: 30, speed: 60, radius: 15, score: 30, sprite: "enemy1") },
            { EnemyType.Runner, new EnemyConfig(health: 20, speed: 100, radius: 13, score: 15, sprite: "enemy2") },
            { EnemyType.Tank, new EnemyConfig(health: 100, speed: 30, radius: 20, score: 25, sprite: "enemy1") },
            { EnemyType.Bomber, new EnemyConfig(health: 40, speed: 50, radius: 18, score: 20, sprite: "enemy1", damage: 20) },
            { EnemyType.Boss, new EnemyConfig(health: 300, speed: 25, radius: 30, score: 100, sprite: "enemy1", damage: 40) }
        };
        
        public static Enemy CreateEnemy(EnemyType type, double x, double y, int scoreLevel = 0, SpriteManager? spriteManager = null)
        {
            if (!EnemyConfigs.ContainsKey(type))
            {
                throw new ArgumentException($"Неизвестный тип врага: {type}");
            }
            
            EnemyConfig config = EnemyConfigs[type];
            
            double healthMultiplier = 1.0 + (scoreLevel / 2000.0);
            double health = config.BaseHealth * healthMultiplier + scoreLevel / 100;
            
            double speedMultiplier = 1.0 + (scoreLevel / 10000.0);
            double speed = config.BaseSpeed * speedMultiplier;
            
            int score = 5 + (int)(scoreLevel * 0.1);
            
            Enemy enemy = new Enemy(
                startX: x,
                startY: y,
                health: health,
                speed: speed,
                radius: config.Radius,
                scoreValue: config.ScoreValue,
                damageOnCollision: config.DamageOnCollision,
                type: type,
                spriteName: config.SpriteName,
                spriteManager: spriteManager
            );
            
            // Устанавливаем опыт в зависимости от типа врага
            int experience = GetExperienceForEnemyType(type, scoreLevel);
            enemy.SetExperienceValue(experience);
            
            return enemy;
        }
        
        private static int GetExperienceForEnemyType(EnemyType type, int scoreLevel)
        {
            switch (type)
            {
                case EnemyType.Basic:
                    return 60 + (int)(scoreLevel * 0.6);
                case EnemyType.Runner:
                    return 65 + (int)(scoreLevel * 0.5);
                case EnemyType.Tank:
                    return 75 + (int)(scoreLevel * 0.7);
                case EnemyType.Bomber:
                    return 85; // Фиксированный опыт, так как он взрывается
                case EnemyType.Boss:
                    return 200 + (int)(scoreLevel * 0.5);
                default:
                    return 10;
            }
        }
        
        public static EnemyType GetRandomEnemyTypeForScore(int score, Random random)
        {
            List<EnemyType> availableTypes = new List<EnemyType> { EnemyType.Basic };
            
            if (score >= 100)
            {
                availableTypes.Add(EnemyType.Runner);
            }
            
            if (score >= 300)
            {
                availableTypes.Add(EnemyType.Tank);
            }
            
            if (score >= 500)
            {
                availableTypes.Add(EnemyType.Bomber);
            }
            
            if (score >= 1000 && random.NextDouble() < 0.05)
            {
                return EnemyType.Boss;
            }
            
            int index = random.Next(availableTypes.Count);
            return availableTypes[index];
        }
    }
} 