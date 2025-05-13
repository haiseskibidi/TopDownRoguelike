using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GunVault.Models;

namespace GunVault.GameEngine
{
    public class GameManager
    {
        private Canvas _gameCanvas;
        private Player _player;
        private List<Enemy> _enemies;
        private List<Bullet> _bullets;
        private List<Explosion> _explosions;
        private List<LaserBeam> _lasers;
        private double _gameWidth;
        private double _gameHeight;
        private Random _random;
        private int _score;
        private double _enemySpawnTimer;
        private double _enemySpawnRate = 2.0;
        private WeaponType _lastWeaponType;
        private const double INITIAL_SPAWN_RATE = 2.0;
        private const double MIN_SPAWN_RATE = 0.3;
        private const int SCORE_PER_SPAWN_RATE_DECREASE = 50;
        private const double SPAWN_RATE_DECREASE_STEP = 0.1;
        private const int SCORE_PER_MULTI_SPAWN = 200;
        private const int MAX_ENEMIES_ON_SCREEN = 20;
        private const double EXPLOSION_EXPANSION_SPEED = 150.0;
        public event EventHandler<int> ScoreChanged;
        public event EventHandler<string> WeaponChanged;

        public GameManager(Canvas gameCanvas, Player player, double gameWidth, double gameHeight)
        {
            _gameCanvas = gameCanvas;
            _player = player;
            _gameWidth = gameWidth;
            _gameHeight = gameHeight;
            _enemies = new List<Enemy>();
            _bullets = new List<Bullet>();
            _explosions = new List<Explosion>();
            _lasers = new List<LaserBeam>();
            _random = new Random();
            _score = 0;
            _enemySpawnTimer = 0;
            _lastWeaponType = _player.GetWeaponType();
            _player.AddWeaponToCanvas(_gameCanvas);
        }

        public void Update(double deltaTime)
        {
            _player.Move();
            _player.ConstrainToScreen(_gameWidth, _gameHeight);
            CheckWeaponUpgrade();
            _enemySpawnTimer -= deltaTime;
            if (_enemySpawnTimer <= 0)
            {
                UpdateSpawnRate();
                if (_enemies.Count < MAX_ENEMIES_ON_SCREEN)
                {
                    int enemiesToSpawn = CalculateEnemiesToSpawn();
                    enemiesToSpawn = Math.Min(enemiesToSpawn, MAX_ENEMIES_ON_SCREEN - _enemies.Count);
                    for (int i = 0; i < enemiesToSpawn; i++)
                    {
                        SpawnEnemy();
                    }
                }
                _enemySpawnTimer = _enemySpawnRate;
            }
            Enemy nearestEnemy = FindNearestEnemy();
            Point targetPoint = new Point(_player.X + 100, _player.Y);
            if (nearestEnemy != null)
            {
                targetPoint = new Point(nearestEnemy.X, nearestEnemy.Y);
                if (_player.GetCurrentWeapon().IsLaser)
                {
                    LaserBeam newLaser = _player.ShootLaser(targetPoint);
                    if (newLaser != null)
                    {
                        _lasers.Add(newLaser);
                        _gameCanvas.Children.Add(newLaser.LaserLine);
                        _gameCanvas.Children.Add(newLaser.LaserDot);
                        ProcessLaserCollisions(newLaser);
                    }
                }
                else
                {
                    List<Bullet> newBullets = _player.Shoot(targetPoint);
                    if (newBullets != null && newBullets.Count > 0)
                    {
                        foreach (Bullet bullet in newBullets)
                        {
                            _bullets.Add(bullet);
                            _gameCanvas.Children.Add(bullet.BulletShape);
                        }
                    }
                }
            }
            _player.UpdateWeapon(deltaTime, targetPoint);
            UpdateBullets(deltaTime);
            UpdateLasers(deltaTime);
            UpdateExplosions(deltaTime);
            UpdateEnemies(deltaTime);
            CheckCollisions();
        }

