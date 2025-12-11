using GameShared.Types;
using System;
using System.Collections.Generic;

namespace GameServer.Iterators
{
    public class CompositeIterator : Iterator
    {
        private readonly Queue<Entity> _queue;
        private Entity? _current;
        private readonly Entity _root;
        private readonly TraversalMode _mode;

        public enum TraversalMode
        {
            BreadthFirst,
            DepthFirst
        }
        public CompositeIterator(Entity root, TraversalMode mode = TraversalMode.DepthFirst)
        {
            _root = root;
            _mode = mode;
            _queue = new Queue<Entity>();
            Initialize();
        }

        private void Initialize()
        {
            _queue.Clear();
            _queue.Enqueue(_root);
        }

        public override int Key()
        {
            return _current?.Id ?? -1;
        }

        public override object Current()
        {
            if (_current == null)
                throw new InvalidOperationException("Iterator is not positioned on a valid element");
            return _current;
        }

        public override bool MoveNext()
        {
            if (_queue.Count == 0)
            {
                _current = null;
                return false;
            }

            _current = _queue.Dequeue();

            var children = _current.GetChildren();
            
            if (_mode == TraversalMode.DepthFirst)
            {
                var childrenList = new List<Entity>(children);
                for (int i = childrenList.Count - 1; i >= 0; i--)
                {
                    _queue.Enqueue(childrenList[i]);
                }
            }
            else
            {
                foreach (var child in children)
                {
                    _queue.Enqueue(child);
                }
            }

            return true;
        }

        public override void Reset()
        {
            Initialize();
        }

        /// <summary>
        /// Get all entities in the composite structure as a list
        /// </summary>
        public List<Entity> GetAllEntities()
        {
            var entities = new List<Entity>();
            Reset();
            while (MoveNext())
            {
                entities.Add((Entity)Current());
            }
            Reset();
            return entities;
        }

        /// <summary>
        /// Get all entities of a specific type
        /// </summary>
        public List<T> GetEntitiesOfType<T>() where T : Entity
        {
            var entities = new List<T>();
            Reset();
            while (MoveNext())
            {
                if (Current() is T entity)
                {
                    entities.Add(entity);
                }
            }
            Reset();
            return entities;
        }

        /// <summary>
        /// Find first entity matching a predicate
        /// </summary>
        public Entity? FindFirst(Func<Entity, bool> predicate)
        {
            Reset();
            while (MoveNext())
            {
                var entity = (Entity)Current();
                if (predicate(entity))
                {
                    return entity;
                }
            }
            Reset();
            return null;
        }

        /// <summary>
        /// Find all entities matching a predicate
        /// </summary>
        public List<Entity> FindAll(Func<Entity, bool> predicate)
        {
            var entities = new List<Entity>();
            Reset();
            while (MoveNext())
            {
                var entity = (Entity)Current();
                if (predicate(entity))
                {
                    entities.Add(entity);
                }
            }
            Reset();
            return entities;
        }

        /// <summary>
        /// Traverse upward from current position to root
        /// </summary>
        public IEnumerable<Entity> TraverseUpToRoot()
        {
            if (_current == null)
                throw new InvalidOperationException("Iterator is not positioned on a valid element");

            return _current.GetPathToRoot();
        }
    }
}
