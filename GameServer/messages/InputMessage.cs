using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.messages
{
    public class InputMessage : GameMessage
    {
        public int Dx { get; set; }
        public int Dy { get; set; }
        public InputMessage() { Type = "input"; }
    }
}
