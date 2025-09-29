using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.messages
{
    public class ErrorMessage : GameMessage
    {
        public string Code { get; set; } = "";
        public string Detail { get; set; } = "";
        public ErrorMessage() { Type = "error"; }
    }
}
