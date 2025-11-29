//./GameShared/Types/Entity.cs
namespace GameShared.Types
{
    public abstract class Entity
    {
        public int Id { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public abstract string EntityType { get; }

        // Composite support
        protected readonly List<Entity> _children = new();
        protected readonly object _childrenLock = new();

        public virtual void Add(Entity child)
        {
            lock (_childrenLock)
            {
                _children.Add(child);
            }
        }

        public virtual void Remove(Entity child)
        {
            lock (_childrenLock)
            {
                _children.Remove(child);
            }
        }

        public virtual IReadOnlyList<Entity> GetChildren()
        {
            lock (_childrenLock)
            {
                return _children.AsReadOnly();
            }
        }

        public virtual void Update()
        {
            // Leaf does nothing here, or override in derived
            // Composite will call Update on children
            lock (_childrenLock)
            {
                foreach (var child in _children)
                {
                    child.Update();
                }
            }
        }
    }
}
