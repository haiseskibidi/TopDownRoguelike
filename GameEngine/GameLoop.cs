using System;
using System.Windows;
using System.Windows.Threading;
using GunVault.Models;

namespace GunVault.GameEngine
{
    public class GameLoop
    {
        private DispatcherTimer _gameTimer;
        private GameManager _gameManager;
        private double _gameWidth;
        private double _gameHeight;
        private DateTime _lastTime;
        public event EventHandler GameTick;
        
        public GameLoop(GameManager gameManager, double gameWidth, double gameHeight)
        {
            _gameManager = gameManager;
            _gameWidth = gameWidth;
            _gameHeight = gameHeight;
            _gameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _gameTimer.Tick += GameTimerTick;
            _lastTime = DateTime.Now;
        }
        
        public void Start()
        {
            _lastTime = DateTime.Now;
            _gameTimer.Start();
        }
        
        public void Stop()
        {
            _gameTimer.Stop();
        }
        
        private void GameTimerTick(object sender, EventArgs e)
        {
            DateTime currentTime = DateTime.Now;
            double deltaTime = (currentTime - _lastTime).TotalSeconds;
            _lastTime = currentTime;
            deltaTime = Math.Min(deltaTime, 0.1);
            _gameManager.Update(deltaTime);
            GameTick?.Invoke(this, EventArgs.Empty);
        }
        
        public void ResizeGameArea(double width, double height)
        {
            _gameWidth = width;
            _gameHeight = height;
            _gameManager.ResizeGameArea(width, height);
        }
    }
} 