        private void CheckWeaponUpgrade()
        {
            WeaponType currentType = _player.GetWeaponType();
            WeaponType expectedType = WeaponFactory.GetWeaponTypeForScore(_score);
            if (expectedType != currentType)
            {
                Weapon newWeapon = WeaponFactory.CreateWeapon(expectedType);
                _player.ChangeWeapon(newWeapon, _gameCanvas);
                _lastWeaponType = expectedType;
                WeaponChanged?.Invoke(this, newWeapon.Name);
            }
        }

        public void HandleKeyPress(KeyEventArgs e)
        {
            if (e.Key == Key.R)
            {
                _player.StartReload();
            }
        }

        private Enemy FindNearestEnemy()
        {
            if (_enemies.Count == 0)
                return null;
            Enemy nearest = null;
            double minDistance = double.MaxValue;
            foreach (var enemy in _enemies)
            {
                double dx = enemy.X - _player.X;
                double dy = enemy.Y - _player.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = enemy;
                }
            }
            return nearest;
        }

        private void CreateExplosion(double x, double y, double damage, double radius)
        {
            Explosion explosion = new Explosion(x, y, radius, EXPLOSION_EXPANSION_SPEED, damage);
            _gameCanvas.Children.Add(explosion.ExplosionShape);
            _explosions.Add(explosion);
        }

        private void UpdateExplosions(double deltaTime)
        {
            for (int i = _explosions.Count - 1; i >= 0; i--)
            {
                bool isActive = _explosions[i].Update(deltaTime);
                if (!isActive)
                {
                    _gameCanvas.Children.Remove(_explosions[i].ExplosionShape);
                    _explosions.RemoveAt(i);
                }
            }
        }

        private void SpawnEnemy()
        {
            double spawnX, spawnY;
            if (_random.NextDouble() < 0.5)
            {
                spawnX = _random.NextDouble() < 0.5 ? -50 : _gameWidth + 50;
                spawnY = _random.Next(0, (int)_gameHeight);
            }
            else
            {
                spawnX = _random.Next(0, (int)_gameWidth);
                spawnY = _random.NextDouble() < 0.5 ? -50 : _gameHeight + 50;
            }
            double healthMultiplier = 1.0 + (_score / 500.0);
            double health = 30 * healthMultiplier + _score / 100;
            double speedMultiplier = 1.0 + (_score / 1000.0);
            double baseSpeed = 50 + _random.Next(0, 30);
            double speed = baseSpeed * speedMultiplier;
            double radius = 15;
            int scoreValue = 10;
            Enemy enemy = new Enemy(spawnX, spawnY, health, speed, radius, scoreValue);
            _gameCanvas.Children.Add(enemy.EnemyShape);
            _gameCanvas.Children.Add(enemy.HealthBar);
            _enemies.Add(enemy);
        }

        private void UpdateSpawnRate()
        {
            double rateDecrease = Math.Min(
                (_score / SCORE_PER_SPAWN_RATE_DECREASE) * SPAWN_RATE_DECREASE_STEP,
                INITIAL_SPAWN_RATE - MIN_SPAWN_RATE
            );
            _enemySpawnRate = Math.Max(INITIAL_SPAWN_RATE - rateDecrease, MIN_SPAWN_RATE);
        }

        private int CalculateEnemiesToSpawn()
        {
            int baseEnemies = 1;
            int additionalEnemies = _score / SCORE_PER_MULTI_SPAWN;
            return Math.Min(baseEnemies + additionalEnemies, 5);
        }

