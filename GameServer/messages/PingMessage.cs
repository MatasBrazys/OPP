using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.messages
{
    public class PingMessage : GameMessage
    {
        public long T { get; set; }
        public PingMessage() { Type = "ping"; }
    }
}
