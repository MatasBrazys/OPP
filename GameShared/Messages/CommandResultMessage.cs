using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameShared.Messages
{
    /// <summary>
    /// Sent message from server to client
    /// </summary>
    public class CommandResultMessage : GameMessage
    {
        public override string Type => "command_result";
        public int PlayerId { get; set; }
        public string CommandType { get; set; }
        public bool Success { get; set; }

    }
}
