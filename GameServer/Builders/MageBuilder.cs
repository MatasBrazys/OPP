//./GameShared/Builders/MageBuilder.cs
using GameShared.Strategies;
using GameShared.Types.Players;
using System.Drawing;
using GameServer.Combat;

namespace GameServer.Builders
{
    public class MageBuilder : IPlayerBuilder
    {
        private Mage _mage;

        public IPlayerBuilder Start()
        {
            _mage = new Mage();
            return this;
        }

        public IPlayerBuilder SetId(int id)
        {
            _mage.Id = id;
            return this;
        }

        public IPlayerBuilder SetPosition(int x, int y)
        {
            _mage.X = x;
            _mage.Y = y;
            return this;
        }

        public IPlayerBuilder SetRoleType()
        {
            _mage.RoleType = "Mage";
            return this;
        }

        public IPlayerBuilder SetHealth()
        {
            _mage.Health = 4;
            return this;
        }

        public IPlayerBuilder SetColor()
        {
            _mage.RoleColor = Color.Blue;
            return this;
        }

        public IPlayerBuilder SetAttackType()
        {
            _mage.AttackStrategy = new MageAttackStrategy();
            return this;
        }
        public IPlayerBuilder SetMovementStrategy(IMovementStrategy strategy)
        {
            _mage.SetMovementStrategy(strategy);
            return this;
        }

        public PlayerRole Build()
        {
            return _mage;
        }
    }

}
