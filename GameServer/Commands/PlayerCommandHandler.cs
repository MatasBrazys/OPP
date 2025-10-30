using GameServer.Events;
using GameShared.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Commands
{
    internal class PlayerCommandHandler : CommandHandler
    {
        private readonly Server _server;
        private readonly Game _game;

        public PlayerCommandHandler(Server server, Game game)
        {
            _server = server;
            _game = game;
        }
        public override string[] HandledEventTypes => new[] { "player_command" };

        public override void OnGameEvent(GameEvent gameEvent)
        {
            if (gameEvent is PlayerCommandEvent commandEvent)
            {
                HandlePlayerCommand(commandEvent);
            }
        }
        private void HandlePlayerCommand(PlayerCommandEvent commandEvent)
        {
            Console.WriteLine($"Processing {commandEvent.Command.Type} command for player {commandEvent.PlayerId}");

            _game.Invoker.ExecuteCommand(commandEvent.Command, commandEvent.PlayerId);

            var resultMessage = new CommandResultMessage
            {
                PlayerId = commandEvent.PlayerId,
                CommandType = commandEvent.Command.Type,
                Success = true
            };

            _server.BroadcastToAll(resultMessage);
        }

    }
}
