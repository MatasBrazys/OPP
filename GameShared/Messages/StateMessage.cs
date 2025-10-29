
using GameShared.Types.DTOs;

namespace GameShared.Messages
{
    public class StateMessage : GameMessage
    {
        public long ServerTime { get; set; }
        public List<PlayerDto> Players { get; set; } = new();
        public List<EnemyDto> Enemies { get; set; } = new(); // NEW
        public StateMessage() { Type = "state"; }
    }
}

