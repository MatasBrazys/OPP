using GameShared.Types.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameShared.Commands
{
    public interface ICommand
    {
        void Execute(World world, int playerId);
        void Undo(World world, int playerId);
        string Type { get; }
    }
}
