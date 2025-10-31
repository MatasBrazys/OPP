//./GameShared/Factories/IEnemyFactory.cs
using GameShared.Types.Enemies;

namespace GameShared.Factories
{
    public interface IEnemyFactory
    {
        Enemy CreateEnemy(string type, int id, int x, int y);
    }
}