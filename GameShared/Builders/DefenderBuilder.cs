//./GameShared/Builders/DefenderBuilder.cs
using GameShared.Strategies;
using GameShared.Types.Players;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameShared.Builders
{
    public class DefenderBuilder : IPlayerBuilder
    {
        private Defender _defender;


        public IPlayerBuilder Start()
        {
            _defender = new Defender();
            return this;
        }

        public IPlayerBuilder SetId(int id)
        {
            _defender.Id = id;
            return this;
        }

        public IPlayerBuilder SetPosition(int x, int y)
        {
            _defender.X = x;
            _defender.Y = y;
            return this;
        }

        public IPlayerBuilder SetRoleType()
        {
            _defender.RoleType = "Defender";
            return this;
        }

        public IPlayerBuilder SetHealth()
        {
            _defender.Health = 6;
            return this;
        }

        public IPlayerBuilder SetColor()
        {
            _defender.RoleColor = Color.Green;
            return this;
        }

        public IPlayerBuilder SetAttackType()
        {
            _defender.AttackType = "Close combat";
            return this;
        }
        public IPlayerBuilder SetMovementStrategy(IMovementStrategy strategy)
        {
            _defender.SetMovementStrategy(strategy);
            return this;
        }

        public PlayerRole Build()
        {
            return _defender;
        }
    }

}
