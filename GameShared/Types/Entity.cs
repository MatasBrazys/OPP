//./GameShared/Types/Entity.cs
namespace GameShared.Types
{
    public abstract class Entity
    {
        public int Id { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public abstract string EntityType { get; }
        public virtual void Update() { }
    }
}