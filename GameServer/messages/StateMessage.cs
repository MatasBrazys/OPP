using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.types;

namespace GameServer.messages
{
    public class StateMessage : GameMessage
    {
        public long ServerTime { get; set; }
        public List<PlayerState> Players { get; set; } = new();
        public StateMessage() { Type = "state"; }
    }
}
