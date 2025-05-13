using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

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
        
        public Rectangle WeaponShape { get; private set; }
        
        private double _cooldownTime;
        private double _currentCooldown;
        
        private double _currentAngle = 0;
        private double _targetAngle = 0;
        private const double ROTATION_SPEED = 10.0;
        
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
            
            Color weaponColor = GetWeaponColor();
            
            WeaponShape = new Rectangle
            {
                Width = 30,
                Height = 8,
                Fill = new SolidColorBrush(weaponColor),
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            
            AdjustWeaponSize();
        }
        
        public void UpdatePosition(double playerX, double playerY, Point targetPoint)
        {
            _targetAngle = Math.Atan2(targetPoint.Y - playerY, targetPoint.X - playerX);
            
            const double OFFSET_FROM_CENTER = 10.0;
            const double OFFSET_X = 0.0;
            const double OFFSET_Y = 10.0;
            
            double handX = playerX + (Math.Cos(_currentAngle) * (OFFSET_FROM_CENTER));
            double handY = playerY + (Math.Sin(_currentAngle) * (OFFSET_FROM_CENTER));
            
            handX += Math.Cos(_currentAngle) * OFFSET_X - Math.Sin(_currentAngle) * OFFSET_Y;
            handY += Math.Sin(_currentAngle) * OFFSET_X + Math.Cos(_currentAngle) * OFFSET_Y;
            
            double gunOffsetX = -WeaponShape.Height / 2 * Math.Sin(_currentAngle);
            double gunOffsetY = WeaponShape.Height / 2 * Math.Cos(_currentAngle);
            
            Canvas.SetLeft(WeaponShape, handX + gunOffsetX);
            Canvas.SetTop(WeaponShape, handY + gunOffsetY - WeaponShape.Height / 2);
            
            var rotateTransform = new RotateTransform(_currentAngle * 180 / Math.PI, 0, WeaponShape.Height / 2);
            WeaponShape.RenderTransform = rotateTransform;
        }
        
        public void UpdateRotation(double deltaTime)
        {
            double angleDifference = NormalizeAngle(_targetAngle - _currentAngle);
            
            double maxRotation = ROTATION_SPEED * deltaTime;
            
            if (Math.Abs(angleDifference) <= maxRotation)
            {
                _currentAngle = _targetAngle;
            }
            else
            {
                double sign = Math.Sign(angleDifference);
                _currentAngle += sign * maxRotation;
                _currentAngle = NormalizeAngle(_currentAngle);
            }
            
            var rotateTransform = new RotateTransform(_currentAngle * 180 / Math.PI, 0, WeaponShape.Height / 2);
            WeaponShape.RenderTransform = rotateTransform;
        }
        
        private double NormalizeAngle(double angle)
        {
            while (angle > Math.PI)
                angle -= 2 * Math.PI;
            while (angle < -Math.PI)
                angle += 2 * Math.PI;
            return angle;
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
        
        private void AdjustWeaponSize()
        {
            switch (Type)
            {
                case WeaponType.Pistol:
                    WeaponShape.Width = 20;
                    break;
                case WeaponType.Shotgun:
                    WeaponShape.Width = 35;
                    WeaponShape.Height = 10;
                    break;
                case WeaponType.AssaultRifle:
                    WeaponShape.Width = 40;
                    break;
                case WeaponType.Sniper:
                    WeaponShape.Width = 50;
                    WeaponShape.Height = 6;
                    break;
                case WeaponType.MachineGun:
                    WeaponShape.Width = 45;
                    WeaponShape.Height = 12;
                    break;
                case WeaponType.RocketLauncher:
                    WeaponShape.Width = 40;
                    WeaponShape.Height = 15;
                    break;
                case WeaponType.Laser:
                    WeaponShape.Width = 35;
                    WeaponShape.Height = 7;
                    break;
            }
        }
        
        private Color GetWeaponColor()
        {
            switch (Type)
            {
                case WeaponType.Pistol:
                    return Colors.DarkGray;
                case WeaponType.Shotgun:
                    return Colors.Brown;
                case WeaponType.AssaultRifle:
                    return Colors.Green;
                case WeaponType.Sniper:
                    return Colors.DarkBlue;
                case WeaponType.MachineGun:
                    return Colors.DarkGreen;
                case WeaponType.RocketLauncher:
                    return Colors.Red;
                case WeaponType.Laser:
                    return Colors.Purple;
                default:
                    return Colors.Gray;
            }
        }
        
        public string GetAmmoInfo()
        {
            if (IsReloading)
                return "Перезарядка...";
            else
                return $"{CurrentAmmo}/{MaxAmmo}";
        }
        
        public Point GetMuzzlePosition()
        {
            double left = Canvas.GetLeft(WeaponShape);
            double top = Canvas.GetTop(WeaponShape);
            
            double pivotX = left;
            double pivotY = top + WeaponShape.Height / 2;
            
            double muzzleX = pivotX + Math.Cos(_currentAngle) * WeaponShape.Width;
            double muzzleY = pivotY + Math.Sin(_currentAngle) * WeaponShape.Width;
            
            return new Point(muzzleX, muzzleY);
        }
    }
} 