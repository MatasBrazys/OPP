//./GameShared/Types/Enemies/Slime.cs
using GameShared.Strategies;
using System;

namespace GameShared.Types.Enemies
{
    public class Slime : Enemy
    {
        private static readonly Random _rnd = new();

        public Slime() 
        {
            EnemyType = "Slime";
            Health = 20;
            MaxHealth = 20;
        }

     
    }
}
