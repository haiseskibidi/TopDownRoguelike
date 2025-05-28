using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GunVault.Models;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace GunVault.GameEngine
{
    public class ChunkManager : IDisposable
    {
        public const int ACTIVATION_DISTANCE = 1;
        private const int MAX_ACTIVE_CHUNKS = 100;
        private Dictionary<string, Chunk> _chunks;
        private List<Chunk> _activeChunks;
        private Canvas _worldContainer;
        private bool _showChunkBoundaries;
        private bool _useAsyncLoading = true;
        private ConcurrentQueue<ChunkLoadRequest> _chunkLoadQueue;
        private CancellationTokenSource _loadTaskCancellation;
        private Task _chunkLoadTask;
        private ConcurrentDictionary<string, Task> _loadingChunks;
        private int _preloadDistance = 2;
        public event EventHandler<ChunkEnemyRestoreEventArgs> EnemiesReadyToRestore;
        
        public ChunkManager(Canvas worldContainer)
        {
            _chunks = new Dictionary<string, Chunk>();
            _activeChunks = new List<Chunk>();
            _worldContainer = worldContainer;
            _showChunkBoundaries = false;
            _chunkLoadQueue = new ConcurrentQueue<ChunkLoadRequest>();
            _loadingChunks = new ConcurrentDictionary<string, Task>();
            _loadTaskCancellation = new CancellationTokenSource();
            
            if (_useAsyncLoading)
            {
                StartChunkLoadTask();
            }
        }
        
        public Chunk GetOrCreateChunk(int chunkX, int chunkY)
        {
            string key = GetChunkKey(chunkX, chunkY);
            
            if (!_chunks.TryGetValue(key, out Chunk chunk))
            {
                if (_useAsyncLoading)
                {
                    int priority = 0;
                    
                    if (!_loadingChunks.ContainsKey(key))
                    {
                        _chunkLoadQueue.Enqueue(new ChunkLoadRequest(chunkX, chunkY, priority));
                    }
                    
                    chunk = new Chunk(chunkX, chunkY);
                    _chunks.Add(key, chunk);
                }
                else
                {
                    chunk = new Chunk(chunkX, chunkY);
                    _chunks.Add(key, chunk);
                    
                    if (_showChunkBoundaries)
                    {
                        CreateChunkDebugMarker(chunk);
                    }
                }
                
                chunk.OnChunkActivated += Chunk_OnActivated;
            }
            
            return chunk;
        }
        
        private void Chunk_OnActivated(object sender, EventArgs e)
        {
            if (sender is Chunk chunk)
            {
                List<EnemyState> cachedEnemies = chunk.GetCachedEnemies();
                
                if (cachedEnemies.Count > 0)
                {
                    EnemiesReadyToRestore?.Invoke(this, new ChunkEnemyRestoreEventArgs(cachedEnemies));
                    chunk.ClearCachedEnemies();
                    Console.WriteLine($"Восстановлено {cachedEnemies.Count} врагов в чанке {chunk.ChunkX}:{chunk.ChunkY}");
                }
            }
        }
        
        public void UpdateActiveChunks(double playerX, double playerY, double velocityX = 0, double velocityY = 0)
        {
            var (playerChunkX, playerChunkY) = Chunk.WorldToChunk(playerX, playerY);
            
            foreach (var chunk in _activeChunks)
            {
                chunk.IsActive = false;
            }
            
            _activeChunks.Clear();
            
            for (int y = playerChunkY - ACTIVATION_DISTANCE; y <= playerChunkY + ACTIVATION_DISTANCE; y++)
            {
                for (int x = playerChunkX - ACTIVATION_DISTANCE; x <= playerChunkX + ACTIVATION_DISTANCE; x++)
                {
                    Chunk chunk = GetOrCreateChunk(x, y);
                    chunk.IsActive = true;
                    _activeChunks.Add(chunk);
                }
            }
            
            if (_useAsyncLoading && (velocityX != 0 || velocityY != 0))
            {
                PreloadChunksInDirection(playerChunkX, playerChunkY, velocityX, velocityY);
            }
            
            Console.WriteLine($"Активировано {_activeChunks.Count} чанков вокруг игрока (чанк: {playerChunkX}, {playerChunkY})");
        }
        
        public List<Chunk> GetActiveChunks()
        {
            return _activeChunks;
        }
        
        public bool IsPointInActiveChunk(double worldX, double worldY)
        {
            var (chunkX, chunkY) = Chunk.WorldToChunk(worldX, worldY);
            string key = GetChunkKey(chunkX, chunkY);
            
            if (_chunks.TryGetValue(key, out Chunk chunk))
            {
                return chunk.IsActive;
            }
            
            return false;
        }
        
        public void AddTileCollider(string colliderKey, RectCollider collider)
        {
            var (chunkX, chunkY) = Chunk.WorldToChunk(collider.X, collider.Y);
            Chunk chunk = GetOrCreateChunk(chunkX, chunkY);
            chunk.AddTileCollider(colliderKey, collider);
        }
        
        public Dictionary<string, RectCollider> GetActiveChunkColliders()
        {
            Dictionary<string, RectCollider> colliders = new Dictionary<string, RectCollider>();
            
            foreach (var chunk in _activeChunks)
            {
                foreach (var collider in chunk.GetTileColliders())
                {
                    colliders.Add(collider.Key, collider.Value);
                }
            }
            
            return colliders;
        }
        
        public void ToggleChunkBoundaries(bool show)
        {
            if (_showChunkBoundaries == show) return;
            
            _showChunkBoundaries = show;
            
            if (show)
            {
                foreach (var chunk in _chunks.Values)
                {
                    if (chunk.DebugMarker == null)
                    {
                        CreateChunkDebugMarker(chunk);
                    }
                }
            }
            else
            {
                foreach (var chunk in _chunks.Values)
                {
                    if (chunk.DebugMarker != null)
                    {
                        _worldContainer.Children.Remove(chunk.DebugMarker);
                        chunk.DebugMarker = null;
                    }
                }
            }
        }
        
        private void CreateChunkDebugMarker(Chunk chunk)
        {
            if (chunk.DebugMarker != null)
            {
                _worldContainer.Children.Remove(chunk.DebugMarker);
            }
            
            Rectangle marker = new Rectangle
            {
                Width = chunk.PixelSize,
                Height = chunk.PixelSize,
                Stroke = chunk.IsActive ? Brushes.Green : Brushes.Red,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 4 },
                Fill = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0))
            };
            
            Canvas.SetLeft(marker, chunk.WorldX);
            Canvas.SetTop(marker, chunk.WorldY);
            Panel.SetZIndex(marker, 9000);
            
            _worldContainer.Children.Add(marker);
            chunk.DebugMarker = marker;
        }
        
        public void UpdateChunkMarkers()
        {
            if (!_showChunkBoundaries) return;
            
            foreach (var chunk in _chunks.Values)
            {
                if (chunk.DebugMarker is Rectangle marker)
                {
                    marker.Stroke = chunk.IsActive ? Brushes.Green : Brushes.Red;
                }
            }
        }
        
        private string GetChunkKey(int x, int y)
        {
            return $"{x}:{y}";
        }
        
        public void Clear()
        {
            if (_showChunkBoundaries)
            {
                foreach (var chunk in _chunks.Values)
                {
                    if (chunk.DebugMarker != null)
                    {
                        _worldContainer.Children.Remove(chunk.DebugMarker);
                    }
                }
            }
            
            _chunks.Clear();
            _activeChunks.Clear();
        }
        
        public bool IsInInactiveStaleChunk(double worldX, double worldY, TimeSpan staleTime)
        {
            var (chunkX, chunkY) = Chunk.WorldToChunk(worldX, worldY);
            string key = GetChunkKey(chunkX, chunkY);
            
            if (_chunks.TryGetValue(key, out Chunk chunk))
            {
                if (chunk.IsActive)
                {
                    return false;
                }
                
                TimeSpan timeSinceLastActive = DateTime.Now - chunk.LastActiveTime;
                return timeSinceLastActive > staleTime.Add(TimeSpan.FromSeconds(1));
            }
            
            return false;
        }
        
        public List<Chunk> GetStaleChunks(TimeSpan staleTime)
        {
            List<Chunk> staleChunks = new List<Chunk>();
            DateTime now = DateTime.Now;
            
            foreach (var chunk in _chunks.Values)
            {
                if (!chunk.IsActive)
                {
                    TimeSpan timeSinceLastActive = now - chunk.LastActiveTime;
                    if (timeSinceLastActive > staleTime)
                    {
                        staleChunks.Add(chunk);
                    }
                }
            }
            
            return staleChunks;
        }

        /// <summary>
        /// Класс для представления запроса на загрузку чанка
        /// </summary>
        private class ChunkLoadRequest
        {
            public int ChunkX { get; }
            public int ChunkY { get; }
            public int Priority { get; } // Приоритет загрузки (чем ниже, тем выше приоритет)
            
            public ChunkLoadRequest(int chunkX, int chunkY, int priority)
            {
                ChunkX = chunkX;
                ChunkY = chunkY;
                Priority = priority;
            }
            
            public string GetKey()
            {
                return $"{ChunkX}:{ChunkY}";
            }
        }

        /// <summary>
        /// Запускает задачу асинхронной загрузки чанков
        /// </summary>
        private void StartChunkLoadTask()
        {
            if (_chunkLoadTask != null && !_chunkLoadTask.IsCompleted)
            {
                return; // Задача уже запущена
            }
            
            _loadTaskCancellation = new CancellationTokenSource();
            
            _chunkLoadTask = Task.Run(() =>
            {
                Console.WriteLine("Запущена асинхронная загрузка чанков");
                
                try
                {
                    while (!_loadTaskCancellation.Token.IsCancellationRequested)
                    {
                        ProcessChunkLoadQueue();
                        
                        // Небольшая пауза между проверками очереди
                        Thread.Sleep(10);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Задача отменена, выходим нормально
                    Console.WriteLine("Асинхронная загрузка чанков отменена");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка в обработке загрузки чанков: {ex.Message}");
                }
            }, _loadTaskCancellation.Token);
        }

        /// <summary>
        /// Обрабатывает очередь загрузки чанков
        /// </summary>
        private void ProcessChunkLoadQueue()
        {
            // Если очередь пуста, ничего не делаем
            if (_chunkLoadQueue.IsEmpty)
                return;
            
            // Пытаемся извлечь запрос из очереди
            if (_chunkLoadQueue.TryDequeue(out ChunkLoadRequest request))
            {
                string key = request.GetKey();
                
                // Проверяем, не загружается ли уже этот чанк
                if (_loadingChunks.ContainsKey(key))
                    return;
                
                // Проверяем, не существует ли уже этот чанк
                if (_chunks.ContainsKey(key))
                    return;
                
                // Создаем задачу для загрузки чанка
                Task loadTask = Task.Run(() =>
                {
                    try
                    {
                        // Создаем новый чанк
                        Chunk chunk = new Chunk(request.ChunkX, request.ChunkY);
                        
                        // Инициализируем чанк (в реальном приложении здесь может быть загрузка данных)
                        InitializeChunk(chunk);
                        
                        // Добавляем чанк в словарь
                        _chunks[key] = chunk;
                        
                        // Создаем визуальный маркер, если нужно
                        if (_showChunkBoundaries)
                        {
                            // Выполняем в основном потоке UI
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                CreateChunkDebugMarker(chunk);
                            });
                        }
                        
                        Console.WriteLine($"Асинхронно загружен чанк {request.ChunkX}:{request.ChunkY}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка загрузки чанка {request.ChunkX}:{request.ChunkY}: {ex.Message}");
                    }
                    finally
                    {
                        // Удаляем чанк из списка загружаемых
                        _loadingChunks.TryRemove(key, out _);
                    }
                }, _loadTaskCancellation.Token);
                
                // Регистрируем задачу в словаре загружаемых чанков
                _loadingChunks[key] = loadTask;
            }
        }

        /// <summary>
        /// Инициализирует чанк (заглушка, в реальном приложении здесь может быть загрузка данных)
        /// </summary>
        private void InitializeChunk(Chunk chunk)
        {
            // В реальном приложении здесь может быть загрузка данных из файла или генерация контента
            // Например, можно загрузить тайлы, объекты, препятствия и т.д.
            
            // Имитация задержки загрузки
            Thread.Sleep(10);
        }

        /// <summary>
        /// Освобождает ресурсы и останавливает фоновые потоки
        /// </summary>
        public void Dispose()
        {
            // Отменяем задачу загрузки чанков
            if (_loadTaskCancellation != null)
            {
                _loadTaskCancellation.Cancel();
                
                try
                {
                    // Ждем завершения задачи, но не более 1 секунды
                    if (_chunkLoadTask != null)
                        _chunkLoadTask.Wait(1000);
                }
                catch { }
                
                _loadTaskCancellation.Dispose();
                _loadTaskCancellation = null;
            }
            
            Console.WriteLine("ChunkManager ресурсы освобождены");
        }

        /// <summary>
        /// Предварительно загружает чанки в указанном направлении от игрока
        /// </summary>
        /// <param name="playerChunkX">X-координата чанка игрока</param>
        /// <param name="playerChunkY">Y-координата чанка игрока</param>
        /// <param name="playerVelocityX">Скорость игрока по X</param>
        /// <param name="playerVelocityY">Скорость игрока по Y</param>
        public void PreloadChunksInDirection(int playerChunkX, int playerChunkY, double playerVelocityX, double playerVelocityY)
        {
            if (!_useAsyncLoading)
                return;
            
            // Определяем направление движения игрока
            int directionX = Math.Sign(playerVelocityX);
            int directionY = Math.Sign(playerVelocityY);
            
            // Если игрок не двигается, предзагружаем во всех направлениях
            if (directionX == 0 && directionY == 0)
            {
                PreloadChunksAround(playerChunkX, playerChunkY, _preloadDistance);
                return;
            }
            
            // Предзагружаем чанки в направлении движения с приоритетом
            for (int distance = 1; distance <= _preloadDistance; distance++)
            {
                int priority = distance; // Чем дальше от игрока, тем ниже приоритет
                
                // Предзагрузка в основном направлении
                QueueChunkForLoading(playerChunkX + directionX * distance, 
                                    playerChunkY + directionY * distance, 
                                    priority);
                
                // Предзагрузка по диагонали (если движение по диагонали)
                if (directionX != 0 && directionY != 0)
                {
                    QueueChunkForLoading(playerChunkX + directionX * distance, 
                                        playerChunkY, 
                                        priority + 1);
                                        
                    QueueChunkForLoading(playerChunkX, 
                                        playerChunkY + directionY * distance, 
                                        priority + 1);
                }
                
                // Предзагрузка в боковых направлениях
                if (directionX != 0)
                {
                    QueueChunkForLoading(playerChunkX + directionX * distance, 
                                        playerChunkY + 1, 
                                        priority + 2);
                                        
                    QueueChunkForLoading(playerChunkX + directionX * distance, 
                                        playerChunkY - 1, 
                                        priority + 2);
                }
                
                if (directionY != 0)
                {
                    QueueChunkForLoading(playerChunkX + 1, 
                                        playerChunkY + directionY * distance, 
                                        priority + 2);
                                        
                    QueueChunkForLoading(playerChunkX - 1, 
                                        playerChunkY + directionY * distance, 
                                        priority + 2);
                }
            }
        }

        /// <summary>
        /// Предварительно загружает чанки вокруг указанной позиции
        /// </summary>
        private void PreloadChunksAround(int centerX, int centerY, int radius)
        {
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    // Пропускаем центральный чанк (он уже должен быть загружен)
                    if (x == centerX && y == centerY)
                        continue;
                        
                    // Вычисляем расстояние от центра для определения приоритета
                    int distance = Math.Max(Math.Abs(x - centerX), Math.Abs(y - centerY));
                    int priority = distance * 2; // Чем дальше от центра, тем ниже приоритет
                    
                    QueueChunkForLoading(x, y, priority);
                }
            }
        }

        /// <summary>
        /// Добавляет чанк в очередь на загрузку, если он еще не загружен
        /// </summary>
        private void QueueChunkForLoading(int chunkX, int chunkY, int priority)
        {
            string key = GetChunkKey(chunkX, chunkY);
            
            // Проверяем, существует ли уже чанк или находится в процессе загрузки
            if (_chunks.ContainsKey(key) || _loadingChunks.ContainsKey(key))
                return;
                
            // Добавляем в очередь загрузки
            _chunkLoadQueue.Enqueue(new ChunkLoadRequest(chunkX, chunkY, priority));
        }

        /// <summary>
        /// Сохраняет состояние чанка для последующего восстановления
        /// </summary>
        /// <param name="chunk">Чанк, состояние которого нужно сохранить</param>
        /// <returns>Объект с сохраненным состоянием чанка</returns>
        private ChunkState SaveChunkState(Chunk chunk)
        {
            return new ChunkState
            {
                ChunkX = chunk.ChunkX,
                ChunkY = chunk.ChunkY,
                LastActiveTime = chunk.LastActiveTime
            };
        }

        /// <summary>
        /// Восстанавливает чанк из сохраненного состояния
        /// </summary>
        /// <param name="state">Сохраненное состояние чанка</param>
        /// <returns>Восстановленный чанк</returns>
        private Chunk RestoreChunkFromState(ChunkState state)
        {
            var chunk = new Chunk(state.ChunkX, state.ChunkY);
            // Можно добавить больше параметров для восстановления
            return chunk;
        }

        /// <summary>
        /// Класс для хранения состояния чанка
        /// </summary>
        private class ChunkState
        {
            public int ChunkX { get; set; }
            public int ChunkY { get; set; }
            public DateTime LastActiveTime { get; set; }
            // Можно добавить другие параметры, которые нужно сохранить
        }

        /// <summary>
        /// Кэширует состояние врага в соответствующем чанке
        /// </summary>
        public void CacheEnemyState(EnemyState enemyState)
        {
            // Получаем чанк, в котором находится враг
            string chunkKey = GetChunkKey(enemyState.ChunkX, enemyState.ChunkY);
            
            if (_chunks.TryGetValue(chunkKey, out Chunk chunk))
            {
                chunk.CacheEnemy(enemyState);
                Console.WriteLine($"Состояние врага типа {enemyState.Type} кэшировано в чанке {enemyState.ChunkX}:{enemyState.ChunkY}");
            }
            else
            {
                // Если чанк не существует, создаем его и кэшируем состояние
                Chunk newChunk = GetOrCreateChunk(enemyState.ChunkX, enemyState.ChunkY);
                newChunk.CacheEnemy(enemyState);
                Console.WriteLine($"Создан новый чанк {enemyState.ChunkX}:{enemyState.ChunkY} для кэширования врага типа {enemyState.Type}");
            }
        }
    }
    
    /// <summary>
    /// Аргументы события восстановления врагов из чанка
    /// </summary>
    public class ChunkEnemyRestoreEventArgs : EventArgs
    {
        // Список состояний врагов для восстановления
        public List<EnemyState> EnemiesToRestore { get; private set; }
        
        public ChunkEnemyRestoreEventArgs(List<EnemyState> enemiesToRestore)
        {
            EnemiesToRestore = enemiesToRestore;
        }
    }
} 