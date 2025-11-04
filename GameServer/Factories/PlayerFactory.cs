//./GameShared/Factories/PlayerFactory.cs
using GameServer.Builders;
using GameShared.Types.Players;

namespace GameServer.Factories
{
    public class PlayerFactory : IPlayerFactory
    {
        // ABSTRACT FACTORY (decoupled creation of players and made on the interface, product family concept exists)
        public PlayerRole CreatePlayer(string roleType, int id, int x, int y)
        {
            IPlayerBuilder builder = roleType.ToLower() switch
            {
                "hunter" => new HunterBuilder().Start(),
                "mage" => new MageBuilder().Start(),
                "defender" => new DefenderBuilder().Start(),
                _ => throw new ArgumentException("Invalid player role type")
            };

            // Configure common properties via builder
            builder.SetId(id)
                   .SetPosition(x, y)
                   .SetRoleType()
                   .SetHealth()
                   .SetColor()
                   .SetAttackType();

            // Build the final PlayerRole instance
            return builder.Build();
        }
    }
}