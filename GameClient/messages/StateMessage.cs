using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameClient.types;

namespace GameClient.messages
{
    public class StateMessage : GameMessage
    {
        public long ServerTime { get; set; }
        public List<PlayerState> Players { get; set; } = new();
        public StateMessage() { Type = "state"; }
    }
}
