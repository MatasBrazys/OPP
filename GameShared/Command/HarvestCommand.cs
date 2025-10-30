using GameShared.Types.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameShared.Commands
{
    internal class HarvestCommand : ICommand
    {
        public string Type => "harvest";
        public int X { get; set; }
        public int Y { get; set; }

        private bool _wasHarvested;
        public void Execute(World world, int playerId)
        {
            var tile = world.Map.GetTile(X, Y);
            if (tile is TreeTile tree)
            {
                _wasHarvested = tree.TryHarvestWithUndo();
                if (_wasHarvested)
                {
                    Console.WriteLine($"Player {playerId} harvested tree at ({X}, {Y})");
                }
            }
        }
        public void Undo(World world, int playerId)
        {
            if (_wasHarvested)
            {
                var tile = world.Map.GetTile(X, Y);
                if (tile is TreeTile tree)
                {
                    tree.RestoreState();
                    Console.WriteLine($"Player {playerId} harvest undone at ({X}, {Y})");
                }
            }
        }
    }
}
