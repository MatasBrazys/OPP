//./GameShared/Factories/IEnemyFactory.cs
using GameShared.Types.Enemies;

namespace GameServer.Factories
{
    public interface IEnemyFactory
    {
        Enemy CreateEnemy(string type, int id, int x, int y);
    }
}