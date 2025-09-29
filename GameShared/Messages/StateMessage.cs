using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameShared.Types;
using GameShared.Types.DTOs;
using GameShared.Types.Players;

namespace GameShared.Messages
{
    public class StateMessage : GameMessage
    {
        public long ServerTime { get; set; }
        public List<PlayerDto> Players { get; set; } = new();
        public StateMessage() { Type = "state"; }
    }
}
