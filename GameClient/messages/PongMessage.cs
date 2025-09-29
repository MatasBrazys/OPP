using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameClient.messages
{
    public class PongMessage : GameMessage
    {
        public long T { get; set; }
        public PongMessage() { Type = "pong"; }
    }
}
