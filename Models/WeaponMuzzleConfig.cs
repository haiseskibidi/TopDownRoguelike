using System;
using System.Collections.Generic;

namespace GunVault.Models
{
    public static class WeaponMuzzleConfig
    {
        private const double BASE_DISTANCE = 1.5;
        
        public struct MuzzleParams
        {
            public double DistanceMultiplier;
            public double OffsetX;
            public double OffsetY;
            
            public MuzzleParams(double distMultiplier, double offsetX, double offsetY)
            {
                DistanceMultiplier = distMultiplier;
                OffsetX = offsetX;
                OffsetY = offsetY;
            }
        }
        
        private static readonly Dictionary<WeaponType, MuzzleParams> MuzzleSettings = new Dictionary<WeaponType, MuzzleParams>
        {
            { WeaponType.Pistol, new MuzzleParams(1.0, 50, 5) },
            { WeaponType.Shotgun, new MuzzleParams(1.0, 50, 15) },
            { WeaponType.AssaultRifle, new MuzzleParams(1.0, 30, 10) },
            { WeaponType.Sniper, new MuzzleParams(1.0, 35, 5) },
            { WeaponType.MachineGun, new MuzzleParams(1.0, 28, 8) },
            { WeaponType.RocketLauncher, new MuzzleParams(1.0, 25, 15) },
            { WeaponType.Laser, new MuzzleParams(1.0, 30, 7) }
        };
        
        public static MuzzleParams GetMuzzleParams(WeaponType weaponType)
        {
            if (MuzzleSettings.TryGetValue(weaponType, out MuzzleParams result))
            {
                return result;
            }
            
            return new MuzzleParams(1.0, 0, 0);
        }
        
        public static double GetMuzzleDistance(WeaponType weaponType, double playerRadius)
        {
            return playerRadius * BASE_DISTANCE * GetMuzzleParams(weaponType).DistanceMultiplier;
        }
    }
} 