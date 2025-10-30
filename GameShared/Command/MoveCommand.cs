using GameShared.Types.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameShared.Commands
{
    public class MoveCommand : ICommand
    {
        public string Type => "move";
        public int Dx { get; set; }
        public int Dy { get; set; }

        private int _originalX;
        private int _originalY;

        public void Execute(World world, int playerId)
        {
            var player = world.GetPlayer(playerId);
            if (player != null)
            {
                _originalX = player.X;
                _originalY = player.Y;

                player.X += Dx * player.GetSpeed();
                player.Y += Dy * player.GetSpeed();

                Console.WriteLine($"Player {playerId} moved to ({player.X}, {player.Y})");
            }
        }

        public void Undo(World world, int playerId)
        {
            var player = world.GetPlayer(playerId);
            if (player != null)
            {
                player.X = _originalX;
                player.Y = _originalY;
                Console.WriteLine($"Player {playerId} movement undone to ({player.X}, {player.Y})");
            }
        }
    }
}