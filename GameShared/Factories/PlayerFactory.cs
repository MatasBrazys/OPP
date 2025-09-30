using GameShared.Types.Players;

namespace GameShared.Factories
{
    public class PlayerFactory : IPlayerFactory
    {
        // ABSTRACT FACTORY (decoupled creation of players and made on the interface, product family concept exists)
        public PlayerRole CreatePlayer(string roleType, int id, int x, int y)
        {
            PlayerRole player = roleType.ToLower() switch
            {
                "hunter" => new Hunter(),
                "mage" => new Mage(),
                "defender" => new Defender(),
                _ => throw new ArgumentException("Invalid player role type")
            };

            player.Id = id;
            player.X = x;
            player.Y = y;

            return player;
        }
    }
}