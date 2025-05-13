using System;
using System.Collections.Generic;
using GunVault.GameEngine;

namespace GunVault.Models
{
    // Перечисление для типов врагов
    public enum EnemyType
    {
        Basic,      // Базовый враг (красный круг)
        Runner,     // Быстрый враг с малым уроном
        Tank,       // Медленный враг с большим здоровьем
        Bomber,     // Взрывающийся враг
        Boss        // Босс (большой враг с большим здоровьем)
    }
    
    // Класс для хранения параметров врага
    public class EnemyConfig
    {
        public double BaseHealth { get; set; }      // Базовое здоровье
        public double BaseSpeed { get; set; }       // Базовая скорость
        public double Radius { get; set; }          // Радиус
        public int ScoreValue { get; set; }         // Количество очков за убийство
        public string SpriteName { get; set; }      // Имя спрайта
        public double DamageOnCollision { get; set; } // Урон при столкновении с игроком
        
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
    
    // Фабрика для создания врагов
    public static class EnemyFactory
    {
        // Словарь конфигураций для каждого типа врага
        private static readonly Dictionary<EnemyType, EnemyConfig> EnemyConfigs = new Dictionary<EnemyType, EnemyConfig>
        {
            { EnemyType.Basic, new EnemyConfig(health: 30, speed: 60, radius: 15, score: 10, sprite: "enemy1") },
            { EnemyType.Runner, new EnemyConfig(health: 20, speed: 100, radius: 12, score: 15, sprite: "enemy1") },
            { EnemyType.Tank, new EnemyConfig(health: 100, speed: 30, radius: 20, score: 25, sprite: "enemy1") },
            { EnemyType.Bomber, new EnemyConfig(health: 40, speed: 50, radius: 18, score: 20, sprite: "enemy1", damage: 20) },
            { EnemyType.Boss, new EnemyConfig(health: 300, speed: 25, radius: 30, score: 100, sprite: "enemy1", damage: 40) }
        };
        
        // Создание врага определенного типа
        public static Enemy CreateEnemy(EnemyType type, double x, double y, int scoreLevel = 0, SpriteManager? spriteManager = null)
        {
            if (!EnemyConfigs.ContainsKey(type))
            {
                throw new ArgumentException($"Неизвестный тип врага: {type}");
            }
            
            // Получаем базовую конфигурацию врага
            EnemyConfig config = EnemyConfigs[type];
            
            // Масштабируем характеристики в зависимости от уровня счета
            double healthMultiplier = 1.0 + (scoreLevel / 500.0); // Каждые 500 очков +100% к здоровью
            double health = config.BaseHealth * healthMultiplier + scoreLevel / 100; // Здоровье увеличивается со счетом
            
            double speedMultiplier = 1.0 + (scoreLevel / 1000.0); // Каждые 1000 очков +100% к скорости
            double speed = config.BaseSpeed * speedMultiplier;
            
            // Создаем врага с соответствующими характеристиками
            return new Enemy(
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
        }
        
        // Определение типа врага в зависимости от счета
        public static EnemyType GetRandomEnemyTypeForScore(int score, Random random)
        {
            // Список доступных типов врагов в зависимости от счета
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
            
            if (score >= 1000 && random.NextDouble() < 0.05) // 5% шанс появления босса
            {
                return EnemyType.Boss;
            }
            
            // Случайно выбираем тип из доступных
            int index = random.Next(availableTypes.Count);
            return availableTypes[index];
        }
    }
} 