using GameShared.Strategies;
using GameShared.Types.Enemies;

namespace GameShared.Factories
{
    public class EnemyFactory : IEnemyFactory
    {
        public Enemy CreateEnemy(string type, int id, int x, int y)
        {
            Enemy enemy = type.ToLower() switch
            {
                "slime" => new Slime(),
                _ => throw new ArgumentException($"Unknown enemy type: {type}")
            };

            enemy.Id = id;
            enemy.X = x;
            enemy.Y = y;

            return enemy;
        }
    }
}
