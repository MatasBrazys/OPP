namespace GameShared.Messages
{
    public class AttackMessage : GameMessage
    {
        public int PlayerId { get; set; }
        public string AttackType { get; set; } = string.Empty;
        public int TargetX { get; set; }
        public int TargetY { get; set; }

        public AttackMessage() { Type = "attack"; }
    }
}
