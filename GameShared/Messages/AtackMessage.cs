namespace GameShared.Messages
{
    public class AttackMessage
    {
        public string Type { get; set; } = "attack";
        public int PlayerId { get; set; }

        // Pixel coordinates of the click (client -> server)
        public float ClickX { get; set; }
        public float ClickY { get; set; }

        // (Optional) attack subtype (slash, arrow, etc.)
        public string AttackType { get; set; } = "slash";
    }
}