        private void UpdateBullets(double deltaTime)
        {
            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                bool isActive = _bullets[i].Move(deltaTime);
                if (!isActive)
                {
                    _gameCanvas.Children.Remove(_bullets[i].BulletShape);
                    _bullets.RemoveAt(i);
                }
            }
        }

        private void UpdateEnemies(double deltaTime)
        {
            foreach (var enemy in _enemies)
            {
                enemy.MoveTowardsPlayer(_player.X, _player.Y, deltaTime);
            }
        }

        private void CheckCollisions()
        {
            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                bool bulletHit = false;
                for (int j = _enemies.Count - 1; j >= 0; j--)
                {
                    if (_bullets[i].Collides(_enemies[j]))
                    {
                        Weapon currentWeapon = _player.GetCurrentWeapon();
                        double damage = _bullets[i].Damage;
                        bool isEnemyAlive = _enemies[j].TakeDamage(damage);
                        if (currentWeapon.IsExplosive)
                        {
                            CreateExplosion(
                                _enemies[j].X, 
                                _enemies[j].Y, 
                                damage * currentWeapon.ExplosionDamageMultiplier, 
                                currentWeapon.ExplosionRadius
                            );
                        }
                        _gameCanvas.Children.Remove(_bullets[i].BulletShape);
                        _bullets.RemoveAt(i);
                        bulletHit = true;
                        if (!isEnemyAlive)
                        {
                            _score += _enemies[j].ScoreValue;
                            ScoreChanged?.Invoke(this, _score);
                            _gameCanvas.Children.Remove(_enemies[j].EnemyShape);
                            _gameCanvas.Children.Remove(_enemies[j].HealthBar);
                            _enemies.RemoveAt(j);
                        }
                        break;
                    }
                }
                if (bulletHit)
                    continue;
            }
            for (int i = _explosions.Count - 1; i >= 0; i--)
            {
                for (int j = _enemies.Count - 1; j >= 0; j--)
                {
                    if (_explosions[i].AffectsEnemy(_enemies[j]))
                    {
                        bool isEnemyAlive = _enemies[j].TakeDamage(_explosions[i].Damage);
                        if (!isEnemyAlive)
                        {
                            _score += _enemies[j].ScoreValue;
                            ScoreChanged?.Invoke(this, _score);
                            _gameCanvas.Children.Remove(_enemies[j].EnemyShape);
                            _gameCanvas.Children.Remove(_enemies[j].HealthBar);
                            _enemies.RemoveAt(j);
                        }
                    }
                }
            }
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                if (_enemies[i].CollidesWithPlayer(_player))
                {
                    _player.TakeDamage(10);
                    _gameCanvas.Children.Remove(_enemies[i].EnemyShape);
                    _gameCanvas.Children.Remove(_enemies[i].HealthBar);
                    _enemies.RemoveAt(i);
                }
            }
        }

        public void ResizeGameArea(double width, double height)
        {
            _gameWidth = width;
            _gameHeight = height;
        }

        public string GetAmmoInfo()
        {
            return _player.GetAmmoInfo();
        }

        private void ProcessLaserCollisions(LaserBeam laser)
        {
            Dictionary<Enemy, double> hitEnemies = new Dictionary<Enemy, double>();
            foreach (var enemy in _enemies)
            {
                double distance;
                if (laser.IntersectsWithEnemy(enemy, out distance))
                {
                    hitEnemies.Add(enemy, distance);
                }
            }
            var sortedEnemies = new List<KeyValuePair<Enemy, double>>(hitEnemies);
            sortedEnemies.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));
            foreach (var pair in sortedEnemies)
            {
                Enemy enemy = pair.Key;
                bool isEnemyAlive = enemy.TakeDamage(laser.Damage);
                if (!isEnemyAlive)
                {
                    _score += enemy.ScoreValue;
                    ScoreChanged?.Invoke(this, _score);
                    _gameCanvas.Children.Remove(enemy.EnemyShape);
                    _gameCanvas.Children.Remove(enemy.HealthBar);
                    _enemies.Remove(enemy);
                }
            }
            if (sortedEnemies.Count > 0)
            {
                double maxDistance = laser.MaxLength;
                double dx = Math.Cos(laser.Angle);
                double dy = Math.Sin(laser.Angle);
                double newEndX = laser.StartX + dx * maxDistance;
                double newEndY = laser.StartY + dy * maxDistance;
                laser.SetEndPoint(newEndX, newEndY);
            }
        }

        private void UpdateLasers(double deltaTime)
        {
            for (int i = _lasers.Count - 1; i >= 0; i--)
            {
                bool isActive = _lasers[i].Update(deltaTime);
                if (!isActive)
                {
                    _gameCanvas.Children.Remove(_lasers[i].LaserLine);
                    _gameCanvas.Children.Remove(_lasers[i].LaserDot);
                    _lasers.RemoveAt(i);
                }
            }
        }
    }
} 