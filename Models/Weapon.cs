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
                      bool isExplosive = false, double explosionRadius = 0, double explosionDamageMultiplier = 1.0)
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
        
        public void StartReload()
        {
            if (!IsReloading && CurrentAmmo < MaxAmmo)
            {
                IsReloading = true;
                _reloadTimer = ReloadTime;
            }
        }
        
        public bool CanFire()
        {
            return _currentCooldown <= 0 && CurrentAmmo > 0 && !IsReloading;
        }
        
        public List<Bullet> Fire(double startX, double startY, double targetX, double targetY)
        {
            if (!CanFire())
                return null;

            _currentCooldown = _cooldownTime;
            
            CurrentAmmo--;
            
            List<Bullet> bullets = new List<Bullet>();
            
            for (int i = 0; i < BulletsPerShot; i++)
            {
                double angle = Math.Atan2(targetY - startY, targetX - startX);
                
                if (Spread > 0)
                {
                    double randomSpread = (new Random().NextDouble() * 2 - 1) * Spread;
                    angle += randomSpread;
                }
                
                bullets.Add(new Bullet(startX, startY, angle, BulletSpeed, Damage, Range, Type));
            }
            
            if (CurrentAmmo <= 0)
            {
                StartReload();
            }
            
            return bullets;
        }
        
        public LaserBeam FireLaser(double startX, double startY, double targetX, double targetY)
        {
            if (!CanFire())
                return null;
            
            _currentCooldown = _cooldownTime;
            
            CurrentAmmo--;
            
            double angle = Math.Atan2(targetY - startY, targetX - startX);
            
            if (Spread > 0)
            {
                double randomSpread = (new Random().NextDouble() * 2 - 1) * Spread;
                angle += randomSpread;
            }
            
            LaserBeam laser = new LaserBeam(startX, startY, angle, Damage, Range);
            
            if (CurrentAmmo <= 0)
            {
                StartReload();
            }
            
            return laser;
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