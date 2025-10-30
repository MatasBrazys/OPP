using GameShared.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GameShared.Messages
{
    /// <summary>
    /// Sent message from Client to Server
    /// </summary>
    public class CommandMessage : GameMessage
    {
        public override string Type => "command";

        [JsonConverter(typeof(CommandConverter))]
        public ICommand Command { get; set; }
    }
}
