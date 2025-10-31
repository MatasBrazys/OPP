//./GameShared/Types/GameObjects/GameObject.cs
namespace GameShared.Types.GameObjects
{
    public abstract class GameObject : Entity
    {
        public string Type { get; protected set; }
        public override string EntityType => Type;
        public bool IsDestructible { get; protected set; }
        public abstract void Interact();
    }
}