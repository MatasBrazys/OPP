namespace GameShared.Messages
{
    public class AttackAnimationMessage
    {
        public string Type { get; set; } = "attack_animation";

        // Player who created it
        public int PlayerId { get; set; }

        // Pixel position where animation should appear (server computes)
        public float AnimX { get; set; }
        public float AnimY { get; set; }

        // Direction in degrees (0 = right, clockwise positive)
        public string Direction { get; set; } = "0.0";

        // Optional: animation kind / size if you want
        public float Radius { get; set; } = GameShared.GameConstants.TILE_SIZE;
    }
}
