using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using GunVault.Models;
using GunVault.GameEngine;

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
    
    public MainWindow()
    {
        InitializeComponent();
        
        // Инициализируем игру после загрузки окна
        Loaded += MainWindow_Loaded;
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
            
            InitializeGame();
            
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
            // Инициализируем игрока в центре экрана
            double centerX = GameCanvas.ActualWidth / 2;
            double centerY = GameCanvas.ActualHeight / 2;
            _player = new Player(centerX, centerY, _spriteManager);
            
            // Инициализируем обработчик ввода
            _inputHandler = new InputHandler(_player);
            
            // Добавляем игрока на канвас
            GameCanvas.Children.Add(_player.PlayerShape);
            
            // Инициализируем менеджер игры
            _gameManager = new GameManager(GameCanvas, _player, GameCanvas.ActualWidth, GameCanvas.ActualHeight);
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
            Console.WriteLine($"Ошибка при инициализации игры: {ex.Message}\n{ex.StackTrace}");
            MessageBox.Show($"Ошибка при инициализации игры: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
    }
    
    // Обработка изменения размера окна
    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
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
    }
    
    // Обработка отпускания клавиш
    private void Window_KeyUp(object sender, KeyEventArgs e)
    {
        if (_inputHandler != null)
        {
            _inputHandler.HandleKeyUp(e);
        }
    }
}