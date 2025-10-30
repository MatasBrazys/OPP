using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.Commands;
using GameShared.Commands;

namespace GameServer.Events
{
    public class PlayerCommandEvent : GameEvent
    {
        public ICommand Command { get; set; }
        public int PlayerId { get; set; }

        public PlayerCommandEvent(ICommand command, int playerId)
            : base("player_command") 
        {
            Command = command;
            PlayerId = playerId;
        }
    }
}
