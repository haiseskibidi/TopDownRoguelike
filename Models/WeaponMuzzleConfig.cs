using System;
using System.Collections.Generic;

namespace GunVault.Models
{
    // Класс для хранения настроек точки вылета пуль для разных типов оружия
    public static class WeaponMuzzleConfig
    {
        // Базовое расстояние для всех типов оружия (множитель радиуса игрока)
        private const double BASE_DISTANCE = 1.5;
        
        // Структура для хранения параметров дула
        public struct MuzzleParams
        {
            public double DistanceMultiplier; // Множитель базового расстояния
            public double OffsetX;            // Боковое смещение (положительно - вправо)
            public double OffsetY;            // Вертикальное смещение (отрицательно - вверх)
            
            public MuzzleParams(double distMultiplier, double offsetX, double offsetY)
            {
                DistanceMultiplier = distMultiplier;
                OffsetX = offsetX;
                OffsetY = offsetY;
            }
        }
        
        // Словарь настроек для каждого типа оружия
        private static readonly Dictionary<WeaponType, MuzzleParams> MuzzleSettings = new Dictionary<WeaponType, MuzzleParams>
        {
            // Пистолет: стандартное расстояние, чуть ниже центра
            { WeaponType.Pistol, new MuzzleParams(1.0, 0, 12) },
            
            // Дробовик: чуть дальше, ближе к центру по высоте
            { WeaponType.Shotgun, new MuzzleParams(1.2, 0, 3) },
            
            // Штурмовая винтовка: длиннее, чуть выше центра
            { WeaponType.AssaultRifle, new MuzzleParams(1.3, 0, -4) },
            
            // Снайперка: самая длинная, почти по центру
            { WeaponType.Sniper, new MuzzleParams(1.5, 0, -2) },
            
            // Пулемет: среднее расстояние, чуть выше центра
            { WeaponType.MachineGun, new MuzzleParams(1.2, 0, -5) },
            
            // Ракетница: ниже центра, средняя длина
            { WeaponType.RocketLauncher, new MuzzleParams(1.1, 0, 12) },
            
            // Лазер: средней длины, немного выше центра
            { WeaponType.Laser, new MuzzleParams(1.2, 0, -3) }
        };
        
        // Получить параметры дула для указанного типа оружия
        public static MuzzleParams GetMuzzleParams(WeaponType weaponType)
        {
            if (MuzzleSettings.TryGetValue(weaponType, out MuzzleParams result))
            {
                return result;
            }
            
            // Вернуть параметры по умолчанию, если тип оружия не найден
            return new MuzzleParams(1.0, 0, 0);
        }
        
        // Расчет абсолютного расстояния от центра игрока до дула оружия
        public static double GetMuzzleDistance(WeaponType weaponType, double playerRadius)
        {
            return playerRadius * BASE_DISTANCE * GetMuzzleParams(weaponType).DistanceMultiplier;
        }
    }
} 