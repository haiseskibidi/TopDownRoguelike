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
            { WeaponType.Pistol, new MuzzleParams(1.0, 0, 12) },
            { WeaponType.Shotgun, new MuzzleParams(1.2, 0, 3) },
            { WeaponType.AssaultRifle, new MuzzleParams(1.3, 0, -4) },
            { WeaponType.Sniper, new MuzzleParams(1.5, 0, -2) },
            { WeaponType.MachineGun, new MuzzleParams(1.2, 0, -5) },
            { WeaponType.RocketLauncher, new MuzzleParams(1.1, 0, 12) },
            { WeaponType.Laser, new MuzzleParams(1.2, 0, -3) }
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