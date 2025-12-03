using GameShared.Types.GameObjects;
using System;
using System.Collections.Generic;

namespace GameServer.Iterators
{
    /// <summary>
    /// Iterator for managing plant collections and their growth stages
    /// </summary>
    public class PlantIterator : Iterator
    {
        private List<Plant> plants;
        private int index = 0;

        public PlantIterator(List<Plant> plants)
        {
            this.plants = plants ?? new List<Plant>();
        }

        public override object Current()
        {
            if (index < 0 || index >= plants.Count)
                throw new IndexOutOfRangeException("Iterator is out of bounds");
            return plants[index];
        }

        public override int Key()
        {
            return index;
        }

        public override bool MoveNext()
        {
            index++;
            return index < plants.Count;
        }

        public override void Reset()
        {
            index = 0;
        }

        /// <summary>
        /// Get all plants that are ready to advance to the next stage
        /// </summary>
        public List<Plant> GetPlantsForGrowth()
        {
            var readyPlants = new List<Plant>();
            foreach (var plant in plants)
            {
                if (plant.IsReadyForNextStage())
                {
                    readyPlants.Add(plant);
                }
            }
            return readyPlants;
        }

        /// <summary>
        /// Advance a plant to its next growth stage
        /// </summary>
        public void AdvancePlantStage(Plant plant)
        {
            if (plant != null && plants.Contains(plant))
            {
                plant.AdvanceStage();
            }
        }

        /// <summary>
        /// Get all current plants in the iterator
        /// </summary>
        public List<Plant> GetAllPlants()
        {
            return new List<Plant>(plants);
        }

        /// <summary>
        /// Add a plant to the iterator
        /// </summary>
        public void AddPlant(Plant plant)
        {
            if (plant != null && !plants.Contains(plant))
            {
                plants.Add(plant);
            }
        }

        /// <summary>
        /// Remove a plant from the iterator
        /// </summary>
        public bool RemovePlant(Plant plant)
        {
            return plants.Remove(plant);
        }

        /// <summary>
        /// Get plant by ID
        /// </summary>
        public Plant? GetPlantById(int id)
        {
            foreach (var plant in plants)
            {
                if (plant.Id == id)
                    return plant;
            }
            return null;
        }
    }
}
