using System;

namespace GunVault.Models
{
    public class TemporaryBoost
    {
        public TreasureType BoostType { get; private set; }
        public double Amount { get; private set; }
        public double Duration { get; private set; }
        public double RemainingTime { get; private set; }
        public bool IsActive => RemainingTime > 0;
        
        public TemporaryBoost(TreasureType boostType, double amount, double duration)
        {
            BoostType = boostType;
            Amount = amount;
            Duration = duration;
            RemainingTime = duration;
        }
        
        public void Update(double deltaTime)
        {
            if (RemainingTime > 0)
            {
                RemainingTime -= deltaTime;
                if (RemainingTime < 0)
                    RemainingTime = 0;
            }
        }
        
        public void Reset()
        {
            RemainingTime = Duration;
        }
        
        public void Extend(double additionalDuration)
        {
            Duration += additionalDuration;
            RemainingTime += additionalDuration;
        }
        
        public void Increase(double additionalAmount)
        {
            Amount += additionalAmount;
        }
        
        public string GetRemainingTimeText()
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(RemainingTime);
            return timeSpan.TotalMinutes >= 1 
                ? $"{timeSpan.Minutes}:{timeSpan.Seconds:D2}" 
                : $"{timeSpan.Seconds}.{timeSpan.Milliseconds / 100}";
        }
        
        public string GetBoostName()
        {
            switch (BoostType)
            {
                case TreasureType.HealthRegenBoost: return "Регенерация здоровья";
                case TreasureType.MaxHealthBoost: return "Макс. здоровье";
                case TreasureType.BulletSpeedBoost: return "Скорость пуль";
                case TreasureType.BulletDamageBoost: return "Урон пуль";
                case TreasureType.ReloadSpeedBoost: return "Перезарядка";
                case TreasureType.MovementSpeedBoost: return "Скорость движения";
                default: return "Неизвестный бонус";
            }
        }
    }
} 