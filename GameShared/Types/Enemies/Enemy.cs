//./GameShared/Types/Enemies/Enemy.cs
using GameServer.Strategies;
using GameShared.Strategies;


namespace GameShared.Types.Enemies
{
    public abstract class Enemy : Entity
    {
        public int Health { get; set; } = 100;
        public int MaxHealth { get; set; } = 100;
        public string EnemyType { get; protected set; } = "Enemy";
        public override string EntityType => EnemyType;
        public LeftRightRoam? RoamingAI { get; set; }
        


        // Simple placeholder update; server will call Update() every tick via World.Update()
        public override void Update()
        {
            RoamingAI?.Update(this);
        }
    }
}
