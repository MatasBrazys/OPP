//./GameShared/Factories/IPlayerFactory.cs
using GameShared.Types.Players;

namespace GameServer.Factories
{
    // ABSTRACT FACTORY interface
    public interface IPlayerFactory
    {
        PlayerRole CreatePlayer(string roleType, int id, int x, int y);
    }
}