using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GunVault.GameEngine;

namespace GunVault.Models
{
    public enum TreasureType
    {
        SkillPoints,
        Experience,
        HealthRegenBoost,
        MaxHealthBoost,
        BulletSpeedBoost,
        BulletDamageBoost,
        ReloadSpeedBoost,
        MovementSpeedBoost
    }

    public class TreasureChest
    {
        // Константы
        private const double CHEST_WIDTH = 40.0;
        private const double CHEST_HEIGHT = 40.0;
        private const double INTERACTION_RADIUS = 60.0;

        // Свойства
        public double X { get; private set; }
        public double Y { get; private set; }
        public bool IsOpen { get; private set; }
        public bool IsCollected { get; private set; }
        public Image ChestImage { get; private set; }
        public RectCollider Collider { get; private set; }
        
        // Содержимое сундука
        public TreasureType TreasureType { get; private set; }
        public int SkillPointsAmount { get; private set; }
        public int ExperienceAmount { get; private set; }
        public double BoostAmount { get; private set; }
        public double BoostDuration { get; private set; } // в секундах
        
        private readonly Random _random;
        private readonly SpriteManager _spriteManager;

        public TreasureChest(double x, double y, SpriteManager spriteManager)
        {
            X = x;
            Y = y;
            IsOpen = false;
            IsCollected = false;
            _random = new Random();
            _spriteManager = spriteManager;
            
            // Создаем изображение сундука
            ChestImage = new Image
            {
                Width = CHEST_WIDTH,
                Height = CHEST_HEIGHT,
                Source = _spriteManager != null 
                    ? _spriteManager.LoadSprite("chest_close") 
                    : new BitmapImage(new Uri("pack://application:,,,/Sprites/chest_close.png"))
            };
            
            // Размещаем изображение на канвасе
            Canvas.SetLeft(ChestImage, X - CHEST_WIDTH / 2);
            Canvas.SetTop(ChestImage, Y - CHEST_HEIGHT / 2);
            
            // Создаем коллайдер
            Collider = new RectCollider(X - CHEST_WIDTH / 2, Y - CHEST_HEIGHT / 2, CHEST_WIDTH, CHEST_HEIGHT);
            
            // Генерируем содержимое сундука
            GenerateTreasure();
        }

        // Генерация содержимого сундука
        private void GenerateTreasure()
        {
            // Определяем тип сокровища (33% шанс на очки навыков, 33% на опыт, 33% на временный бонус)
            double treasureRoll = _random.NextDouble();
            
            if (treasureRoll < 0.33)
            {
                TreasureType = TreasureType.SkillPoints;
                
                // Генерируем количество очков навыков (от 1 до 4)
                // Чем больше очков, тем меньше шанс выпадения
                double roll = _random.NextDouble();
                if (roll < 0.5)      SkillPointsAmount = 1; // 50% шанс
                else if (roll < 0.8) SkillPointsAmount = 2; // 30% шанс
                else if (roll < 0.95) SkillPointsAmount = 3; // 15% шанс
                else                  SkillPointsAmount = 4; // 5% шанс
            }
            else if (treasureRoll < 0.66)
            {
                TreasureType = TreasureType.Experience;
                
                // Генерируем количество опыта (от 50 до 300)
                // Чем больше опыта, тем меньше шанс выпадения
                double roll = _random.NextDouble();
                if (roll < 0.5)      ExperienceAmount = 50; // 50% шанс
                else if (roll < 0.8) ExperienceAmount = 100; // 30% шанс
                else if (roll < 0.95) ExperienceAmount = 200; // 15% шанс
                else                  ExperienceAmount = 300; // 5% шанс
            }
            else
            {
                // Выбираем случайный тип бонуса
                Array treasureTypes = Enum.GetValues(typeof(TreasureType));
                // Начинаем с 2, чтобы пропустить SkillPoints и Experience
                TreasureType = (TreasureType)treasureTypes.GetValue(_random.Next(2, treasureTypes.Length));
                
                // Генерируем продолжительность бонуса (от 5 до 120 секунд)
                // Чем больше продолжительность, тем меньше шанс выпадения
                double durationRoll = _random.NextDouble();
                if (durationRoll < 0.5)      BoostDuration = 5.0;  // 50% шанс: 5 секунд
                else if (durationRoll < 0.8) BoostDuration = 15.0; // 30% шанс: 15 секунд
                else if (durationRoll < 0.95) BoostDuration = 30.0; // 15% шанс: 30 секунд
                else                          BoostDuration = 120.0; // 5% шанс: 2 минуты
                
                // Генерируем величину бонуса в зависимости от типа
                double boostRoll = _random.NextDouble();
                switch (TreasureType)
                {
                    case TreasureType.HealthRegenBoost:
                        if (boostRoll < 0.7) BoostAmount = 1.0;      // +1 к регенерации (70%)
                        else                  BoostAmount = 2.0;      // +2 к регенерации (30%)
                        break;
                    case TreasureType.MaxHealthBoost:
                        if (boostRoll < 0.7) BoostAmount = 50.0;     // +50 к макс. здоровью (70%)
                        else                  BoostAmount = 100.0;    // +100 к макс. здоровью (30%)
                        break;
                    case TreasureType.BulletSpeedBoost:
                    case TreasureType.BulletDamageBoost:
                    case TreasureType.ReloadSpeedBoost:
                        if (boostRoll < 0.7) BoostAmount = 0.25;     // +25% к характеристике (70%)
                        else                  BoostAmount = 0.5;      // +50% к характеристике (30%)
                        break;
                    case TreasureType.MovementSpeedBoost:
                        if (boostRoll < 0.7) BoostAmount = 2.0;      // +2 к скорости движения (70%)
                        else                  BoostAmount = 4.0;      // +4 к скорости движения (30%)
                        break;
                }
            }
        }

