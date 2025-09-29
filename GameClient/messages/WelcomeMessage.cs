using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameClient.messages
{
    public class WelcomeMessage : GameMessage
    {
        public int Id { get; set; }
        public WelcomeMessage() { Type = "welcome"; }
    }
}
