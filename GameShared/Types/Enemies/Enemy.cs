//./GameShared/Types/Enemies/Enemy.cs

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
            base.Update();
        }

        #region Composite leaf override

        public override void Add(Entity child)
            => throw new NotSupportedException("Enemy is a leaf and cannot have children");

        public override void Remove(Entity child)
            => throw new NotSupportedException("Enemy is a leaf and cannot have children");

        public override IReadOnlyList<Entity> GetChildren()
            => throw new NotSupportedException("Enemy is a leaf and does not have children");

        #endregion Composite leaf override
    }
}