        // Проверка, находится ли игрок в радиусе взаимодействия с сундуком
        public bool IsPlayerInRange(Player player)
        {
            double distance = Math.Sqrt(Math.Pow(player.X - X, 2) + Math.Pow(player.Y - Y, 2));
            return distance <= INTERACTION_RADIUS;
        }

        // Открытие сундука
        public void Open()
        {
            if (!IsOpen)
            {
                IsOpen = true;
                
                // Меняем спрайт на открытый сундук
                ChestImage.Source = _spriteManager != null 
                    ? _spriteManager.LoadSprite("chest_open") 
                    : new BitmapImage(new Uri("pack://application:,,,/Sprites/chest_open.png"));
            }
        }

        // Получение содержимого сундука
        public string Collect()
        {
            if (!IsCollected)
            {
                IsCollected = true;
                
                // Возвращаем описание содержимого сундука
                switch (TreasureType)
                {
                    case TreasureType.SkillPoints:
                        return $"Получено {SkillPointsAmount} {GetSkillPointsText(SkillPointsAmount)}!";
                    case TreasureType.Experience:
                        return $"Получено {ExperienceAmount} единиц опыта!";
                    case TreasureType.HealthRegenBoost:
                        return $"Бонус восстановления +{BoostAmount} на {BoostDuration} сек.!";
                    case TreasureType.MaxHealthBoost:
                        return $"Бонус макс. здоровья +{BoostAmount} на {BoostDuration} сек.!";
                    case TreasureType.BulletSpeedBoost:
                        return $"Бонус скорости пуль +{BoostAmount * 100}% на {BoostDuration} сек.!";
                    case TreasureType.BulletDamageBoost:
                        return $"Бонус урона пуль +{BoostAmount * 100}% на {BoostDuration} сек.!";
                    case TreasureType.ReloadSpeedBoost:
                        return $"Бонус скорости перезарядки +{BoostAmount * 100}% на {BoostDuration} сек.!";
                    case TreasureType.MovementSpeedBoost:
                        return $"Бонус скорости движения +{BoostAmount} на {BoostDuration} сек.!";
                    default:
                        return "Сундук пуст!";
                }
            }
            
            return string.Empty;
        }
        
        // Вспомогательный метод для правильного склонения слова "очко"
        private string GetSkillPointsText(int amount)
        {
            if (amount == 1)
                return "очко навыка";
            else if (amount >= 2 && amount <= 4)
                return "очка навыков";
            else
                return "очков навыков";
        }

        // Применение эффекта сундука к игроку
        public void ApplyEffect(Player player)
        {
            if (!IsCollected)
            {
                switch (TreasureType)
                {
                    case TreasureType.SkillPoints:
                    case TreasureType.Experience:
                        // Логика добавления очков навыков и опыта будет в GameManager
                        break;
                    case TreasureType.HealthRegenBoost:
                    case TreasureType.MaxHealthBoost:
                    case TreasureType.BulletSpeedBoost:
                    case TreasureType.BulletDamageBoost:
                    case TreasureType.ReloadSpeedBoost:
                    case TreasureType.MovementSpeedBoost:
                        // Временные бонусы будут применяться в GameManager
                        break;
                }
                
                IsCollected = true;
            }
        }
    }
} 