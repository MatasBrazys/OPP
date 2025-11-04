//./GameShared/Builders/IPlayerBuilder.cs
using GameShared.Strategies;
using GameShared.Types.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Builders
{
    public interface IPlayerBuilder
    {
        IPlayerBuilder Start();
        IPlayerBuilder SetId(int id);
        IPlayerBuilder SetPosition(int x, int y);
        IPlayerBuilder SetRoleType();
        IPlayerBuilder SetHealth();
        IPlayerBuilder SetColor();
        IPlayerBuilder SetAttackType();
        IPlayerBuilder SetMovementStrategy(IMovementStrategy strategy);
        PlayerRole Build();
    }
}
