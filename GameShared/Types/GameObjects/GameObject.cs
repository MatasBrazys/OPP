namespace GameShared.Types.GameObjects
{
    public abstract class GameObject : Entity
    {
        public string Type { get; protected set; }
        public bool IsDestructible { get; protected set; }
        public abstract void Interact();
    }
}