using GameShared.Types.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameShared.Commands
{
    public class Invoker
    {
        private readonly Stack<ICommand> _commandHistory = new();
        private readonly World _world;

        public Invoker(World w)
        {
            _world = w;
        }

        public void ExecuteCommand(ICommand command, int playerId)
        {
            command.Execute(_world, playerId);
            _commandHistory.Push(command);
        }
        public void UndoLastcommand(int playerId)
        {
            if (_commandHistory.Count > 0)
            {
                var cmd = _commandHistory.Pop();
                cmd.Undo(_world, playerId);
            }
        }
    }
}
