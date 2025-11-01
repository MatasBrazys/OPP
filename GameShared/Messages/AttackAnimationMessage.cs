namespace GameShared.Messages
{
    public class AttackAnimationMessage
    {
        public string Type { get; set; } = "attack_animation";
        public int PlayerId { get; set; }
        public float AnimX { get; set; }    // pixel X
        public float AnimY { get; set; }    // pixel Y
        public string Direction { get; set; } = "0.0";
    }
}
