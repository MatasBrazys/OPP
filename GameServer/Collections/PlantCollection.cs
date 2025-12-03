using GameShared.Types.GameObjects;
using GameServer.Iterators;
using System;
using System.Collections;
using System.Collections.Generic;

namespace GameServer.Collections
{
    /// <summary>
    /// Collection for managing plants with growth stage updates
    /// </summary>
    public class PlantCollection : IteratorAggregate
    {
        private List<Plant> plants = new List<Plant>();
        private static int _nextPlantId = 1;

        public void Add(Plant plant)
        {
            if (plant == null)
                throw new ArgumentNullException(nameof(plant));
            
            // Auto-assign ID if not set
            if (plant.Id == 0)
            {
                plant.Id = _nextPlantId++;
            }
            
            plants.Add(plant);
        }

        public bool Remove(Plant plant)
        {
            return plants.Remove(plant);
        }

        public int Count => plants.Count;

        public Plant GetAt(int index)
        {
            if (index < 0 || index >= plants.Count)
                throw new IndexOutOfRangeException("Index is out of bounds");
            return plants[index];
        }

        public void Clear()
        {
            plants.Clear();
        }

        /// <summary>
        /// Get a plant by its ID
        /// </summary>
        public Plant? GetById(int plantId)
        {
            foreach (var plant in plants)
            {
                if (plant.Id == plantId)
                    return plant;
            }
            return null;
        }

        public override IEnumerator GetEnumerator()
        {
            return new PlantIterator(new List<Plant>(plants));
        }

        /// <summary>
        /// Get iterator for manual control of iteration
        /// </summary>
        public PlantIterator GetIterator()
        {
            return new PlantIterator(new List<Plant>(plants));
        }
    }
}
