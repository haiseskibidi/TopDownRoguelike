using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
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
        private List<BulletImpactEffect> _bulletImpactEffects;
        public LevelGenerator _levelGenerator;
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
        private SpriteManager _spriteManager;
        public event EventHandler<int> ScoreChanged;
        public event EventHandler<string> WeaponChanged;

        // Размер игрового мира (по умолчанию равен размеру экрана, но может быть намного больше)
        private double _worldWidth;
        private double _worldHeight;
        
        // Камера для скроллинга
        private Camera _camera;
        
        // Контейнер для всех игровых объектов, который будет смещаться относительно камеры
        private Canvas _worldContainer;

        // Множитель размера игрового мира относительно экрана
        private const double WORLD_SIZE_MULTIPLIER = 3.0;

        public GameManager(Canvas gameCanvas, Player player, double gameWidth, double gameHeight, SpriteManager spriteManager = null)
        {
            _gameCanvas = gameCanvas;
            _player = player;
            _gameWidth = gameWidth;
            _gameHeight = gameHeight;
            _spriteManager = spriteManager;
            _enemies = new List<Enemy>();
            _bullets = new List<Bullet>();
            _explosions = new List<Explosion>();
            _lasers = new List<LaserBeam>();
            _bulletImpactEffects = new List<BulletImpactEffect>();
            _random = new Random();
            _score = 0;
            _enemySpawnTimer = 0;
            _enemySpawnRate = INITIAL_SPAWN_RATE;
            _lastWeaponType = _player.GetWeaponType();
            
            // Создаем мировой контейнер
            _worldContainer = new Canvas
            {
                Width = _gameWidth * WORLD_SIZE_MULTIPLIER,
                Height = _gameHeight * WORLD_SIZE_MULTIPLIER
            };
            
            // Инициализируем размеры мира
            _worldWidth = _gameWidth * WORLD_SIZE_MULTIPLIER;
            _worldHeight = _gameHeight * WORLD_SIZE_MULTIPLIER;
            
            // Добавляем мировой контейнер на игровой канвас
            _gameCanvas.Children.Add(_worldContainer);
            
            // Инициализируем камеру
            _camera = new Camera(_gameWidth, _gameHeight, _worldWidth, _worldHeight);
            
            // Центрируем камеру на игроке
            _camera.CenterOn(_player.X, _player.Y);
            
            // Установим начальную позицию мирового контейнера, чтобы игрок был в центре экрана
            Canvas.SetLeft(_worldContainer, -_camera.X);
            Canvas.SetTop(_worldContainer, -_camera.Y);
            
            // Перемещаем игрока из GameCanvas в мировой контейнер
            _gameCanvas.Children.Remove(_player.PlayerShape);
            _worldContainer.Children.Add(_player.PlayerShape);
            
            _player.AddWeaponToCanvas(_worldContainer);
            
            _levelGenerator = new LevelGenerator(_worldContainer, _worldWidth, _worldHeight, _spriteManager);
            _levelGenerator.GenerateLevel();
        }

        public void Update(double deltaTime)
        {
            _player.Move();
            
            // Удаляем ограничение игрока на экране, теперь можно двигаться по всему миру
            // _player.ConstrainToScreen(_gameWidth, _gameHeight);
            
            // Вместо этого, ограничиваем игрока размерами мира
            _player.ConstrainToWorldBounds(0, 0, _worldWidth, _worldHeight);
            
            // Обновляем позицию камеры, чтобы следовать за игроком
            _camera.FollowTarget(_player.X, _player.Y);
            
            // Обновляем позицию мирового контейнера относительно камеры
            Canvas.SetLeft(_worldContainer, -_camera.X);
            Canvas.SetTop(_worldContainer, -_camera.Y);
            
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
            }
            
            // Обновляем врагов только если они видны в области камеры или рядом
            foreach (var enemy in _enemies)
            {
                bool isInViewOrNear = _camera.IsInView(
                    enemy.X - 50, enemy.Y - 50, 
                    100, 100  // Размер, немного увеличенный для захвата ближайших врагов
                );
                
                if (isInViewOrNear)
                {
                    enemy.MoveTowardsPlayer(_player.X, _player.Y, deltaTime);
                }
            }
            
            // Обрабатываем ввод мыши и клавиатуры для оружия
            Point mousePosition = Mouse.GetPosition(_gameCanvas);
            Point worldMousePosition = _camera.ScreenToWorld(mousePosition.X, mousePosition.Y);
            _player.UpdateWeapon(deltaTime, worldMousePosition);
            
            // Стрельба игрока
            if (_player.GetCurrentWeapon().IsLaser)
            {
                LaserBeam newLaser = _player.ShootLaser(worldMousePosition);
                    if (newLaser != null)
                    {
                        _lasers.Add(newLaser);
                    _worldContainer.Children.Add(newLaser.LaserLine);
                    _worldContainer.Children.Add(newLaser.LaserDot);
                        
                        ProcessLaserCollisions(newLaser);
                    }
                }
                else
                {
                List<Bullet> newBullets = _player.Shoot(worldMousePosition);
                    if (newBullets != null && newBullets.Count > 0)
                    {
                        foreach (Bullet bullet in newBullets)
                        {
                            _bullets.Add(bullet);
                        _worldContainer.Children.Add(bullet.BulletShape);
                    }
                }
            }
            
            UpdateBullets(deltaTime);
            UpdateLasers(deltaTime);
            UpdateExplosions(deltaTime);
            UpdateBulletImpacts(deltaTime);
            CheckCollisions();
        }

        private void CheckWeaponUpgrade()
        {
            WeaponType currentType = _player.GetWeaponType();
            WeaponType expectedType = WeaponFactory.GetWeaponTypeForScore(_score);
            
            if (expectedType != currentType)
            {
                Weapon newWeapon = WeaponFactory.CreateWeapon(expectedType);
                _player.ChangeWeapon(newWeapon, _worldContainer);
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
            _explosions.Add(explosion);
            _worldContainer.Children.Add(explosion.ExplosionShape);
        }

        private void UpdateExplosions(double deltaTime)
        {
            for (int i = _explosions.Count - 1; i >= 0; i--)
            {
                bool isActive = _explosions[i].Update(deltaTime);
                if (!isActive)
                {
                    _worldContainer.Children.Remove(_explosions[i].ExplosionShape);
                    _explosions.RemoveAt(i);
                }
            }
        }

        private void SpawnEnemy()
        {
            // Модифицируем код для спавна врагов, чтобы они появлялись за пределами видимой области камеры,
            // но все еще внутри игрового мира
            double spawnX = 0, spawnY = 0;
            bool foundValidSpawn = false;
            
            // Максимальное количество попыток найти проходимую позицию
            int maxAttempts = 30; // Увеличиваем количество попыток для повышения шансов найти подходящее место
            int attempts = 0;
            
            // Получаем границы видимой области камеры
            double cameraLeft = _camera.X;
            double cameraTop = _camera.Y;
            double cameraRight = _camera.X + _camera.ViewportWidth;
            double cameraBottom = _camera.Y + _camera.ViewportHeight;
            
            // Добавляем небольшой буфер, чтобы враги появлялись вне экрана
            double buffer = 100;
            
            // Примерный радиус врага для проверки
            double enemyRadius = 20;
            
            // Создаем временный коллайдер, который будет использоваться для проверки области спавна
            RectCollider tempCollider = null;
            
            while (!foundValidSpawn && attempts < maxAttempts)
            {
                attempts++;
                
                if (_random.NextDouble() < 0.5)
                {
                    // Спавн слева или справа от области просмотра
                    spawnX = _random.NextDouble() < 0.5 ? 
                        Math.Max(enemyRadius, cameraLeft - buffer) : 
                        Math.Min(_worldWidth - enemyRadius, cameraRight + buffer);
                    
                    // Случайная Y-координата в диапазоне видимой области (с небольшим расширением)
                    spawnY = _random.NextDouble() * (_camera.ViewportHeight + buffer * 2) + 
                        Math.Max(enemyRadius, cameraTop - buffer);
                    spawnY = Math.Min(spawnY, _worldHeight - enemyRadius);
                }
                else
                {
                    // Спавн сверху или снизу от области просмотра
                    spawnX = _random.NextDouble() * (_camera.ViewportWidth + buffer * 2) + 
                        Math.Max(enemyRadius, cameraLeft - buffer);
                    spawnX = Math.Min(spawnX, _worldWidth - enemyRadius);
                    
                    spawnY = _random.NextDouble() < 0.5 ? 
                        Math.Max(enemyRadius, cameraTop - buffer) : 
                        Math.Min(_worldHeight - enemyRadius, cameraBottom + buffer);
                }
                
                // Создаем временный коллайдер для проверки проходимости области
                // Используем настоящий размер коллайдера врага
                if (tempCollider == null)
                {
                    double colliderSize = enemyRadius * 2 * 0.8; // Примерный размер коллайдера, соответствующий Enemy.Collider
                    tempCollider = new RectCollider(spawnX - colliderSize/2, spawnY - colliderSize/2, colliderSize, colliderSize);
                }
                else
                {
                    // Обновляем позицию существующего коллайдера
                    tempCollider.UpdatePosition(spawnX - tempCollider.Width/2, spawnY - tempCollider.Height/2);
                }
                
                // Проверяем проходимость точек по окружности, а также используем IsAreaWalkable для точной проверки коллизий
                if (_levelGenerator != null)
                {
                    bool centerWalkable = _levelGenerator.IsTileWalkable(spawnX, spawnY);
                    
                    // Проверяем 8 точек по окружности с радиусом enemyRadius * 0.8 (примерный коллайдер)
                    bool allPointsWalkable = true;
                    double checkRadius = enemyRadius * 0.8;
                    
                    for (int i = 0; i < 8 && allPointsWalkable; i++)
                    {
                        double angle = i * Math.PI / 4;
                        double checkX = spawnX + Math.Cos(angle) * checkRadius;
                        double checkY = spawnY + Math.Sin(angle) * checkRadius;
                        
                        if (!_levelGenerator.IsTileWalkable(checkX, checkY))
                        {
                            allPointsWalkable = false;
                        }
                    }
                    
                    // Используем дополнительно IsAreaWalkable для более точной проверки коллизий
                    bool areaWalkable = _levelGenerator.IsAreaWalkable(tempCollider);
                    
                    // Все проверки должны быть успешными
                    if (centerWalkable && allPointsWalkable && areaWalkable)
                    {
                        foundValidSpawn = true;
                        
                        // Для отладки
                        Console.WriteLine($"Найдена валидная позиция для спавна на попытке {attempts}: ({spawnX:F1}, {spawnY:F1})");
                    }
                }
            }
            
            // Если мы не нашли проходимое место после всех попыток, 
            // найдем 100% безопасное место для спавна
            if (!foundValidSpawn)
            {
                Console.WriteLine($"Не удалось найти проходимую позицию для спавна за {maxAttempts} попыток, ищем безопасное место");
                
                // Ищем гарантированно проходимые области - начинаем с позиции игрока
                double searchRadius = 300; // Большой радиус поиска
                
                // Сначала проверим центр экрана - часто там проходимо
                spawnX = _camera.X + _camera.ViewportWidth / 2;
                spawnY = _camera.Y + _camera.ViewportHeight / 2;
                
                // Обновляем позицию коллайдера
                tempCollider.UpdatePosition(spawnX - tempCollider.Width/2, spawnY - tempCollider.Height/2);
                
                // Если центр экрана непроходим, ищем по спирали вокруг игрока
                if (!_levelGenerator.IsAreaWalkable(tempCollider))
                {
                    bool found = false;
                    
                    // Начинаем от игрока и расходимся спиралью
                    for (int r = 1; r <= 20 && !found; r++)
                    {
                        double step = Math.PI / 8; // 16 точек на круге
                        
                        for (double angle = 0; angle < 2 * Math.PI && !found; angle += step)
                        {
                            double testX = _player.X + Math.Cos(angle) * (r * searchRadius / 10);
                            double testY = _player.Y + Math.Sin(angle) * (r * searchRadius / 10);
                            
                            // Ограничиваем координаты границами мира
                            testX = Math.Max(enemyRadius, Math.Min(testX, _worldWidth - enemyRadius));
                            testY = Math.Max(enemyRadius, Math.Min(testY, _worldHeight - enemyRadius));
                            
                            // Обновляем коллайдер
                            tempCollider.UpdatePosition(testX - tempCollider.Width/2, testY - tempCollider.Height/2);
                            
                            if (_levelGenerator.IsAreaWalkable(tempCollider))
                            {
                                spawnX = testX;
                                spawnY = testY;
                                found = true;
                                
                                Console.WriteLine($"Найдена безопасная позиция для спавна при поиске по спирали: ({spawnX:F1}, {spawnY:F1})");
                            }
                        }
                    }
                    
                    // Если даже это не помогло, просто используем позицию игрока
                    if (!found)
                    {
                        // В крайнем случае, просто спавним рядом с игроком с небольшим смещением
                        spawnX = _player.X + (_random.NextDouble() * 200 - 100);
                        spawnY = _player.Y + (_random.NextDouble() * 200 - 100);
                        
                        // Ограничиваем координаты границами мира
                        spawnX = Math.Max(enemyRadius, Math.Min(spawnX, _worldWidth - enemyRadius));
                        spawnY = Math.Max(enemyRadius, Math.Min(spawnY, _worldHeight - enemyRadius));
                        
                        Console.WriteLine($"Не удалось найти безопасное место, спавним рядом с игроком: ({spawnX:F1}, {spawnY:F1})");
                    }
                }
                else
                {
                    Console.WriteLine($"Центр экрана оказался проходимым, спавним там: ({spawnX:F1}, {spawnY:F1})");
                }
            }
            
            EnemyType enemyType = EnemyFactory.GetRandomEnemyTypeForScore(_score, _random);
            Enemy enemy = EnemyFactory.CreateEnemy(enemyType, spawnX, spawnY, _score, _spriteManager);
            
            // Добавляем врага в мировой контейнер
            _worldContainer.Children.Add(enemy.EnemyShape);
            _worldContainer.Children.Add(enemy.HealthBar);
            
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
                    _worldContainer.Children.Remove(_bullets[i].BulletShape);
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
                bool bulletHitTile = false;
                if (_levelGenerator != null)
                {
                    var nearbyTileColliders = _levelGenerator.GetNearbyTileColliders(_bullets[i].X, _bullets[i].Y);
                    
                    foreach (var tileCollider in nearbyTileColliders)
                    {
                        TileType tileType = _levelGenerator.GetTileTypeAt(tileCollider.Key);
                        if (_bullets[i].CollidesWithTile(tileCollider.Value, tileType))
                        {
                            BulletImpactEffect effect = new BulletImpactEffect(
                                _bullets[i].X, 
                                _bullets[i].Y, 
                                Math.Atan2(_bullets[i].Y - _bullets[i].PrevY, _bullets[i].X - _bullets[i].PrevX),
                                tileType,
                                _worldContainer
                            );
                            _bulletImpactEffects.Add(effect);
                            
                            _worldContainer.Children.Remove(_bullets[i].BulletShape);
                            _bullets.RemoveAt(i);
                            bulletHitTile = true;
                            break;
                        }
                    }
                }
                
                if (bulletHitTile)
                    continue;
                
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
                        _worldContainer.Children.Remove(_bullets[i].BulletShape);
                        _bullets.RemoveAt(i);
                        bulletHit = true;
                        if (!isEnemyAlive)
                        {
                            _score += _enemies[j].ScoreValue;
                            ScoreChanged?.Invoke(this, _score);
                            _worldContainer.Children.Remove(_enemies[j].EnemyShape);
                            _worldContainer.Children.Remove(_enemies[j].HealthBar);
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
                            _worldContainer.Children.Remove(_enemies[j].EnemyShape);
                            _worldContainer.Children.Remove(_enemies[j].HealthBar);
                            _enemies.RemoveAt(j);
                        }
                    }
                }
            }
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                if (_enemies[i].CollidesWithPlayer(_player))
                {
                    _player.TakeDamage(_enemies[i].DamageOnCollision);
                    if (_enemies[i].Type == EnemyType.Bomber)
                    {
                        CreateExplosion(
                            _enemies[i].X,
                            _enemies[i].Y,
                            _enemies[i].DamageOnCollision * 2,
                            100
                        );
                    }
                    _worldContainer.Children.Remove(_enemies[i].EnemyShape);
                    _worldContainer.Children.Remove(_enemies[i].HealthBar);
                    _enemies.RemoveAt(i);
                }
            }
        }

        public void ResizeGameArea(double width, double height)
        {
            _gameWidth = width;
            _gameHeight = height;
            
            // Обновляем размеры видимой области в камере
            _camera.UpdateViewport(width, height);
            
            // Обновляем уровень только если изменились размеры мира
            if (_levelGenerator != null && (_worldWidth != width * WORLD_SIZE_MULTIPLIER || 
                                           _worldHeight != height * WORLD_SIZE_MULTIPLIER))
            {
                _worldWidth = width * WORLD_SIZE_MULTIPLIER;
                _worldHeight = height * WORLD_SIZE_MULTIPLIER;
                
                // Обновляем размер мирового контейнера
                _worldContainer.Width = _worldWidth;
                _worldContainer.Height = _worldHeight;
                
                // Обновляем размер мира в камере
                _camera.UpdateWorldSize(_worldWidth, _worldHeight);
                
                // Ограничиваем игрока новыми границами мира
                _player.ConstrainToWorldBounds(0, 0, _worldWidth, _worldHeight);
                
                // Перегенерируем уровень при значительном изменении размеров
                _levelGenerator.ResizeLevel(_worldWidth, _worldHeight);
            }
        }

        public string GetAmmoInfo()
        {
            return _player.GetAmmoInfo();
        }

        public bool IsTileWalkable(double x, double y)
        {
            if (_levelGenerator == null)
            {
                return true;
            }
            
            return _levelGenerator.IsTileWalkable(x, y);
        }

        public bool IsAreaWalkable(RectCollider playerCollider)
        {
            if (_levelGenerator == null)
                return true;
            
            return _levelGenerator.IsAreaWalkable(playerCollider);
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
                    _worldContainer.Children.Remove(enemy.EnemyShape);
                    _worldContainer.Children.Remove(enemy.HealthBar);
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
                    _worldContainer.Children.Remove(_lasers[i].LaserLine);
                    _worldContainer.Children.Remove(_lasers[i].LaserDot);
                    _lasers.RemoveAt(i);
                }
            }
        }

        private void UpdateBulletImpacts(double deltaTime)
        {
            for (int i = _bulletImpactEffects.Count - 1; i >= 0; i--)
            {
                bool isActive = _bulletImpactEffects[i].Update(deltaTime);
                if (!isActive)
                {
                    _bulletImpactEffects.RemoveAt(i);
                }
            }
        }
    }
} 