using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameClient.messages
{
    public class GoodbyeMessage : GameMessage
    {
        public string Reason { get; set; } = "";
        public GoodbyeMessage() { Type = "goodbye"; }
    }
}
