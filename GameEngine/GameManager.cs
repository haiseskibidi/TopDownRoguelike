using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using GunVault.Models;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

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
        private double _healthRegenTimer = 1.0; // Regenerate health every second
        private double _enemyUpdateTimer = 0.0; // Таймер для оптимизации обновления врагов
        private const double ENEMY_UPDATE_INTERVAL = 0.3; 
        private WeaponType _lastWeaponType;
        private const double INITIAL_SPAWN_RATE = 2.0;
        private const double MIN_SPAWN_RATE = 0.3;
        private const int SCORE_PER_SPAWN_RATE_DECREASE = 50;
        private const double SPAWN_RATE_DECREASE_STEP = 0.1;
        private const int SCORE_PER_MULTI_SPAWN = 200;
        private const int MAX_ENEMIES_ON_SCREEN = 10;
        private const double EXPLOSION_EXPANSION_SPEED = 150.0;
        private SpriteManager _spriteManager;
        private Dictionary<Enemy, Point> _enemyTargets; // Новое поле для хранения целей врагов
        public event EventHandler<int> ScoreChanged;
        public event EventHandler<string> WeaponChanged;
        public event EventHandler<int> EnemyKilled;

        private ChunkManager _chunkManager;
        private bool _showChunkBoundaries = false;

        private double _worldWidth;
        private double _worldHeight;
        
        private Camera _camera;
        
        private Canvas _worldContainer;

        private const double WORLD_SIZE_MULTIPLIER = 3.0;

        private const double ENEMY_DESPAWN_TIME = 3.0;
        
        private double _enemyDespawnCheckTimer = 0.0;
        private const double ENEMY_DESPAWN_CHECK_INTERVAL = 1.0;

        private bool _useMultithreading = true;
        private CancellationTokenSource _enemyProcessingCancellation;
        private Task _enemyProcessingTask;
        private ConcurrentQueue<Enemy> _enemyUpdateQueue;
        private object _enemiesLock = new object();
        private int _maxEnemiesPerThread = 10;
        private int _processingThreadCount = 0;

        // Добавляем новые поля для сундуков
        private List<TreasureChest> _treasureChests;
        private List<TemporaryBoost> _activeBoosts;
        private double _chestSpawnTimer;
        private double _chestSpawnRate = 1.0; // Новый сундук каждые 30 секунд
        private const int MAX_CHESTS_ON_SCREEN = 5;
        private const double MIN_CHEST_SPAWN_DISTANCE_FROM_PLAYER = 300.0;
        private const double MIN_CHEST_SPAWN_DISTANCE_FROM_CHEST = 200.0;
        private const double CHECK_CHEST_INTERACTION_INTERVAL = 0.5;
        private double _chestInteractionTimer;
        
        // Добавляем новое событие для уведомления о сундуках
        public event EventHandler<string> TreasureFound;
        public event EventHandler<int> SkillPointsAdded;
        public event EventHandler<int> ExperienceAdded; // Новое событие для опыта
        public event EventHandler<string> BoostActivated;

        private ObjectPool<Bullet> _bulletPool;

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
            _enemyTargets = new Dictionary<Enemy, Point>(); // Инициализация
            
            _bulletPool = new ObjectPool<Bullet>(
                factory: () => {
                    var bullet = new Bullet(0, 0, 0, 0, 0, "bullet", _spriteManager);
                    _worldContainer.Children.Add(bullet.BulletShape);
                    bullet.Deactivate();
                    return bullet;
                },
                onGet: bullet => bullet.Activate(),
                onReturn: bullet => bullet.Deactivate()
            );

            _enemyUpdateQueue = new ConcurrentQueue<Enemy>();
            _enemyProcessingCancellation = new CancellationTokenSource();
            
            _worldContainer = new Canvas
            {
                Width = _gameWidth * WORLD_SIZE_MULTIPLIER,
                Height = _gameHeight * WORLD_SIZE_MULTIPLIER
            };
            
            _worldWidth = _gameWidth * WORLD_SIZE_MULTIPLIER;
            _worldHeight = _gameHeight * WORLD_SIZE_MULTIPLIER;
            
            _gameCanvas.Children.Add(_worldContainer);
            
            _camera = new Camera(_gameWidth, _gameHeight, _worldWidth, _worldHeight);
            
            _camera.CenterOn(_player.X, _player.Y);
            
            Canvas.SetLeft(_worldContainer, -_camera.X);
            Canvas.SetTop(_worldContainer, -_camera.Y);
            
            _gameCanvas.Children.Remove(_player.PlayerShape);
            _worldContainer.Children.Add(_player.PlayerShape);
            
            _player.AddWeaponToCanvas(_worldContainer);
            
            _chunkManager = new ChunkManager(_worldContainer);
            
            _levelGenerator = new LevelGenerator(_worldContainer, _worldWidth, _worldHeight, _spriteManager);
            _levelGenerator.GenerateLevel();
            
            InitializeChunks();
            
            _chunkManager.EnemiesReadyToRestore += OnEnemiesReadyToRestore;

            if (_useMultithreading)
            {
                StartEnemyProcessingTask();
                Console.WriteLine("Многопоточная обработка врагов активирована");
            }

            _treasureChests = new List<TreasureChest>();
            _activeBoosts = new List<TemporaryBoost>();
            _chestSpawnTimer = 5.0; // Первый сундук появится через 5 секунд
            _chestInteractionTimer = 0;
        }
        
        private void InitializeChunks()
        {
            Dictionary<string, RectCollider> tileColliders = _levelGenerator.GetTileColliders();
            
            foreach (var colliderPair in tileColliders)
            {
                _chunkManager.AddTileCollider(colliderPair.Key, colliderPair.Value);
            }
            
            _chunkManager.UpdateActiveChunks(_player.X, _player.Y);
            
            Console.WriteLine($"Инициализированы чанки и распределены тайлы");
        }

        public void Update(double deltaTime)
        {
            _player.Move();
            
            // Health Regeneration
            _healthRegenTimer -= deltaTime;
            if (_healthRegenTimer <= 0)
            {
                _player.Heal(_player.HealthRegen);
                _healthRegenTimer = 1.0; // Reset timer
            }

            _player.ConstrainToWorldBounds(0, 0, _worldWidth, _worldHeight);
            
            _camera.FollowTarget(_player.X, _player.Y);
            
            Canvas.SetLeft(_worldContainer, -_camera.X);
            Canvas.SetTop(_worldContainer, -_camera.Y);
            
            _chunkManager.UpdateActiveChunks(_player.X, _player.Y, _player.VelocityX, _player.VelocityY);
            _chunkManager.UpdateChunkMarkers();
            
            _enemyDespawnCheckTimer -= deltaTime;
            if (_enemyDespawnCheckTimer <= 0)
            {
                RemoveEnemiesInStaleChunks();
                _enemyDespawnCheckTimer = ENEMY_DESPAWN_CHECK_INTERVAL;
            }
            
            // Динамическое обновление врагов
            foreach (var enemy in _enemies)
            {
                enemy.TimeUntilNextUpdate -= deltaTime;
            
                if (enemy.TimeUntilNextUpdate <= 0)
                {
                    // Обновляем цель
                    _enemyTargets[enemy] = new Point(_player.X, _player.Y);
            
                    // Рассчитываем расстояние до игрока
                    double distance = Math.Sqrt(Math.Pow(enemy.X - _player.X, 2) + Math.Pow(enemy.Y - _player.Y, 2));
            
                    // Устанавливаем новый интервал в зависимости от расстояния
                    if (distance < 500)
                    {
                        enemy.TimeUntilNextUpdate = 0.3;
                    }
                    else if (distance < 1000)
                    {
                        enemy.TimeUntilNextUpdate = 0.4; 
                    }
                    else
                    {
                        enemy.TimeUntilNextUpdate = 1.0;
                    }
                }
            
                // Враг всегда движется к своей цели
                if (_enemyTargets.TryGetValue(enemy, out Point target))
                {
                    enemy.MoveTowardsPlayer(target.X, target.Y, deltaTime);
                }
            }

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
            
            Point mousePosition = Mouse.GetPosition(_gameCanvas);
            Point worldMousePosition = _camera.ScreenToWorld(mousePosition.X, mousePosition.Y);
            _player.UpdateWeapon(deltaTime, worldMousePosition);
            
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
                    var bulletParams = _player.Shoot(worldMousePosition);
                    if (bulletParams != null)
                    {
                        foreach (var p in bulletParams)
                        {
                            var bullet = _bulletPool.Get();
                            bullet.Init(p.StartX, p.StartY, p.Angle, p.Speed, p.Damage, "bullet", _spriteManager, p.IsExplosive, p.ExplosionRadius, p.ExplosionDamage);
                            _bullets.Add(bullet);
                        }
                    }
                }
            
            UpdateBullets(deltaTime);
            UpdateLasers(deltaTime);
            UpdateExplosions(deltaTime);
            UpdateBulletImpacts(deltaTime);
            CheckCollisions();

            // Обновление таймера взаимодействия с сундуками
            _chestInteractionTimer -= deltaTime;
            if (_chestInteractionTimer <= 0)
            {
                CheckChestInteractions();
                _chestInteractionTimer = CHECK_CHEST_INTERACTION_INTERVAL;
            }
            
            // Обновление таймера появления сундуков
            _chestSpawnTimer -= deltaTime;
            if (_chestSpawnTimer <= 0)
            {
                if (_treasureChests.Count < MAX_CHESTS_ON_SCREEN)
                {
                    SpawnTreasureChest();
                }
                _chestSpawnTimer = _chestSpawnRate;
            }
            
            // Обновление активных бонусов
            UpdateActiveBoosts(deltaTime);
        }

        public void HandleKeyPress(KeyEventArgs e)
        {
            if (e.Key == Key.F3)
            {
                _showChunkBoundaries = !_showChunkBoundaries;
                _chunkManager.ToggleChunkBoundaries(_showChunkBoundaries);
                Console.WriteLine($"Отображение границ чанков: {(_showChunkBoundaries ? "включено" : "выключено")}");
            }
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
            double spawnX = 0, spawnY = 0;
            bool foundValidSpawn = false;
            
            // Максимальное количество попыток найти проходимую позицию
            int maxAttempts = 30;
            int attempts = 0;
            
            // Получаем границы видимой области камеры
            double cameraLeft = _camera.X;
            double cameraTop = _camera.Y;
            double cameraRight = _camera.X + _camera.ViewportWidth;
            double cameraBottom = _camera.Y + _camera.ViewportHeight;
            
            // Буфер расстояния от края экрана
            double buffer = 100;
            
            // Примерный радиус врага для проверки
            double enemyRadius = 20;
            
            // Временный коллайдер для проверки
            RectCollider tempCollider = null;
            
            // Активные чанки для спавна
            List<Chunk> activeChunks = _chunkManager.GetActiveChunks();
            
            // Если нет активных чанков (что мало вероятно), не спавним врага
            if (activeChunks.Count == 0)
            {
                Console.WriteLine("Не удалось создать врага: нет активных чанков");
                return;
            }
            
            // Сохраняем чанк, в котором будет создан враг
            Chunk? spawnChunk = null;
            
            while (!foundValidSpawn && attempts < maxAttempts)
            {
                attempts++;
                
                // Выбираем случайный активный чанк для спавна
                spawnChunk = activeChunks[_random.Next(activeChunks.Count)];
                
                // Не спавним в чанке, где находится игрок
                var (playerChunkX, playerChunkY) = Chunk.WorldToChunk(_player.X, _player.Y);
                if (spawnChunk.ChunkX == playerChunkX && spawnChunk.ChunkY == playerChunkY)
                {
                    continue;
                }
                
                // Определяем позицию спавна внутри выбранного чанка
                double chunkLeft = spawnChunk.WorldX;
                double chunkTop = spawnChunk.WorldY;
                double spawnAreaWidth = spawnChunk.PixelSize;
                double spawnAreaHeight = spawnChunk.PixelSize;
                
                // Небольшое смещение от края чанка
                double margin = 20;
                
                // Получаем случайную позицию внутри выбранного чанка
                spawnX = chunkLeft + margin + _random.NextDouble() * (spawnAreaWidth - 2 * margin);
                spawnY = chunkTop + margin + _random.NextDouble() * (spawnAreaHeight - 2 * margin);
                
                // Создаем временный коллайдер для проверки проходимости области
                if (tempCollider == null)
                {
                    double colliderSize = enemyRadius * 2 * 0.8;
                    tempCollider = new RectCollider(spawnX - colliderSize/2, spawnY - colliderSize/2, colliderSize, colliderSize);
                }
                else
                {
                    tempCollider.UpdatePosition(spawnX - tempCollider.Width/2, spawnY - tempCollider.Height/2);
                }
                
                // Проверяем проходимость точек и коллизии
                if (_levelGenerator != null)
                {
                    bool centerWalkable = _levelGenerator.IsTileWalkable(spawnX, spawnY);
                    
                    // Проверяем 8 точек по окружности
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
                    
                    // Используем IsAreaWalkable для точной проверки коллизий
                    bool areaWalkable = _levelGenerator.IsAreaWalkable(tempCollider);
                    
                    if (centerWalkable && allPointsWalkable && areaWalkable)
                    {
                        foundValidSpawn = true;
                        Console.WriteLine($"Найдена валидная позиция для спавна в чанке {spawnChunk.ChunkX}:{spawnChunk.ChunkY} на попытке {attempts}: ({spawnX:F1}, {spawnY:F1})");
                    }
                }
            }
            
            // Если не нашли место после всех попыток, используем фоллбэк метод
            if (!foundValidSpawn)
            {
                Console.WriteLine("Не удалось найти проходимую позицию для спавна, ищем безопасное место вне чанка игрока");
                
                // Получаем чанк игрока
                var (playerChunkX, playerChunkY) = Chunk.WorldToChunk(_player.X, _player.Y);
                
                // Перебираем все чанки вокруг игрока, но не сам чанк игрока
                for (int y = playerChunkY - ChunkManager.ACTIVATION_DISTANCE; y <= playerChunkY + ChunkManager.ACTIVATION_DISTANCE; y++)
                {
                    for (int x = playerChunkX - ChunkManager.ACTIVATION_DISTANCE; x <= playerChunkX + ChunkManager.ACTIVATION_DISTANCE; x++)
                    {
                        // Пропускаем чанк игрока
                        if (x == playerChunkX && y == playerChunkY)
                            continue;
                            
                        // Получаем мировые координаты центра чанка
                        var (worldX, worldY) = Chunk.ChunkToWorld(x, y);
                        worldX += Chunk.CHUNK_SIZE * TileSettings.TILE_SIZE / 2;
                        worldY += Chunk.CHUNK_SIZE * TileSettings.TILE_SIZE / 2;
                        
                        // Проверяем проходимость центра чанка
                        tempCollider.UpdatePosition(worldX - tempCollider.Width/2, worldY - tempCollider.Height/2);
                        
                        if (_levelGenerator.IsTileWalkable(worldX, worldY) && _levelGenerator.IsAreaWalkable(tempCollider))
                        {
                            spawnX = worldX;
                            spawnY = worldY;
                            foundValidSpawn = true;
                            spawnChunk = _chunkManager.GetOrCreateChunk(x, y);
                            Console.WriteLine($"Найдено безопасное место в чанке {x}:{y}: ({spawnX:F1}, {spawnY:F1})");
                            break;
                        }
                    }
                    
                    if (foundValidSpawn) break;
                }
                
                // Если все еще не нашли безопасное место, спавним за пределами экрана
                if (!foundValidSpawn)
                {
                    // В крайнем случае, просто спавним за пределами экрана
                    if (_random.NextDouble() < 0.5)
                    {
                        // Слева или справа от экрана
                        spawnX = _random.NextDouble() < 0.5 ? 
                            Math.Max(enemyRadius, cameraLeft - buffer) : 
                            Math.Min(_worldWidth - enemyRadius, cameraRight + buffer);
                        
                        spawnY = _random.NextDouble() * (_camera.ViewportHeight + buffer * 2) + 
                            Math.Max(enemyRadius, cameraTop - buffer);
                        spawnY = Math.Min(spawnY, _worldHeight - enemyRadius);
                    }
                    else
                    {
                        // Сверху или снизу от экрана
                        spawnX = _random.NextDouble() * (_camera.ViewportWidth + buffer * 2) + 
                            Math.Max(enemyRadius, cameraLeft - buffer);
                        spawnX = Math.Min(spawnX, _worldWidth - enemyRadius);
                        
                        spawnY = _random.NextDouble() < 0.5 ? 
                            Math.Max(enemyRadius, cameraTop - buffer) : 
                            Math.Min(_worldHeight - enemyRadius, cameraBottom + buffer);
                    }
                    
                    Console.WriteLine($"Крайний случай: спавн за пределами экрана: ({spawnX:F1}, {spawnY:F1})");
                    
                    // Получаем или создаем чанк для этой позиции
                    var (spawnChunkX, spawnChunkY) = Chunk.WorldToChunk(spawnX, spawnY);
                    spawnChunk = _chunkManager.GetOrCreateChunk(spawnChunkX, spawnChunkY);
                }
            }
            
            // Если у нас есть чанк для спавна, гарантируем, что он активен
            if (spawnChunk != null)
            {
                // Принудительно активируем чанк, чтобы враг не исчез сразу
                spawnChunk.IsActive = true;
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
            
            // Используем квадратный корень для более плавного роста количества врагов
            int additionalEnemies = (int)Math.Sqrt(_score / SCORE_PER_MULTI_SPAWN);
            
            // Ограничиваем максимальное количество врагов за один спавн
            int maxSpawnCount = 4;
            
            return Math.Min(baseEnemies + additionalEnemies, maxSpawnCount);
        }

        private void UpdateBullets(double deltaTime)
        {
            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                var bullet = _bullets[i];
                if (!bullet.IsActive)
                {
                    _bullets.RemoveAt(i);
                    _bulletPool.Return(bullet);
                    continue;
                }

                bool isActive = bullet.Move(deltaTime);
                if (!isActive)
                {
                    _bullets.RemoveAt(i);
                    _bulletPool.Return(bullet);
                }
            }
        }

        private void UpdateEnemies(double deltaTime)
        {
            // Этот метод будет использоваться для более сложной логики врагов в будущем
        }

        private void CheckCollisions()
        {
            // Коллизии пуль с врагами и тайлами
            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                Bullet bullet = _bullets[i];
                if (!bullet.IsActive) continue;

                bool bulletRemoved = false;

                // Проверка на столкновение с тайлами
                if (_levelGenerator != null)
                {
                    var nearbyTileColliders = _levelGenerator.GetNearbyTileColliders(bullet.X, bullet.Y);
                    foreach (var tileCollider in nearbyTileColliders)
                    {
                        TileType tileType = _levelGenerator.GetTileTypeAt(tileCollider.Key);
                        if (bullet.CollidesWithTile(tileCollider.Value, tileType))
                        {
                            bullet.Deactivate();
                            bulletRemoved = true;
                            
                            // Создаем взрыв, если пуля взрывная
                            if (bullet.IsExplosive && bullet.ExplosionRadius > 0)
                            {
                                CreateExplosion(bullet.X, bullet.Y, bullet.ExplosionDamage, bullet.ExplosionRadius);
                            }
                            
                            break;
                        }
                    }
                }
                
                if (bulletRemoved) continue;
                
                // Проверка на столкновение с врагами
                for (int j = _enemies.Count - 1; j >= 0; j--)
                {
                    Enemy enemy = _enemies[j];
                    if (bullet.Collides(enemy))
                    {
                        bool isEnemyAlive = enemy.TakeDamage(bullet.Damage);
                        bullet.Deactivate();
                        bulletRemoved = true;

                        // Создаем взрыв, если пуля взрывная
                        if (bullet.IsExplosive && bullet.ExplosionRadius > 0)
                        {
                            CreateExplosion(bullet.X, bullet.Y, bullet.ExplosionDamage, bullet.ExplosionRadius);
                        }

                        if (!isEnemyAlive)
                        {
                            _score += enemy.ScoreValue;
                            ScoreChanged?.Invoke(this, _score);
                            EnemyKilled?.Invoke(this, enemy.ExperienceValue);
                            _worldContainer.Children.Remove(enemy.EnemyShape);
                            _worldContainer.Children.Remove(enemy.HealthBar);
                            _enemyTargets.Remove(enemy);
                            _enemies.RemoveAt(j);
                        }
                        break;
                    }
                }
            }

            // Коллизии взрывов с врагами
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
                            EnemyKilled?.Invoke(this, _enemies[j].ExperienceValue);
                            _worldContainer.Children.Remove(_enemies[j].EnemyShape);
                            _worldContainer.Children.Remove(_enemies[j].HealthBar);
                            _enemyTargets.Remove(_enemies[j]);
                            _enemies.RemoveAt(j);
                        }
                    }
                }
            }

            // Коллизии игрока с врагами
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = _enemies[i];
                if (_player.Collider.Intersects(enemy.Collider))
                {
                    _player.TakeDamage(enemy.DamageOnCollision);
                    _worldContainer.Children.Remove(enemy.EnemyShape);
                    _worldContainer.Children.Remove(enemy.HealthBar);
                    _enemyTargets.Remove(enemy);
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

        /// <summary>
        /// Обновляет ссылку на игрока после возрождения
        /// </summary>
        /// <param name="player">Новый объект игрока</param>
        public void UpdatePlayer(Player player)
        {
            if (player != null)
            {
                _player = player;
            }
        }
        
        /// <summary>
        /// Добавляет игрока в мировой контейнер
        /// </summary>
        /// <param name="player">Объект игрока</param>
        public void AddPlayerToWorld(Player player)
        {
            if (player != null && _worldContainer != null)
            {
                // Добавляем игрока в мировой контейнер
                _worldContainer.Children.Add(player.PlayerShape);
                
                // Добавляем оружие игрока в мировой контейнер
                player.AddWeaponToCanvas(_worldContainer);
                
                // Обновляем позицию камеры, чтобы сфокусироваться на игроке
                _camera.CenterOn(player.X, player.Y);
                
                // Обновляем положение мирового контейнера
                Canvas.SetLeft(_worldContainer, -_camera.X);
                Canvas.SetTop(_worldContainer, -_camera.Y);
                
                // Обновляем активные чанки вокруг игрока
                _chunkManager.UpdateActiveChunks(player.X, player.Y);
                
                Console.WriteLine($"Игрок добавлен в мировой контейнер на позиции ({player.X}, {player.Y})");
            }
        }

        /// <summary>
        /// Возвращает ширину игрового мира
        /// </summary>
        public double GetWorldWidth()
        {
            return _worldWidth;
        }
        
        /// <summary>
        /// Возвращает высоту игрового мира
        /// </summary>
        public double GetWorldHeight()
        {
            return _worldHeight;
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

        /// <summary>
        /// Получает коллайдеры тайлов около указанной позиции
        /// </summary>
        /// <param name="x">Координата X</param>
        /// <param name="y">Координата Y</param>
        /// <returns>Словарь с коллайдерами тайлов</returns>
        public Dictionary<string, RectCollider> GetNearbyTileColliders(double x, double y)
        {
            // Теперь используем только коллайдеры из активных чанков
            return _chunkManager.GetActiveChunkColliders();
        }
        
        /// <summary>
        /// Проверяет, является ли область проходимой
        /// </summary>
        /// <param name="playerCollider">Коллайдер игрока или другого объекта</param>
        /// <returns>true, если область проходима</returns>
        public bool IsAreaWalkable(RectCollider playerCollider)
        {
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
                    _enemyTargets.Remove(enemy);
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

        /// <summary>
        /// Возвращает менеджер чанков для внешнего доступа
        /// </summary>
        public ChunkManager GetChunkManager()
        {
            return _chunkManager;
        }

        /// <summary>
        /// Включает/выключает отображение границ чанков
        /// </summary>
        public void ToggleChunkBoundaries()
        {
            _showChunkBoundaries = !_showChunkBoundaries;
            _chunkManager.ToggleChunkBoundaries(_showChunkBoundaries);
        }

        /// <summary>
        /// Удаляет врагов, находящихся в устаревших неактивных чанках
        /// </summary>
        private void RemoveEnemiesInStaleChunks()
        {
            // Если нет врагов, нечего удалять
            if (_enemies.Count == 0) return;
            
            // Минимальное время с момента создания врага, после которого его можно удалять из неактивного чанка
            TimeSpan minimumEnemyLifetime = TimeSpan.FromSeconds(3.0);
            
            // Время бездействия чанка, после которого враги в нём удаляются
            TimeSpan staleTime = TimeSpan.FromSeconds(ENEMY_DESPAWN_TIME);
            
            List<Enemy> enemiesToRemove = new List<Enemy>();
            DateTime now = DateTime.Now;
            
            // Проходим по всем врагам
            foreach (var enemy in _enemies)
            {
                // Проверяем, находится ли враг в устаревшем неактивном чанке
                // И если враг существует достаточно долго
                if (enemy.CreationTime.Add(minimumEnemyLifetime) < now && 
                    _chunkManager.IsInInactiveStaleChunk(enemy.X, enemy.Y, staleTime))
                {
                    enemiesToRemove.Add(enemy);
                }
            }
            
            // Вместо удаления, сохраняем состояние врагов и затем удаляем их
            if (enemiesToRemove.Count > 0)
            {
                foreach (var enemy in enemiesToRemove)
                {
                    // Создаем объект состояния врага
                    string spriteName = GetEnemySpriteName(enemy.Type);
                    EnemyState enemyState = EnemyState.CreateFromEnemy(enemy, spriteName);
                    
                    // Кэшируем состояние врага в соответствующем чанке
                    _chunkManager.CacheEnemyState(enemyState);
                    
                    // Удаляем враждебный объект с экрана
                    _worldContainer.Children.Remove(enemy.EnemyShape);
                    _worldContainer.Children.Remove(enemy.HealthBar);
                    _enemyTargets.Remove(enemy);
                    _enemies.Remove(enemy);
                }
                
                Console.WriteLine($"Кэшировано {enemiesToRemove.Count} врагов из устаревших неактивных чанков.");
            }
        }
        
        /// <summary>
        /// Возвращает имя спрайта врага по его типу
        /// </summary>
        private string GetEnemySpriteName(EnemyType enemyType)
        {
            switch (enemyType)
            {
                case EnemyType.Basic:
                    return "enemy1";
                case EnemyType.Runner:
                    return "enemy2";
                case EnemyType.Tank:
                    return "enemy1"; // Использовать соответствующий спрайт
                case EnemyType.Bomber:
                    return "enemy1"; // Использовать соответствующий спрайт
                case EnemyType.Boss:
                    return "enemy1"; // Использовать соответствующий спрайт
                default:
                    return "enemy1";
            }
        }
        
        /// <summary>
        /// Восстанавливает врагов из их кэшированных состояний
        /// </summary>
        private void RestoreEnemiesFromState(List<EnemyState> enemyStates)
        {
            foreach (var state in enemyStates)
            {
                // Создаем нового врага на основе сохраненного состояния
                Enemy enemy = EnemyFactory.CreateEnemy(
                    type: state.Type,
                    x: state.X,
                    y: state.Y,
                    scoreLevel: _score,
                    spriteManager: _spriteManager
                );
                
                // Восстанавливаем сохраненные параметры врага
                if (enemy.TakeDamage(enemy.MaxHealth - state.Health))
                {
                    // Добавляем врага в игру
                    _enemies.Add(enemy);
                    
                    // Добавляем визуальные элементы врага на экран
                    _worldContainer.Children.Add(enemy.EnemyShape);
                    _worldContainer.Children.Add(enemy.HealthBar);
                    
                    enemy.UpdatePosition();
                    
                    Console.WriteLine($"Восстановлен враг типа {state.Type} на позиции {state.X}, {state.Y}");
                }
            }
        }

        /// <summary>
        /// Запускает задачу обработки врагов в отдельном потоке
        /// </summary>
        private void StartEnemyProcessingTask()
        {
            if (_enemyProcessingTask != null && !_enemyProcessingTask.IsCompleted)
            {
                return; // Задача уже запущена
            }

            _enemyProcessingCancellation = new CancellationTokenSource();
            
            _enemyProcessingTask = Task.Run(() => 
            {
                Console.WriteLine("Запущена многопоточная обработка врагов");
                
                try
                {
                    while (!_enemyProcessingCancellation.Token.IsCancellationRequested)
                    {
                        ProcessEnemiesInThreads();
                        
                        // Небольшая пауза для синхронизации с основным циклом
                        Thread.Sleep(10);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Задача отменена, выходим нормально
                    Console.WriteLine("Многопоточная обработка врагов отменена");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка в обработке врагов: {ex.Message}");
                }
            }, _enemyProcessingCancellation.Token);
        }

        /// <summary>
        /// Обрабатывает врагов в нескольких потоках
        /// </summary>
        private void ProcessEnemiesInThreads()
        {
            // Если потоки уже работают над врагами, пропускаем
            if (Interlocked.CompareExchange(ref _processingThreadCount, 0, 0) > 0)
                return;
        
            List<Enemy> enemiesCopy;
        
            // Копируем список врагов для безопасной работы
            lock (_enemiesLock)
            {
                if (_enemies.Count == 0)
                    return;
            
                enemiesCopy = new List<Enemy>(_enemies);
            }
        
            // Вычисляем оптимальное количество потоков
            int optimalThreadCount = Math.Max(1, Math.Min(
                Environment.ProcessorCount - 1, 
                (int)Math.Ceiling((double)enemiesCopy.Count / _maxEnemiesPerThread)
            ));
        
            // Разбиваем врагов на группы
            List<List<Enemy>> enemyGroups = new List<List<Enemy>>();
            int groupSize = (int)Math.Ceiling((double)enemiesCopy.Count / optimalThreadCount);
        
            for (int i = 0; i < enemiesCopy.Count; i += groupSize)
            {
                int count = Math.Min(groupSize, enemiesCopy.Count - i);
                enemyGroups.Add(enemiesCopy.GetRange(i, count));
            }
        
            // Запускаем потоки для обработки групп врагов
            Interlocked.Exchange(ref _processingThreadCount, enemyGroups.Count);
        
            // Запускаем задачи для каждой группы
            foreach (var group in enemyGroups)
            {
                Task.Run(() => 
                {
                    try
                    {
                        // Получаем позицию игрока для вычислений
                        double playerX = _player.X;
                        double playerY = _player.Y;
                    
                        foreach (var enemy in group)
                        {
                            // Проверяем, находится ли враг в поле зрения
                            if (_camera.IsInViewExtended(enemy.X, enemy.Y, 200))
                            {
                                // Вычисляем следующую позицию врага
                                enemy.MoveTowardsPlayer(playerX, playerY, 1.0 / 60.0); // фиксированный deltaTime
                            }
                        }
                    }
                    finally
                    {
                        // Уменьшаем счетчик активных потоков
                        Interlocked.Decrement(ref _processingThreadCount);
                    }
                });
            }
        }

        /// <summary>
        /// Освобождает ресурсы и останавливает фоновые потоки
        /// </summary>
        public void Dispose()
        {
            // Отменяем задачу обработки врагов
            if (_enemyProcessingCancellation != null)
            {
                _enemyProcessingCancellation.Cancel();
                
                try
                {
                    // Ждем завершения задачи, но не более 1 секунды
                    if (_enemyProcessingTask != null)
                        _enemyProcessingTask.Wait(1000);
                }
                catch { }
                
                _enemyProcessingCancellation.Dispose();
                _enemyProcessingCancellation = null;
            }
            
            // Отписываемся от события восстановления врагов
            if (_chunkManager != null)
            {
                _chunkManager.EnemiesReadyToRestore -= OnEnemiesReadyToRestore;
                _chunkManager.Dispose();
            }
            
            Console.WriteLine("GameManager ресурсы освобождены");
        }

        /// <summary>
        /// Обработчик события восстановления врагов
        /// </summary>
        private void OnEnemiesReadyToRestore(object sender, ChunkEnemyRestoreEventArgs e)
        {
            // Восстанавливаем врагов из их состояний
            RestoreEnemiesFromState(e.EnemiesToRestore);
        }

        // Метод для спавна сундука с сокровищами
        private void SpawnTreasureChest()
        {
            // Определяем позицию для сундука
            double x, y;
            int maxAttempts = 50;
            int attempts = 0;
            bool validPosition = false;
            
            do
            {
                attempts++;
                
                // Генерируем случайную позицию в пределах мира
                x = _random.NextDouble() * _worldWidth;
                y = _random.NextDouble() * _worldHeight;
                
                // Проверяем, что позиция достаточно далеко от игрока
                double distanceToPlayer = Math.Sqrt(Math.Pow(_player.X - x, 2) + Math.Pow(_player.Y - y, 2));
                if (distanceToPlayer < MIN_CHEST_SPAWN_DISTANCE_FROM_PLAYER)
                    continue;
                
                // Проверяем, что позиция достаточно далеко от других сундуков
                bool tooCloseToOtherChests = false;
                foreach (var chest in _treasureChests)
                {
                    double distanceToChest = Math.Sqrt(Math.Pow(chest.X - x, 2) + Math.Pow(chest.Y - y, 2));
                    if (distanceToChest < MIN_CHEST_SPAWN_DISTANCE_FROM_CHEST)
                    {
                        tooCloseToOtherChests = true;
                        break;
                    }
                }
                
                if (tooCloseToOtherChests)
                    continue;
                
                // Проверяем, что позиция на проходимом тайле
                if (!IsTileWalkable(x, y))
                    continue;
                
                validPosition = true;
            } while (!validPosition && attempts < maxAttempts);
            
            if (validPosition)
            {
                // Создаем сундук
                TreasureChest chest = new TreasureChest(x, y, _spriteManager);
                _treasureChests.Add(chest);
                
                // Добавляем изображение сундука на канвас
                _worldContainer.Children.Add(chest.ChestImage);
                
                Console.WriteLine($"Сундук с сокровищами создан на позиции ({x:F1}, {y:F1})");
            }
        }
        
        // Метод для проверки взаимодействия игрока с сундуками
        private void CheckChestInteractions()
        {
            foreach (var chest in _treasureChests)
            {
                if (chest.IsCollected)
                    continue;
                
                if (chest.IsPlayerInRange(_player) && !chest.IsOpen)
                {
                    chest.Open();
                    string treasureDescription = chest.Collect();

                    // Сначала показываем общее уведомление о находке
                    TreasureFound?.Invoke(this, treasureDescription);

                    // Затем применяем эффект и показываем конкретное уведомление
                    ApplyChestEffect(chest);

                    // Запланируем удаление сундука
                    Task.Delay(5000).ContinueWith(_ => 
                    {
                        Application.Current.Dispatcher.Invoke(() => 
                        {
                            if (_treasureChests.Contains(chest))
                            {
                                _worldContainer.Children.Remove(chest.ChestImage);
                                _treasureChests.Remove(chest);
                            }
                        });
                    });
                }
            }
        }
        
        // Метод для применения эффекта сундука
        private void ApplyChestEffect(TreasureChest chest)
        {
            if (chest.TreasureType == TreasureType.SkillPoints)
            {
                // Вызываем событие для добавления очков навыков
                SkillPointsAdded?.Invoke(this, chest.SkillPointsAmount);
            }
            else if (chest.TreasureType == TreasureType.Experience)
            {
                // Вызываем событие для добавления опыта
                ExperienceAdded?.Invoke(this, chest.ExperienceAmount);
            }
            else
            {
                // Проверяем, есть ли уже активный бонус такого типа
                TemporaryBoost existingBoost = _activeBoosts.Find(b => b.BoostType == chest.TreasureType);
                
                if (existingBoost != null)
                {
                    existingBoost.Extend(chest.BoostDuration);
                    if (existingBoost.Amount < chest.BoostAmount)
                    {
                        double oldAmount = existingBoost.Amount;
                        existingBoost.Increase(chest.BoostAmount - oldAmount);
                    }
                    BoostActivated?.Invoke(this, $"Бонус продлен на {chest.BoostDuration:F0} сек.!");
                }
                else
                {
                    TemporaryBoost newBoost = new TemporaryBoost(chest.TreasureType, chest.BoostAmount, chest.BoostDuration);
                    _activeBoosts.Add(newBoost);
                    ApplyBoostToPlayer(newBoost, true);
                    BoostActivated?.Invoke(this, $"Бонус '{newBoost.GetBoostName()}' активирован!");
                }
            }
        }
        
        // Метод для обновления активных бонусов
        private void UpdateActiveBoosts(double deltaTime)
        {
            List<TemporaryBoost> boostsToRemove = new List<TemporaryBoost>();
            
            foreach (var boost in _activeBoosts)
            {
                // Запоминаем, был ли бонус активен до обновления
                bool wasActive = boost.IsActive;
                
                // Обновляем оставшееся время
                boost.Update(deltaTime);
                
                // Если бонус был активен, но стал неактивным, снимаем его эффект
                if (wasActive && !boost.IsActive)
                {
                    ApplyBoostToPlayer(boost, false);
                    boostsToRemove.Add(boost);
                }
            }
            
            // Удаляем неактивные бонусы
            foreach (var boost in boostsToRemove)
            {
                _activeBoosts.Remove(boost);
            }
        }
        
        // Метод для применения/снятия бонуса к игроку
        private void ApplyBoostToPlayer(TemporaryBoost boost, bool apply)
        {
            double amount = apply ? boost.Amount : -boost.Amount;
            
            switch (boost.BoostType)
            {
                case TreasureType.HealthRegenBoost:
                    _player.Heal(0); // Вызываем метод для обновления UI
                    break;
                case TreasureType.MaxHealthBoost:
                    if (apply)
                    {
                        _player.UpgradeMaxHealth(boost.Amount);
                    }
                    else
                    {
                        _player.ReduceMaxHealth(boost.Amount);
                    }
                    break;
                case TreasureType.BulletSpeedBoost:
                    _player.ModifyBulletSpeed(amount);
                    break;
                case TreasureType.BulletDamageBoost:
                    _player.ModifyBulletDamage(amount);
                    break;
                case TreasureType.ReloadSpeedBoost:
                    _player.ModifyReloadSpeed(amount);
                    break;
                case TreasureType.MovementSpeedBoost:
                    _player.ModifyMovementSpeed(amount);
                    break;
            }
        }
        
        // Метод для получения информации об активных бонусах
        public List<string> GetActiveBoostsInfo()
        {
            List<string> boostsInfo = new List<string>();
            
            foreach (var boost in _activeBoosts)
            {
                string boostName = "";
                string boostValue = "";
                
                switch (boost.BoostType)
                {
                    case TreasureType.HealthRegenBoost:
                        boostName = "Регенерация";
                        boostValue = $"+{boost.Amount}";
                        break;
                    case TreasureType.MaxHealthBoost:
                        boostName = "Макс. здоровье";
                        boostValue = $"+{boost.Amount}";
                        break;
                    case TreasureType.BulletSpeedBoost:
                        boostName = "Скорость пуль";
                        boostValue = $"+{boost.Amount * 100}%";
                        break;
                    case TreasureType.BulletDamageBoost:
                        boostName = "Урон пуль";
                        boostValue = $"+{boost.Amount * 100}%";
                        break;
                    case TreasureType.ReloadSpeedBoost:
                        boostName = "Скорость перезарядки";
                        boostValue = $"+{boost.Amount * 100}%";
                        break;
                    case TreasureType.MovementSpeedBoost:
                        boostName = "Скорость движения";
                        boostValue = $"+{boost.Amount}";
                        break;
                }
                
                boostsInfo.Add($"{boostName}: {boostValue} ({boost.GetRemainingTimeText()})");
            }
            
            return boostsInfo;
        }
    }
} 