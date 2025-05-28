using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using GunVault.Models;
using GunVault.GameEngine;
using System.Threading.Tasks;
using System.Threading;

namespace GunVault;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Player? _player;
    private GameLoop? _gameLoop;
    private InputHandler? _inputHandler;
    private GameManager? _gameManager;
    private SpriteManager? _spriteManager; // Менеджер спрайтов
    private int _score = 0;
    
    // Таймер для автоматического скрытия уведомления
    private System.Windows.Threading.DispatcherTimer? _notificationTimer;
    
    // Флаг для отображения информации о размерах мира
    private bool _showDebugInfo = false;
    
    // Переменные для экрана загрузки
    private Task? _preloadTask;
    private CancellationTokenSource? _preloadCancellation;
    private int _totalChunksToLoad = 0;
    private int _loadedChunksCount = 0;
    private const int PRELOAD_RADIUS = 3; // Радиус предзагрузки чанков вокруг игрока (в чанках)
    private const int INITIAL_BUFFER_SIZE = 500; // Размер буферной зоны вокруг игрока (в пикселях)
    private System.Windows.Threading.DispatcherTimer? _loadingAnimationTimer; // Таймер для анимации текста загрузки
    
    public MainWindow()
    {
        InitializeComponent();
        
        // Инициализируем игру после загрузки окна
        Loaded += MainWindow_Loaded;
        
        // Добавляем обработчик закрытия окна
        Closing += MainWindow_Closing;
    }
    
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Инициализируем менеджер спрайтов
            try
            {
                _spriteManager = new SpriteManager();
                Console.WriteLine("SpriteManager успешно инициализирован");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при инициализации SpriteManager: {ex.Message}");
                // Продолжаем работу без менеджера спрайтов
                _spriteManager = null;
            }
            
            // Показываем экран загрузки
            LoadingScreen.Visibility = Visibility.Visible;
            LoadingStatusText.Text = "Инициализация игры...";
            UpdateLoadingProgress(0);
            
            // Инициализируем и запускаем таймер анимации загрузки
            InitializeLoadingAnimation();
            
            // Запускаем инициализацию игры с предзагрузкой в отдельном потоке
            _preloadCancellation = new CancellationTokenSource();
            _preloadTask = Task.Run(() => PreloadGame(_preloadCancellation.Token), _preloadCancellation.Token);
            
            // Инициализируем таймер для уведомлений
            _notificationTimer = new System.Windows.Threading.DispatcherTimer();
            _notificationTimer.Tick += NotificationTimer_Tick;
            _notificationTimer.Interval = TimeSpan.FromSeconds(4); // Уведомление исчезнет через 4 секунды
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при запуске игры: {ex.Message}\n\n{ex.StackTrace}", 
                "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void InitializeGame()
    {
        try
        {
            // Рассчитываем размеры мира (в 3 раза больше экрана)
            double worldWidth = GameCanvas.ActualWidth * 3.0; // WORLD_SIZE_MULTIPLIER из GameManager
            double worldHeight = GameCanvas.ActualHeight * 3.0;

            // Инициализируем игрока в центре МИРА, а не экрана
            double centerX = worldWidth / 2;
            double centerY = worldHeight / 2;
            _player = new Player(centerX, centerY, _spriteManager);
            
            // Инициализируем обработчик ввода
            _inputHandler = new InputHandler(_player);
            
            // Добавляем игрока на канвас
            GameCanvas.Children.Add(_player.PlayerShape);
            
            // Метод AddColliderVisualToCanvas все еще существует, но уже ничего не делает
            _player.AddColliderVisualToCanvas(GameCanvas);
            
            // Инициализируем менеджер игры, передаем менеджер спрайтов
            _gameManager = new GameManager(GameCanvas, _player, GameCanvas.ActualWidth, GameCanvas.ActualHeight, _spriteManager);
            _gameManager.ScoreChanged += GameManager_ScoreChanged;
            _gameManager.WeaponChanged += GameManager_WeaponChanged;
            
            // Инициализируем игровой цикл
            _gameLoop = new GameLoop(_gameManager, GameCanvas.ActualWidth, GameCanvas.ActualHeight);
            _gameLoop.GameTick += GameLoop_GameTick;
            
            // Запускаем игровой цикл
            _gameLoop.Start();
            
            // Фокус на канвас для обработки ввода
            GameCanvas.Focus();
            
            // Обновляем информацию об игроке
            UpdatePlayerInfo();
            
            Console.WriteLine("Игра успешно инициализирована");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при инициализации игры: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            Console.WriteLine($"Ошибка при инициализации игры: {ex}");
        }
    }
    
    // Обработка изменения счета
    private void GameManager_ScoreChanged(object sender, int newScore)
    {
        _score = newScore;
        UpdatePlayerInfo();
    }
    
    // Обработка изменения оружия
    private void GameManager_WeaponChanged(object sender, string weaponName)
    {
        // Вместо MessageBox показываем внутриигровое уведомление
        ShowWeaponNotification(weaponName);
    }
    
    // Показывает уведомление о получении нового оружия
    private void ShowWeaponNotification(string weaponName)
    {
        // Остановим таймер, если он уже запущен
        if (_notificationTimer!.IsEnabled)
        {
            _notificationTimer.Stop();
        }
        
        // Устанавливаем название оружия
        NotificationWeaponName.Text = weaponName;
        
        // Показываем уведомление с анимацией появления
        WeaponNotification.Opacity = 0;
        WeaponNotification.Visibility = Visibility.Visible;
        
        // Сначала создаем анимацию "выдвижения" сверху
        ThicknessAnimation slideDownAnimation = new ThicknessAnimation
        {
            From = new Thickness(0, -100, 0, 0),
            To = new Thickness(0, 0, 0, 0),
            Duration = TimeSpan.FromSeconds(0.5),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        
        // Затем создаем анимацию появления
        DoubleAnimation fadeInAnimation = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromSeconds(0.5)
        };
        
        // Запускаем анимации
        WeaponNotification.BeginAnimation(Border.MarginProperty, slideDownAnimation);
        WeaponNotification.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
        
        // Запускаем таймер для автоматического скрытия
        _notificationTimer.Start();
    }
    
    // Автоматически скрывает уведомление по таймеру
    private void NotificationTimer_Tick(object sender, EventArgs e)
    {
        _notificationTimer!.Stop();
        HideNotification();
    }
    
    // Скрывает уведомление с анимацией
    private void HideNotification()
    {
        // Анимация скрытия вверх
        ThicknessAnimation slideUpAnimation = new ThicknessAnimation
        {
            From = new Thickness(0, 0, 0, 0),
            To = new Thickness(0, -100, 0, 0),
            Duration = TimeSpan.FromSeconds(0.5),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        
        // Анимация прозрачности
        DoubleAnimation fadeOutAnimation = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromSeconds(0.5)
        };
        
        // По завершении анимации скрываем элемент полностью
        fadeOutAnimation.Completed += (s, e) => WeaponNotification.Visibility = Visibility.Collapsed;
        
        // Запускаем анимации
        WeaponNotification.BeginAnimation(Border.MarginProperty, slideUpAnimation);
        WeaponNotification.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
    }
    
    // Обновление информации об игроке
    private void GameLoop_GameTick(object sender, EventArgs e)
    {
        UpdatePlayerInfo();
    }
    
    // Обновление отображаемой информации
    private void UpdatePlayerInfo()
    {
        // Обновляем информацию о здоровье игрока
        HealthText.Text = $"Здоровье: {_player?.Health:F0}";
        
        // Обновляем информацию об оружии
        WeaponText.Text = $"Оружие: {_player?.GetWeaponName()}";
        
        // Обновляем информацию о боеприпасах
        AmmoText.Text = $"Патроны: {_player?.GetAmmoInfo()}";
        
        // Обновляем счет
        ScoreText.Text = $"Счёт: {_score}";
        
        // Отображаем отладочную информацию, если включено
        if (_showDebugInfo)
        {
            DebugInfoText.Text = $"Поз. игрока: ({_player?.X:F0}, {_player?.Y:F0})";
            DebugInfoText.Visibility = Visibility.Visible;
        }
        else
        {
            DebugInfoText.Visibility = Visibility.Collapsed;
        }
    }
    
    // Отображение обычного уведомления
    private void ShowNotification(string message)
    {
        // Останавливаем таймер, если он активен
        if (_notificationTimer!.IsEnabled)
        {
            _notificationTimer.Stop();
        }
        
        // Устанавливаем текст уведомления
        NotificationWeaponName.Text = message;
        
        // Показываем уведомление с анимацией
        WeaponNotification.Opacity = 0;
        WeaponNotification.Visibility = Visibility.Visible;
        
        // Анимация появления
        DoubleAnimation fadeInAnimation = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromSeconds(0.3)
        };
        
        // Запускаем анимацию
        WeaponNotification.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
        
        // Запускаем таймер для автоматического скрытия
        _notificationTimer.Start();
    }
    
    // Обработка изменения размера окна
    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Игнорируем изменения размера, когда ширина или высота canvas равны 0 (при минимизации)
        if (GameCanvas.ActualWidth <= 1 || GameCanvas.ActualHeight <= 1)
        {
            return;
        }
            
        // Также игнорируем, если размер изменился незначительно (может быть вызвано восстановлением из минимизации)
        if (Math.Abs(e.PreviousSize.Width - e.NewSize.Width) < 10 && 
            Math.Abs(e.PreviousSize.Height - e.NewSize.Height) < 10)
        {
            return;
        }
            
        if (_gameLoop != null)
        {
            _gameLoop.ResizeGameArea(GameCanvas.ActualWidth, GameCanvas.ActualHeight);
        }
    }
    
    // Обработка нажатия клавиш
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (_inputHandler != null)
        {
            _inputHandler.HandleKeyDown(e);
        }
        
        // Передаем событие нажатия клавиш менеджеру игры
        if (_gameManager != null)
        {
            _gameManager.HandleKeyPress(e);
        }
        
        // Отображение/скрытие отладочной информации
        if (e.Key == Key.F3)
        {
            _showDebugInfo = !_showDebugInfo;
        }
    }
    
    // Обработка отпускания клавиш
    private void Window_KeyUp(object sender, KeyEventArgs e)
    {
        if (_inputHandler != null)
        {
            _inputHandler.HandleKeyUp(e);
        }
    }

    private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // Обрабатываем клавиши только если игра инициализирована
        if (_gameManager != null && _player != null)
        {
            // Обработка клавиш движения
            switch (e.Key)
            {
                case Key.W:
                _player.MovingUp = true;
                break;
                case Key.S:
                _player.MovingDown = true;
                break;
                case Key.A:
                _player.MovingLeft = true;
                break;
                case Key.D:
                _player.MovingRight = true;
                break;
            }
            
            // Обработка специальных клавиш
            if (e.Key == Key.R)
            {
                _player.StartReload();
            }
            // Переключаем отображение границ чанков
            else if (e.Key == Key.F3)
            {
                _gameManager.ToggleChunkBoundaries();
            }
            
            // Передаем событие нажатия клавиши в GameManager
            _gameManager.HandleKeyPress(e);
        }
    }

    private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Останавливаем таймер анимации
        _loadingAnimationTimer?.Stop();
        _loadingAnimationTimer = null;
        
        // Отменяем задачу предзагрузки, если она выполняется
        if (_preloadTask != null && !_preloadTask.IsCompleted)
        {
            _preloadCancellation?.Cancel();
            try
            {
                _preloadTask.Wait(500); // Ждем не более 500 мс
            }
            catch { }
        }
        
        // Освобождаем ресурсы при закрытии приложения
        if (_gameManager != null)
        {
            _gameManager.Dispose();
        }
        
        // Останавливаем игровой цикл
        if (_gameLoop != null)
        {
            _gameLoop.Stop();
        }
        
        // Освобождаем ресурсы менеджера чанков
        if (_gameManager?._levelGenerator != null)
        {
            var chunkManager = _gameManager.GetChunkManager();
            if (chunkManager != null)
            {
                (chunkManager as IDisposable)?.Dispose();
            }
        }
        
        // Очищаем ресурсы предзагрузки
        _preloadCancellation?.Dispose();
        _preloadCancellation = null;
        
        Console.WriteLine("Ресурсы игры освобождены при закрытии приложения.");
    }

    /// <summary>
    /// Предзагружает игру и чанки вокруг начальной позиции игрока
    /// </summary>
    private void PreloadGame(CancellationToken cancellationToken)
    {
        try
        {
            // Шаг 1: Инициализация базовых игровых компонентов
            Dispatcher.Invoke(() => {
                LoadingStatusText.Text = "Инициализация игровых компонентов...";
                UpdateLoadingProgress(5);
            });
            
            // Рассчитываем размеры мира (в 3 раза больше экрана)
            double worldWidth = 0, worldHeight = 0;
            double centerX = 0, centerY = 0;
            
            Dispatcher.Invoke(() => {
                worldWidth = GameCanvas.ActualWidth * 3.0;
                worldHeight = GameCanvas.ActualHeight * 3.0;
                centerX = worldWidth / 2;
                centerY = worldHeight / 2;
            });
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Шаг 2: Создание игрока
            Dispatcher.Invoke(() => {
                LoadingStatusText.Text = "Создание игрока...";
                UpdateLoadingProgress(10);
                
                _player = new Player(centerX, centerY, _spriteManager);
                _inputHandler = new InputHandler(_player);
                GameCanvas.Children.Add(_player.PlayerShape);
                _player.AddColliderVisualToCanvas(GameCanvas);
            });
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Шаг 3: Инициализация менеджера игры
            Dispatcher.Invoke(() => {
                LoadingStatusText.Text = "Инициализация игрового мира...";
                UpdateLoadingProgress(20);
                
                _gameManager = new GameManager(GameCanvas, _player, GameCanvas.ActualWidth, GameCanvas.ActualHeight, _spriteManager);
                _gameManager.ScoreChanged += GameManager_ScoreChanged;
                _gameManager.WeaponChanged += GameManager_WeaponChanged;
            });
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Шаг 4: Предварительная загрузка чанков вокруг игрока
            Dispatcher.Invoke(() => {
                LoadingStatusText.Text = "Предзагрузка мира вокруг игрока...";
                UpdateLoadingProgress(30);
            });
            
            // Получаем менеджер чанков
            ChunkManager chunkManager = _gameManager.GetChunkManager();
            
            // Определяем чанк игрока
            var (playerChunkX, playerChunkY) = Chunk.WorldToChunk(centerX, centerY);
            
            // Рассчитываем общее количество чанков для загрузки
            _totalChunksToLoad = (2 * PRELOAD_RADIUS + 1) * (2 * PRELOAD_RADIUS + 1);
            _loadedChunksCount = 0;
            
            // Предзагружаем чанки в большой зоне вокруг игрока
            for (int dy = -PRELOAD_RADIUS; dy <= PRELOAD_RADIUS; dy++)
            {
                for (int dx = -PRELOAD_RADIUS; dx <= PRELOAD_RADIUS; dx++)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    
                    int chunkX = playerChunkX + dx;
                    int chunkY = playerChunkY + dy;
                    
                    // Создаем чанк и ждем его загрузки
                    Chunk chunk = chunkManager.GetOrCreateChunk(chunkX, chunkY);
                    
                    // Определяем приоритет загрузки (ближние чанки - выше приоритет)
                    int distance = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    int loadPriority = 10 * distance; // 0 для центрального чанка, больше для дальних
                    
                    // Активируем чанки рядом с игроком
                    if (distance <= ChunkManager.ACTIVATION_DISTANCE)
                    {
                        chunk.IsActive = true;
                    }
                    
                    // Имитируем ожидание загрузки чанка
                    int waitTime = 10 + loadPriority; // Чем дальше чанк, тем дольше загрузка
                    Thread.Sleep(waitTime);
                    
                    // Обновляем прогресс
                    _loadedChunksCount++;
                    int progressPercent = 30 + (int)(60.0 * _loadedChunksCount / _totalChunksToLoad);
                    
                    Dispatcher.Invoke(() => {
                        UpdateLoadingProgress(progressPercent);
                        LoadingStatusText.Text = $"Загружено чанков: {_loadedChunksCount}/{_totalChunksToLoad}";
                    });
                }
            }
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Шаг 5: Завершение инициализации
            Dispatcher.Invoke(() => {
                LoadingStatusText.Text = "Запуск игрового цикла...";
                UpdateLoadingProgress(95);
                
                // Инициализируем игровой цикл
                _gameLoop = new GameLoop(_gameManager, GameCanvas.ActualWidth, GameCanvas.ActualHeight);
                _gameLoop.GameTick += GameLoop_GameTick;
                
                // Обновляем активные чанки вокруг игрока
                _gameManager.GetChunkManager().UpdateActiveChunks(_player.X, _player.Y, _player.VelocityX, _player.VelocityY);
                
                LoadingStatusText.Text = "Загрузка завершена!";
                UpdateLoadingProgress(100);
            });
            
            // Небольшая пауза, чтобы показать 100% загрузки
            Thread.Sleep(500);
            
            // Запускаем игру в UI потоке
            Dispatcher.Invoke(() => {
                // Останавливаем таймер анимации
                _loadingAnimationTimer?.Stop();
                
                // Скрываем экран загрузки
                LoadingScreen.Visibility = Visibility.Collapsed;
                
                // Запускаем игровой цикл
                _gameLoop.Start();
                
                // Фокус на канвас для обработки ввода
                GameCanvas.Focus();
                
                // Обновляем информацию об игроке
                UpdatePlayerInfo();
                
                Console.WriteLine("Игра успешно инициализирована");
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => {
                MessageBox.Show($"Ошибка при инициализации игры: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"Ошибка при инициализации игры: {ex}");
                
                // Скрываем экран загрузки при ошибке
                LoadingScreen.Visibility = Visibility.Collapsed;
            });
        }
    }

    /// <summary>
    /// Обновляет отображаемый прогресс загрузки
    /// </summary>
    private void UpdateLoadingProgress(int percent)
    {
        LoadingProgressBar.Value = percent;
        LoadingProgressText.Text = $"{percent}%";
    }

    /// <summary>
    /// Инициализирует и запускает анимацию текста загрузки
    /// </summary>
    private void InitializeLoadingAnimation()
    {
        _loadingAnimationTimer = new System.Windows.Threading.DispatcherTimer();
        _loadingAnimationTimer.Tick += LoadingAnimation_Tick;
        _loadingAnimationTimer.Interval = TimeSpan.FromMilliseconds(500); // Обновление каждые 500 мс
        _loadingAnimationTimer.Start();
    }

    /// <summary>
    /// Обрабатывает тик таймера анимации загрузки
    /// </summary>
    private int _animationDotCount = 0;
    private void LoadingAnimation_Tick(object sender, EventArgs e)
    {
        if (LoadingStatusText.Text.EndsWith("..."))
        {
            // Обрезаем многоточие
            string baseText = LoadingStatusText.Text.Substring(0, LoadingStatusText.Text.Length - 3);
            LoadingStatusText.Text = baseText;
            _animationDotCount = 0;
        }
        else
        {
            _animationDotCount++;
            LoadingStatusText.Text += ".";
        }
    }
}