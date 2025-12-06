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
        
        // Parent reference for upward traversal
        public Entity? Parent { get; set; }

        public virtual void Add(Entity child)
        {
            lock (_childrenLock)
            {
                _children.Add(child);
                child.Parent = this; // Set parent when adding child
            }
        }

        public virtual void Remove(Entity child)
        {
            lock (_childrenLock)
            {
                _children.Remove(child);
                child.Parent = null; // Clear parent when removing
            }
        }

        public virtual IReadOnlyList<Entity> GetChildren()
        {
            lock (_childrenLock)
            {
                return _children.AsReadOnly();
            }
        }

        /// <summary>
        /// Get the root entity by traversing up the parent chain
        /// </summary>
        public Entity GetRoot()
        {
            var current = this;
            while (current.Parent != null)
            {
                current = current.Parent;
            }
            return current;
        }

        /// <summary>
        /// Get the path from this entity to the root (including self)
        /// </summary>
        public IEnumerable<Entity> GetPathToRoot()
        {
            var current = this;
            while (current != null)
            {
                yield return current;
                current = current.Parent;
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
