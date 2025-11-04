//./GameShared/Builders/HunterBuilder.cs
using GameServer.Combat;
using GameShared.Strategies;
using GameShared.Types.Players;
using System.Drawing;


namespace GameServer.Builders
{
    public class HunterBuilder : IPlayerBuilder
    {
        private Hunter _hunter;

        public IPlayerBuilder Start()
        {
            _hunter = new Hunter();
            return this;
        }

        public IPlayerBuilder SetId(int id)
        {
            _hunter.Id = id;
            return this;
        }

        public IPlayerBuilder SetPosition(int x, int y)
        {
            _hunter.X = x;
            _hunter.Y = y;
            return this;
        }
        public IPlayerBuilder SetRoleType()
        {
            _hunter.RoleType = "Hunter";
            return this;
        }

        public IPlayerBuilder SetHealth()
        {
            _hunter.Health = 5;
            return this;
        }

        public IPlayerBuilder SetColor()
        {
            _hunter.RoleColor = Color.Brown;
            return this;
        }

        public IPlayerBuilder SetAttackType()
        {
            _hunter.AttackStrategy = new HunterAttackStrategy();
            return this;
        }

        public IPlayerBuilder SetMovementStrategy(IMovementStrategy strategy) 
        { 
            _hunter.SetMovementStrategy(strategy); 
            return this; 
        }

        public PlayerRole Build()
        {
            return _hunter;
        }
    }
}
