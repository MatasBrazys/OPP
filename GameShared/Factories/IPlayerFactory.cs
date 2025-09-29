using GameShared.Types.Players;

namespace GameShared.Factories
{
    // ABSTRACT FACTORY interface
    public interface IPlayerFactory
    {
        PlayerRole CreatePlayer(string roleType, int id, int x, int y);
    }
}