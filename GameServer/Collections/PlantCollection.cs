using GameShared.Types.GameObjects;
using GameServer.Iterators;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GameServer.Collections
{
    /// <summary>
    /// Collection for managing plants with growth stage updates.
    /// </summary>
    public class PlantCollection : IteratorAggregate, GameShared.Types.GameObjects.IPlantComponent
    {
        private readonly List<GameShared.Types.GameObjects.IPlantComponent> _children = new();
        private static int _nextPlantId = 1;

        // Composite does not have inherent location or type
        public int Id { get; } = -1;
        public string PlantType => "Group";

        /// <summary>
        /// Add a component (Plant leaf or nested Composite) to this collection
        /// </summary>
        public void Add(GameShared.Types.GameObjects.IPlantComponent component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            // Auto-assign ID if it's a Plant leaf without an ID
            if (component is Plant p && p.Id == 0)
                p.Id = _nextPlantId++;

            _children.Add(component);
        }

        /// <summary>
        /// Convenience overload to add Plant directly
        /// </summary>
        public void Add(Plant plant)
        {
            Add((GameShared.Types.GameObjects.IPlantComponent)plant);
        }

        /// <summary>
        /// Remove a component from this collection
        /// </summary>
        public bool Remove(GameShared.Types.GameObjects.IPlantComponent component)
        {
            return _children.Remove(component);
        }

        /// <summary>
        /// Convenience overload to remove Plant directly
        /// </summary>
        public bool Remove(Plant plant)
        {
            return Remove((GameShared.Types.GameObjects.IPlantComponent)plant);
        }

        public int Count => _children.Count;

        /// <summary>
        /// Get component at index (may be Plant or nested Composite)
        /// </summary>
        public GameShared.Types.GameObjects.IPlantComponent GetAt(int index)
        {
            if (index < 0 || index >= _children.Count)
                throw new IndexOutOfRangeException("Index is out of bounds");
            return _children[index];
        }

        public void Clear()
        {
            _children.Clear();
        }

        /// <summary>
        /// Get a Plant leaf by its ID (searches all levels if nested)
        /// </summary>
        public Plant? GetById(int plantId)
        {
            foreach (var component in _children)
            {
                if (component is Plant p && p.Id == plantId)
                    return p;

                // Recursively search in nested composites
                if (component is PlantCollection collection)
                {
                    var found = collection.GetById(plantId);
                    if (found != null) return found;
                }
            }
            return null;
        }

        /// <summary>
        /// Get all children (both Plant leaves and nested Composites)
        /// </summary>
        public IEnumerable<GameShared.Types.GameObjects.IPlantComponent> GetChildren()
        {
            return _children.ToList();
        }

        // Composite operations: delegate to all children
        public bool IsReadyForNextStage()
        {
            foreach (var child in _children)
            {
                if (child.IsReadyForNextStage()) return true;
            }
            return false;
        }

        public void AdvanceStage()
        {
            foreach (var child in _children)
            {
                child.AdvanceStage();
            }
        }

        /// <summary>
        /// Get current tile type - composite simply returns "Group"
        /// Leaf (Plant) queries should come from GameWorldFacade layer
        /// </summary>
        public string GetCurrentTileType()
        {
            return "Group";
        }

        public bool IsMatured()
        {
            // Empty composite is considered matured; all children must be matured
            if (_children.Count == 0) return true;
            foreach (var child in _children)
            {
                if (!child.IsMatured()) return false;
            }
            return true;
        }

        public override IEnumerator GetEnumerator()
        {
            return new PlantIterator(_children);
        }

        /// <summary>
        /// Get iterator for manual control of iteration
        /// </summary>
        public PlantIterator GetIterator()
        {
            return new PlantIterator(_children);
        }

        /// <summary>
        /// Get all Plant leaves (flattened, including from nested composites)
        /// </summary>
        public List<Plant> GetAllPlants()
        {
            var result = new List<Plant>();
            foreach (var component in _children)
            {
                if (component is Plant p)
                {
                    result.Add(p);
                }
                else if (component is PlantCollection collection)
                {
                    result.AddRange(collection.GetAllPlants());
                }
            }
            return result;
        }
    }
}
