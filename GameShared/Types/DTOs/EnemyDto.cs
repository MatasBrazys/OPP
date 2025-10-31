namespace GameShared.Types.DTOs
{
    public class EnemyDto
    {
        public int Id { get; set; }
        public string EnemyType { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
    }
}
