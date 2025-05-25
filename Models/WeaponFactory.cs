using System;
using System.Collections.Generic;

namespace GunVault.Models
{
    public class WeaponFactory
    {
        private const int SCORE_PER_WEAPON_UPGRADE = 100;
        
        private static readonly List<WeaponType> WeaponProgression = new List<WeaponType>
        {
            WeaponType.Pistol,
            WeaponType.Shotgun,
            WeaponType.AssaultRifle,
            WeaponType.MachineGun,
            WeaponType.Sniper,
            WeaponType.RocketLauncher,
            WeaponType.Laser
        };
        
        public static WeaponType GetWeaponTypeForScore(int score)
        {
            int weaponIndex = Math.Min(score / SCORE_PER_WEAPON_UPGRADE, WeaponProgression.Count - 1);
            return WeaponProgression[weaponIndex];
        }
        
        public static Weapon CreateWeapon(WeaponType type, object unused = null)
        {
            switch (type)
            {
                case WeaponType.Pistol:
                    return new Weapon(
                        name: "Пистолет",
                        type: WeaponType.Pistol,
                        damage: 10,
                        fireRate: 2,
                        range: 500,
                        bulletSpeed: 300,
                        maxAmmo: 12,
                        reloadTime: 1.5,
                        spread: 0.05,
                        bulletsPerShot: 1
                    );
                    
                case WeaponType.Shotgun:
                    return new Weapon(
                        name: "Дробовик",
                        type: WeaponType.Shotgun,
                        damage: 5,
                        fireRate: 1,
                        range: 300,
                        bulletSpeed: 250,
                        maxAmmo: 6,
                        reloadTime: 2.0,
                        spread: 0.2,
                        bulletsPerShot: 5
                    );
                    
                case WeaponType.AssaultRifle:
                    return new Weapon(
                        name: "Штурмовая Винтовка",
                        type: WeaponType.AssaultRifle,
                        damage: 8,
                        fireRate: 5,
                        range: 600,
                        bulletSpeed: 400,
                        maxAmmo: 30,
                        reloadTime: 2.0,
                        spread: 0.1,
                        bulletsPerShot: 1
                    );
                    
                case WeaponType.MachineGun:
                    return new Weapon(
                        name: "Пулемет",
                        type: WeaponType.MachineGun,
                        damage: 6,
                        fireRate: 8,
                        range: 550,
                        bulletSpeed: 350,
                        maxAmmo: 50,
                        reloadTime: 3.0,
                        spread: 0.15,
                        bulletsPerShot: 1
                    );
                    
                case WeaponType.Sniper:
                    return new Weapon(
                        name: "Снайперская Винтовка",
                        type: WeaponType.Sniper,
                        damage: 50,
                        fireRate: 0.8, 
                        range: 1000,
                        bulletSpeed: 600,
                        maxAmmo: 7,
                        reloadTime: 2.5,
                        spread: 0.01,
                        bulletsPerShot: 1
                    );
                    
                case WeaponType.RocketLauncher:
                    return new Weapon(
                        name: "Ракетница",
                        type: WeaponType.RocketLauncher,
                        damage: 50,
                        fireRate: 0.3,
                        range: 800,
                        bulletSpeed: 200,
                        maxAmmo: 3,
                        reloadTime: 3.5,
                        spread: 0.05,
                        bulletsPerShot: 1,
                        isExplosive: true,
                        explosionRadius: 100,
                        explosionDamageMultiplier: 0.85
                    );
                    
                case WeaponType.Laser:
                    return new Weapon(
                        name: "Лазер",
                        type: WeaponType.Laser,
                        damage: 35, 
                        fireRate: 2, 
                        range: 800,
                        bulletSpeed: 800,
                        maxAmmo: 15,
                        reloadTime: 2.0,
                        spread: 0.02,
                        bulletsPerShot: 1
                    );
                    
                default:
                    return CreateWeapon(WeaponType.Pistol);
            }
        }
    }
} 