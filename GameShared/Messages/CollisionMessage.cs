//./GameShared/Messages/CollisionMessage.cs

namespace GameShared.Messages
{
    public class CollisionMessage
    {
        public string Type { get; set; } = "collision";
        public int Entity1Id { get; set; }
        public int Entity2Id { get; set; }
        public string Entity1Type { get; set; }
        public string Entity2Type { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
    }
}
