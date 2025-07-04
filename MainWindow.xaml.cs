using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using GunVault.Models;
using GunVault.GameEngine;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Media;
using System.Collections.Generic;
using GunVault.Models.PlayerClasses;

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
    private System.Windows.Threading.DispatcherTimer? _statsNotificationTimer;
    private System.Windows.Threading.DispatcherTimer? _deathCheckTimer; // Таймер для проверки смерти игрока
    
    // Опыт и уровень игрока
    private int _playerLevel = 1;
    private int _playerExperience = 0;
    private int _experienceToNextLevel = 100;
    private int _skillPoints = 0;
    private bool _classSelectionShown = false;
    
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
    
    // Флаг для отслеживания состояния игрока
    private bool _playerIsDead = false;
    
    private System.Windows.Threading.DispatcherTimer? _activeBoostsUpdateTimer;
    private List<string> _activeBoosts = new List<string>();
    
    // Новая система для управления очередью уведомлений
    private readonly Queue<string> _notificationQueue = new Queue<string>();
    private bool _isShowingNotification = false;
    
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

            _statsNotificationTimer = new System.Windows.Threading.DispatcherTimer();
            _statsNotificationTimer.Tick += StatsNotificationTimer_Tick;
            _statsNotificationTimer.Interval = TimeSpan.FromSeconds(6); // Окно характеристик исчезнет через 6 секунд
            
            // Инициализируем таймер для проверки смерти игрока
            _deathCheckTimer = new System.Windows.Threading.DispatcherTimer();
            _deathCheckTimer.Tick += DeathCheckTimer_Tick;
            _deathCheckTimer.Interval = TimeSpan.FromSeconds(0.5); // Проверяем каждые 0.5 секунд
            _deathCheckTimer.Start();
            
            // Инициализируем таймер для обновления информации об активных бонусах
            _activeBoostsUpdateTimer = new System.Windows.Threading.DispatcherTimer();
            _activeBoostsUpdateTimer.Tick += ActiveBoostsUpdateTimer_Tick;
            _activeBoostsUpdateTimer.Interval = TimeSpan.FromSeconds(0.5); // Обновляем каждые 0.5 секунды
            _activeBoostsUpdateTimer.Start();
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
            
            // Инициализируем менеджер игры, передаем менеджер спрайтов
            _gameManager = new GameManager(GameCanvas, _player, GameCanvas.ActualWidth, GameCanvas.ActualHeight, _spriteManager);
            _gameManager.ScoreChanged += GameManager_ScoreChanged;
            _gameManager.WeaponChanged += GameManager_WeaponChanged;
            _gameManager.EnemyKilled += GameManager_EnemyKilled;
            _gameManager.TreasureFound += GameManager_TreasureFound;
            _gameManager.SkillPointsAdded += GameManager_SkillPointsAdded;
            _gameManager.ExperienceAdded += GameManager_ExperienceAdded;
            _gameManager.BoostActivated += GameManager_BoostActivated;
            
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
    
    private void GameManager_EnemyKilled(object sender, int experience)
    {
        AddExperience(experience);
    }

    // Обработчик события нахождения сундука
    private void GameManager_TreasureFound(object sender, string treasureDescription)
    {
        ShowTreasureNotification(treasureDescription);
    }
    
    // Обработчик события добавления очков навыков
    private void GameManager_SkillPointsAdded(object sender, int skillPoints)
    {
        _skillPoints += skillPoints;
        UpdatePlayerInfo();
        
        // Показываем окно характеристик
        ShowStatsNotification();
        
        // Показываем уведомление о получении очков навыков
        ShowSkillPointsNotification(skillPoints);
    }
    
    // Обработчик события добавления опыта
    private void GameManager_ExperienceAdded(object sender, int experience)
    {
        AddExperience(experience);
        
        // Показываем уведомление о получении опыта
        ShowExperienceNotification(experience);
    }
    
    // Вспомогательный метод для правильного склонения слова "очко"
    private string GetSkillPointsText(int amount)
    {
        if (amount % 10 == 1 && amount % 100 != 11)
            return "очко навыка";
        else if ((amount % 10 == 2 || amount % 10 == 3 || amount % 10 == 4) && 
                (amount % 100 < 10 || amount % 100 > 20))
            return "очка навыков";
        else
            return "очков навыков";
    }
    
    // Обработчик события активации бонуса
    private void GameManager_BoostActivated(object sender, string message)
    {
        ShowBoostNotification(message);
    }
    
    // Метод для отображения уведомления о сундуке
    private void ShowTreasureNotification(string message)
    {
        _notificationQueue.Enqueue(message);
        ProcessNotificationQueue();
    }
    
    // Метод для отображения уведомления о бонусе
    private void ShowBoostNotification(string message)
    {
        _notificationQueue.Enqueue(message);
        ProcessNotificationQueue();
    }
    
    // Обработчик таймера обновления активных бонусов
    private void ActiveBoostsUpdateTimer_Tick(object sender, EventArgs e)
    {
        if (_gameManager != null)
        {
            _activeBoosts = _gameManager.GetActiveBoostsInfo();
            UpdateActiveBoostsUI();
        }
    }
    
    // Метод для обновления UI активных бонусов
    private void UpdateActiveBoostsUI()
    {
        if (_activeBoosts.Count > 0)
        {
            // Очищаем предыдущие элементы, оставляя только заголовок
            while (ActiveBoostsList.Children.Count > 1)
            {
                ActiveBoostsList.Children.RemoveAt(1);
            }
            
            // Добавляем информацию о каждом активном бонусе
            foreach (var boostInfo in _activeBoosts)
            {
                TextBlock boostText = new TextBlock
                {
                    Text = boostInfo,
                    Foreground = Brushes.White,
                    FontSize = 12,
                    Margin = new Thickness(0, 2, 0, 2)
                };
                
                ActiveBoostsList.Children.Add(boostText);
            }
            
            // Показываем панель с бонусами
            ActiveBoostsPanel.Visibility = Visibility.Visible;
        }
        else
        {
            // Если нет активных бонусов, скрываем панель
            ActiveBoostsPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void AddExperience(int amount)
    {
        _playerExperience += amount;
        bool leveledUp = false;
        
        while (_playerExperience >= _experienceToNextLevel)
        {
            _playerLevel++;
            _skillPoints++;
            _playerExperience -= _experienceToNextLevel;
            _experienceToNextLevel = (int)(_experienceToNextLevel * 1.5); // Усложняем получение следующего уровня
            leveledUp = true;

            // Check if player reaches level 5 to show class selection, only once
            if (_playerLevel >= 5 && _player.ChosenClass == null && !_classSelectionShown)
            {
                _classSelectionShown = true;
                ShowClassSelectionScreen();
            }
        }
        
        UpdatePlayerInfo();
        
        // Если произошло повышение уровня, показываем окно характеристик
        if (leveledUp)
        {
            ShowStatsNotification();
        }
    }
    
    // Показывает уведомление о получении нового оружия
    private void ShowWeaponNotification(string weaponName)
    {
        string message = $"Новое оружие: {weaponName}";
        _notificationQueue.Enqueue(message);
        ProcessNotificationQueue();
    }
    
    private void ProcessNotificationQueue()
    {
        if (_isShowingNotification || _notificationQueue.Count == 0)
        {
            return;
        }

        _isShowingNotification = true;
        string message = _notificationQueue.Dequeue();

        NotificationText.Text = message;
        NotificationBorder.Visibility = Visibility.Visible;

        var fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5));
        var slideInAnimation = new ThicknessAnimation(new Thickness(0, -50, 0, 0), new Thickness(0), TimeSpan.FromSeconds(0.5));

        Storyboard sb = new Storyboard();
        sb.Children.Add(fadeInAnimation);
        sb.Children.Add(slideInAnimation);

        Storyboard.SetTarget(fadeInAnimation, NotificationBorder);
        Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(OpacityProperty));

        Storyboard.SetTarget(slideInAnimation, NotificationBorder);
        Storyboard.SetTargetProperty(slideInAnimation, new PropertyPath(MarginProperty));
        
        sb.Completed += (s, e) => 
        {
            // Запускаем таймер на скрытие после завершения анимации появления
        _notificationTimer.Start();
        };

        sb.Begin();
    }
    
    private void NotificationTimer_Tick(object? sender, EventArgs e)
    {
        _notificationTimer.Stop();
        HideNotification();
    }
    
    private void HideNotification()
    {
        var fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.5));
        var slideOutAnimation = new ThicknessAnimation(new Thickness(0), new Thickness(0, -50, 0, 0), TimeSpan.FromSeconds(0.5));

        Storyboard sb = new Storyboard();
        sb.Children.Add(fadeOutAnimation);
        sb.Children.Add(slideOutAnimation);

        Storyboard.SetTarget(fadeOutAnimation, NotificationBorder);
        Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath(OpacityProperty));
        
        Storyboard.SetTarget(slideOutAnimation, NotificationBorder);
        Storyboard.SetTargetProperty(slideOutAnimation, new PropertyPath(MarginProperty));

        sb.Completed += (s, e) =>
        {
            NotificationBorder.Visibility = Visibility.Collapsed;
            _isShowingNotification = false;
            // Проверяем, есть ли еще уведомления в очереди
            ProcessNotificationQueue(); 
        };

        sb.Begin();
    }
    
    // Обновление информации об игроке
    private void GameLoop_GameTick(object? sender, EventArgs e)
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
        
        // Обновляем уровень и опыт
        LevelText.Text = $"Уровень: {_playerLevel}";
        ExperienceBar.Value = _playerExperience;
        ExperienceBar.Maximum = _experienceToNextLevel;
        SkillPointsText.Text = $"Очки навыков: {_skillPoints}";
        
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
        
        // Проверяем здоровье игрока
        if (_player != null && _player.Health <= 0 && !_playerIsDead)
        {
            ShowDeathNotification();
        }
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
        _inputHandler?.HandleKeyDown(e);

        if (e.Key == Key.B)
        {
            // Открываем/закрываем окно характеристик по нажатию 'B'
            if (StatsNotification.Visibility == Visibility.Visible)
            {
                HideStatsNotification();
            }
            else
            {
                if (_skillPoints > 0)
                {
                    ShowStatsNotification();
                }
            }
        }
        
        if (e.Key == Key.Escape)
        {
            // Если открыто окно улучшений, закрываем его
            if (StatsNotification.Visibility == Visibility.Visible)
            {
                HideStatsNotification();
                return; // Выходим, чтобы не открывать меню паузы
            }
            
            // Если открыто окно выбора класса, закрываем его
            if (ClassSelectionGrid.Visibility == Visibility.Visible)
            {
                HideClassSelectionScreen();
                return; // Выходим, чтобы не открывать меню паузы
            }
            
            // Если другие окна не открыты, переключаем паузу
            TogglePause();
        }
        
        if (e.Key == Key.L)
        {
            AddExperience(50);
        }
        
        if (e.Key == Key.F3)
        {
            _showDebugInfo = !_showDebugInfo;
        }

        if (_skillPoints > 0 && StatsNotification.Visibility == Visibility.Visible)
        {
            bool usedSkillPoint = false;
            switch (e.Key)
            {
                case Key.D1:
                    if(_player.HealthRegenUpgradeLevel < Player.MAX_UPGRADE_LEVEL)
                    {
                        _player?.UpgradeHealthRegen();
                        _skillPoints--;
                        usedSkillPoint = true;
                    }
                    break;
                case Key.D2:
                    if(_player.MaxHealthUpgradeLevel < Player.MAX_UPGRADE_LEVEL)
                    {
                        _player?.UpgradeMaxHealth();
                        _skillPoints--;
                        usedSkillPoint = true;
                    }
                    break;
                case Key.D3:
                    if(_player.BulletSpeedUpgradeLevel < Player.MAX_UPGRADE_LEVEL)
                    {
                        _player?.UpgradeBulletSpeed();
                        _skillPoints--;
                        usedSkillPoint = true;
                    }
                    break;
                case Key.D4:
                    if(_player.BulletDamageUpgradeLevel < Player.MAX_UPGRADE_LEVEL)
                    {
                        _player?.UpgradeBulletDamage();
                        _skillPoints--;
                        usedSkillPoint = true;
                    }
                    break;
                case Key.D5:
                    if(_player.FireRateUpgradeLevel < Player.MAX_UPGRADE_LEVEL)
                    {
                        _player?.UpgradeFireRate();
                        _skillPoints--;
                        usedSkillPoint = true;
                    }
                    break;
                case Key.D6:
                    if(_player.ReloadSpeedUpgradeLevel < Player.MAX_UPGRADE_LEVEL)
                    {
                        _player?.UpgradeReloadSpeed();
                        _skillPoints--;
                        usedSkillPoint = true;
                    }
                    break;
                case Key.D7:
                    if(_player.MovementSpeedUpgradeLevel < Player.MAX_UPGRADE_LEVEL)
                    {
                        _player?.UpgradeMovementSpeed();
                        _skillPoints--;
                        usedSkillPoint = true;
                    }
                    break;
            }
            
            // Обновляем UI после любых изменений характеристик
            if (usedSkillPoint)
            {
                UpdateStatsUI();
                UpdatePlayerInfo();
            }
            
            // Если очки навыков закончились, запускаем таймер автоматического закрытия
            if (usedSkillPoint && _skillPoints == 0)
            {
                _statsNotificationTimer!.Stop();
                _statsNotificationTimer.Interval = TimeSpan.FromSeconds(1.5);
                _statsNotificationTimer.Start();
            }
        }
    }
    
    // Обработка отпускания клавиш
    private void Window_KeyUp(object sender, KeyEventArgs e)
            {
        _inputHandler?.HandleKeyUp(e);
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
            });
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Шаг 3: Инициализация менеджера игры
            Dispatcher.Invoke(() => {
                LoadingStatusText.Text = "Инициализация игрового мира...";
                UpdateLoadingProgress(20);
                
                _gameManager = new GameManager(GameCanvas, _player, GameCanvas.ActualWidth, GameCanvas.ActualHeight, _spriteManager);
                _gameManager.ScoreChanged += GameManager_ScoreChanged;
                _gameManager.WeaponChanged += GameManager_WeaponChanged;
                _gameManager.EnemyKilled += GameManager_EnemyKilled;
                _gameManager.TreasureFound += GameManager_TreasureFound;
                _gameManager.SkillPointsAdded += GameManager_SkillPointsAdded;
                _gameManager.ExperienceAdded += GameManager_ExperienceAdded;
                _gameManager.BoostActivated += GameManager_BoostActivated;
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
        _animationDotCount = (_animationDotCount + 1) % 4;
    }

    private void ShowStatsNotification()
    {
        if (_statsNotificationTimer!.IsEnabled)
        {
            _statsNotificationTimer.Stop();
        }

        // Обновляем UI характеристик
        UpdateStatsUI();

        // Показываем окно с анимацией
        StatsNotification.Opacity = 0;
        StatsNotification.Visibility = Visibility.Visible;

        DoubleAnimation fadeInAnimation = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromSeconds(0.5)
        };
        
        StatsNotification.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
        
        // Запускаем таймер автоматического скрытия только если нет доступных очков навыков
        if (_skillPoints == 0)
        {
            _statsNotificationTimer.Interval = TimeSpan.FromSeconds(1.5); // Окно исчезнет через 1.5 секунды
            _statsNotificationTimer.Start();
        }
    }
    
    private void StatsNotificationTimer_Tick(object? sender, EventArgs e)
    {
        _statsNotificationTimer!.Stop();
        HideStatsNotification();
    }

    private void HideStatsNotification()
    {
        DoubleAnimation fadeOutAnimation = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromSeconds(0.5)
        };

        fadeOutAnimation.Completed += (s, e) => StatsNotification.Visibility = Visibility.Collapsed;
        
        StatsNotification.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
    }

    // Метод для обновления UI характеристик
    private void UpdateStatsUI()
    {
        if (_player == null) return;
        
        SkillPointsText.Text = _skillPoints.ToString();

        // Health Regen
        HealthRegenFill.Width = (double)_player.HealthRegenUpgradeLevel / Player.MAX_UPGRADE_LEVEL * (HealthRegenBar.ActualWidth);
        
        // Max Health
        MaxHealthFill.Width = (double)_player.MaxHealthUpgradeLevel / Player.MAX_UPGRADE_LEVEL * (MaxHealthBar.ActualWidth);

        // Bullet Speed
        BulletSpeedFill.Width = (double)_player.BulletSpeedUpgradeLevel / Player.MAX_UPGRADE_LEVEL * (BulletSpeedBar.ActualWidth);
        
        // Bullet Damage
        BulletDamageFill.Width = (double)_player.BulletDamageUpgradeLevel / Player.MAX_UPGRADE_LEVEL * (BulletDamageBar.ActualWidth);
        
        // Reload
        ReloadFill.Width = (double)_player.ReloadSpeedUpgradeLevel / Player.MAX_UPGRADE_LEVEL * (ReloadBar.ActualWidth);

        // Movement Speed
        MovementSpeedFill.Width = (double)_player.MovementSpeedUpgradeLevel / Player.MAX_UPGRADE_LEVEL * (MovementSpeedBar.ActualWidth);

        // Fire Rate
        FireRateFill.Width = (double)_player.FireRateUpgradeLevel / Player.MAX_UPGRADE_LEVEL * (FireRateBar.ActualWidth);

        // Обновляем доступность кнопок
        bool canUpgrade = _skillPoints > 0;
        HealthRegenUpgradeButton.IsEnabled = canUpgrade;
        MaxHealthUpgradeButton.IsEnabled = canUpgrade;
        BulletSpeedUpgradeButton.IsEnabled = canUpgrade;
        BulletDamageUpgradeButton.IsEnabled = canUpgrade;
        FireRateUpgradeButton.IsEnabled = canUpgrade;
        ReloadUpgradeButton.IsEnabled = canUpgrade;
        MovementSpeedUpgradeButton.IsEnabled = canUpgrade;
    }
    
    // Обработчики кнопок улучшения характеристик
    private void HealthRegenUpgrade_Click(object sender, RoutedEventArgs e)
    {
        if (_player != null && _skillPoints > 0 && _player.HealthRegenUpgradeLevel < Player.MAX_UPGRADE_LEVEL)
        {
            _player.UpgradeHealthRegen();
            _skillPoints--;
            UpdateStatsUI();
            UpdatePlayerInfo();
            
            // Если очки навыков закончились, запускаем таймер автоматического закрытия
            if (_skillPoints == 0)
            {
                _statsNotificationTimer!.Stop();
                _statsNotificationTimer.Interval = TimeSpan.FromSeconds(1.5);
                _statsNotificationTimer.Start();
            }
        }
    }
    
    private void MaxHealthUpgrade_Click(object sender, RoutedEventArgs e)
    {
        if (_skillPoints > 0 && _player != null)
        {
            _player.UpgradeMaxHealth();
            _skillPoints--;
            UpdateStatsUI();
            UpdatePlayerInfo();
            
            // Если очки навыков закончились, запускаем таймер автоматического закрытия
            if (_skillPoints == 0)
            {
                _statsNotificationTimer!.Stop();
                _statsNotificationTimer.Interval = TimeSpan.FromSeconds(1.5);
                _statsNotificationTimer.Start();
            }
        }
    }
    
    private void BulletSpeedUpgrade_Click(object sender, RoutedEventArgs e)
    {
        if (_player != null && _skillPoints > 0 && _player.BulletSpeedUpgradeLevel < Player.MAX_UPGRADE_LEVEL)
        {
            _player.UpgradeBulletSpeed();
            _skillPoints--;
            UpdateStatsUI();
            UpdatePlayerInfo();
            if (_player.MaxHealthUpgradeLevel >= Player.MAX_UPGRADE_LEVEL)
            {
                (sender as Button)!.IsEnabled = false;
                (sender as Button)!.Content = "✓";
            }
            
            // Если очки навыков закончились, запускаем таймер автоматического закрытия
            if (_skillPoints == 0)
            {
                _statsNotificationTimer!.Stop();
                _statsNotificationTimer.Interval = TimeSpan.FromSeconds(1.5);
                _statsNotificationTimer.Start();
            }
        }
    }
    
    private void BulletDamageUpgrade_Click(object sender, RoutedEventArgs e)
    {
        if (_player != null && _skillPoints > 0)
        {
            _skillPoints--;
            _player.UpgradeBulletDamage();
            UpdateStatsUI();
            UpdatePlayerInfo();
        }
    }
    
    private void FireRateUpgrade_Click(object sender, RoutedEventArgs e)
    {
        if (_player != null && _skillPoints > 0)
        {
            _skillPoints--;
            _player.UpgradeFireRate();
            UpdateStatsUI();
            UpdatePlayerInfo();
        }
    }
    
    private void ReloadUpgrade_Click(object sender, RoutedEventArgs e)
    {
        if (_player != null && _skillPoints > 0)
        {
            _player.UpgradeReloadSpeed();
            _skillPoints--;
            UpdateStatsUI();
            UpdatePlayerInfo();
            
            // Если очки навыков закончились, запускаем таймер автоматического закрытия
            if (_skillPoints == 0)
            {
                _statsNotificationTimer!.Stop();
                _statsNotificationTimer.Interval = TimeSpan.FromSeconds(1.5);
                _statsNotificationTimer.Start();
            }
        }
    }
    
    private void MovementSpeedUpgrade_Click(object sender, RoutedEventArgs e)
    {
        if (_player != null && _skillPoints > 0 && _player.MovementSpeedUpgradeLevel < Player.MAX_UPGRADE_LEVEL)
        {
            _player.UpgradeMovementSpeed();
            _skillPoints--;
            UpdateStatsUI();
            UpdatePlayerInfo();
            if (_player.MaxHealthUpgradeLevel >= Player.MAX_UPGRADE_LEVEL)
            {
                (sender as Button)!.IsEnabled = false;
                (sender as Button)!.Content = "✓";
            }
            
            // Если очки навыков закончились, запускаем таймер автоматического закрытия
            if (_skillPoints == 0)
            {
                _statsNotificationTimer!.Stop();
                _statsNotificationTimer.Interval = TimeSpan.FromSeconds(1.5);
                _statsNotificationTimer.Start();
            }
        }
    }

    /// <summary>
    /// Проверяет состояние игрока и показывает окно смерти, если игрок мертв
    /// </summary>
    private void DeathCheckTimer_Tick(object sender, EventArgs e)
    {
        if (_player != null && _player.Health <= 0 && !_playerIsDead)
        {
            ShowDeathNotification();
        }
    }
    
    /// <summary>
    /// Показывает окно смерти игрока
    /// </summary>
    private void ShowDeathNotification()
    {
        // Устанавливаем флаг смерти игрока
        _playerIsDead = true;
        
        // Останавливаем игровой цикл
        _gameLoop?.Stop();
        
        // Обновляем статистику в окне смерти
        DeathScoreText.Text = _score.ToString();
        DeathLevelText.Text = _playerLevel.ToString();
        DeathWeaponText.Text = _player?.GetWeaponName() ?? "Пистолет";
        
        // Показываем окно с анимацией
        DeathNotification.Opacity = 0;
        DeathNotification.Visibility = Visibility.Visible;
        
        DoubleAnimation fadeInAnimation = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromSeconds(0.5)
        };
        
        DeathNotification.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
    }
    
    /// <summary>
    /// Скрывает окно смерти игрока
    /// </summary>
    private void HideDeathNotification()
    {
        DeathNotification.Visibility = Visibility.Collapsed;
        
        // Останавливаем таймер, если он был запущен
        _deathCheckTimer?.Stop();
    }
    
    /// <summary>
    /// Возрождает игрока с начальными характеристиками
    /// </summary>
    private void RespawnPlayer()
    {
        // 1. Stop and clean up the old game instance
        _gameLoop?.Stop();
        _gameManager?.Dispose(); // Dispose game manager resources
        _gameLoop = null;
        _gameManager = null;
        _player = null;

        // 2. Clear the canvas
        GameCanvas.Children.Clear();

        // 3. Reset progression stats
        _score = 0;
        _playerLevel = 1;
        _playerExperience = 0;
        _experienceToNextLevel = 100;
        _skillPoints = 0;
        _classSelectionShown = false;

        // 4. Re-initialize the game to create a fresh session
        InitializeGame();
        
        // 5. Reset stats and hide death screen
        _playerIsDead = false;
        HideDeathNotification();
        
        // 6. Restart the death check timer
        _deathCheckTimer?.Start();
        
        // 7. Update UI to reflect the reset
        UpdatePlayerInfo();
    }
    
    /// <summary>
    /// Обработчик нажатия кнопки возрождения
    /// </summary>
    private void RespawnButton_Click(object sender, RoutedEventArgs e)
    {
        HideDeathNotification();
        RespawnPlayer();
    }

    // Новый метод для отображения уведомления об очках навыков
    private void ShowSkillPointsNotification(int skillPoints)
    {
        string message = $"Получено {skillPoints} {GetSkillPointsText(skillPoints)}";
        _notificationQueue.Enqueue(message);
        ProcessNotificationQueue();
    }
    
    // Метод для отображения уведомления о получении опыта
    private void ShowExperienceNotification(int experience)
    {
        string message = $"Получено {experience} единиц опыта!";
        _notificationQueue.Enqueue(message);
        ProcessNotificationQueue();
    }

    private void ClassSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_player == null || _player.ChosenClass != null) return;

        var button = sender as Button;
        if (button?.Tag is PlayerClassType classType)
        {
            PlayerClass chosenClass = classType switch
            {
                PlayerClassType.Assault => new Assault(),
                PlayerClassType.Sniper => new Sniper(),
                PlayerClassType.Heavy => new Heavy(),
                PlayerClassType.Engineer => new Engineer(),
                _ => null
            };

            if (chosenClass != null)
            {
                _player.SetPlayerClass(chosenClass);
                HideClassSelectionScreen();
                _gameManager?.ResumeGame();
            }
        }
    }

    private void ShowClassSelectionScreen()
    {
        _gameManager?.PauseGame(GameState.ClassSelection);
        ClassSelectionGrid.Visibility = Visibility.Visible;
    }

    private void HideClassSelectionScreen()
    {
        ClassSelectionGrid.Visibility = Visibility.Collapsed;
        _gameManager?.ResumeGame();
    }

    private void TogglePause()
    {
        if (_gameManager == null || _playerIsDead) return;

        if (_gameManager.CurrentState == GameState.Playing)
        {
            _gameManager.PauseGame();
            PauseMenuGrid.Visibility = Visibility.Visible;
        }
        else if (_gameManager.CurrentState == GameState.Paused)
        {
            _gameManager.ResumeGame();
            PauseMenuGrid.Visibility = Visibility.Collapsed;
        }
    }

    private void ResumeButton_Click(object sender, RoutedEventArgs e)
    {
        TogglePause();
    }

    private void SuicideButton_Click(object sender, RoutedEventArgs e)
    {
        if (_player != null)
        {
            _player.Kill();
        }
        TogglePause();
    }
}