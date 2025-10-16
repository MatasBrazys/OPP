using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameShared.Messages
{
    public abstract class GameMessage
    {
        public virtual string Type { get; set; } = "";
        public int V { get; set; } = 1;
    }

}
