using System.Windows.Input;
using GunVault.Models;

namespace GunVault.GameEngine
{
    public class InputHandler
    {
        private Player _player;
        
        public InputHandler(Player player)
        {
            _player = player;
        }
        
        public void HandleKeyDown(KeyEventArgs e)
        {
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
        }
        
        public void HandleKeyUp(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.W:
                    _player.MovingUp = false;
                    break;
                case Key.S:
                    _player.MovingDown = false;
                    break;
                case Key.A:
                    _player.MovingLeft = false;
                    break;
                case Key.D:
                    _player.MovingRight = false;
                    break;
            }
        }
    }
} 