using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameClient.messages
{
    public abstract class GameMessage
    {
        public string Type { get; set; } = "";
        public int V { get; set; } = 1;
    }

}
