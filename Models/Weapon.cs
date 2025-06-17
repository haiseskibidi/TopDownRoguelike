using System;
using System.Collections.Generic;
using System.Windows;

namespace GunVault.Models
{
    public enum WeaponType
    {
        Pistol,
        Shotgun,
        AssaultRifle,
        Sniper,
        MachineGun,
        RocketLauncher,
        Laser
    }
    
    public class Weapon
    {
        public string Name { get; private set; }
        public double Damage { get; private set; }
        public double FireRate { get; private set; }
        public double Range { get; private set; }
        public double BulletSpeed { get; private set; }
        public WeaponType Type { get; private set; }
        public string BulletSpriteName { get; set; }
        
        public int MaxAmmo { get; private set; }
        public int CurrentAmmo { get; private set; }
        public double ReloadTime { get; private set; }
        public bool IsReloading { get; private set; }
        private double _reloadTimer;
        
        public double Spread { get; private set; }
        public int BulletsPerShot { get; private set; }
        
        public bool IsExplosive { get; private set; }
        public double ExplosionRadius { get; private set; }
        public double ExplosionDamageMultiplier { get; private set; }
        
        private double _cooldownTime;
        private double _currentCooldown;
        
        private double _currentAngle = 0;
        
        public bool IsLaser => Type == WeaponType.Laser;
        
        public Weapon(string name, WeaponType type, double damage, double fireRate, double range, 
                      double bulletSpeed, int maxAmmo, double reloadTime, double spread, int bulletsPerShot,
                      string bulletSpriteName, bool isExplosive = false, double explosionRadius = 0, double explosionDamageMultiplier = 1.0)
        {
            Name = name;
            Type = type;
            Damage = damage;
            FireRate = fireRate;
            Range = range;
            BulletSpeed = bulletSpeed;
            MaxAmmo = maxAmmo;
            CurrentAmmo = MaxAmmo;
            ReloadTime = reloadTime;
            Spread = spread;
            BulletsPerShot = bulletsPerShot;
            BulletSpriteName = bulletSpriteName;
            
            IsExplosive = isExplosive;
            ExplosionRadius = explosionRadius;
            ExplosionDamageMultiplier = explosionDamageMultiplier;
            
            _cooldownTime = 1.0 / FireRate;
            _currentCooldown = 0;
            
            _reloadTimer = 0;
            IsReloading = false;
        }
        
        public void Update(double deltaTime)
        {
            if (_currentCooldown > 0)
            {
                _currentCooldown -= deltaTime;
            }
            
            if (IsReloading)
            {
                _reloadTimer -= deltaTime;
                if (_reloadTimer <= 0)
                {
                    CurrentAmmo = MaxAmmo;
                    IsReloading = false;
                }
            }
        }
        
        public void StartReload(double reloadSpeedModifier = 1.0)
        {
            if (!IsReloading && CurrentAmmo < MaxAmmo)
            {
                IsReloading = true;
                _reloadTimer = ReloadTime / reloadSpeedModifier;
            }
        }
        
        public bool CanShoot()
        {
            return _currentCooldown <= 0 && CurrentAmmo > 0 && !IsReloading;
        }

        public void Shoot()
        {
            if (!CanShoot())
                return;

            _currentCooldown = _cooldownTime;
            CurrentAmmo--;

            if (CurrentAmmo <= 0)
            {
                StartReload(); // Note: This will use the default modifier of 1.0
            }
        }
        
        public List<Point> GetMuzzlePositions(double playerAngle)
        {
            // This is a placeholder. The actual implementation would be more complex,
            // likely involving data from a configuration file.
            // For now, let's just return a single point in front of the player.
            var list = new List<Point>();
            double muzzleOffsetX = 30; // distance from player center
            double muzzleX = Math.Cos(playerAngle) * muzzleOffsetX;
            double muzzleY = Math.Sin(playerAngle) * muzzleOffsetX;
            list.Add(new Point(muzzleX, muzzleY));
            return list;
        }
        
        public string GetAmmoInfo()
        {
            if (IsReloading)
                return "Перезарядка...";
            else
                return $"{CurrentAmmo}/{MaxAmmo}";
        }
    }
} 