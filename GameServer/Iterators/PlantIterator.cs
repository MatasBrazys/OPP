using GameShared.Types.GameObjects;
using GameServer.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameServer.Iterators
{
    /// <summary>
    /// Iterator for managing plant collections and their growth stages
    /// Works with IPlantComponent to support both Plant leaves and nested Composites
    /// </summary>
    public class PlantIterator : Iterator
    {
        private List<IPlantComponent> components;
        private int index = 0;

        public PlantIterator(List<IPlantComponent> components)
        {
            this.components = components ?? new List<IPlantComponent>();
        }

        // Convenience constructor for Plant-only lists
        public PlantIterator(List<Plant> plants)
        {
            this.components = plants?.Cast<IPlantComponent>().ToList() ?? new List<IPlantComponent>();
        }

        public override object Current()
        {
            if (index < 0 || index >= components.Count)
                throw new IndexOutOfRangeException("Iterator is out of bounds");
            return components[index];
        }

        public override int Key()
        {
            return index;
        }

        public override bool MoveNext()
        {
            index++;
            return index < components.Count;
        }

        public override void Reset()
        {
            index = 0;
        }

        /// <summary>
        /// Get all components (Plant leaves or Composites) that are ready to advance to the next stage
        /// </summary>
        public List<IPlantComponent> GetComponentsForGrowth()
        {
            var readyComponents = new List<IPlantComponent>();
            foreach (var component in components)
            {
                if (component.IsReadyForNextStage())
                {
                    readyComponents.Add(component);
                }
            }
            return readyComponents;
        }

        /// <summary>
        /// Get all Plant leaves (flattened from all components, including nested composites)
        /// </summary>
        public List<Plant> GetAllPlants()
        {
            var allPlants = new List<Plant>();
            foreach (var component in components)
            {
                if (component is Plant p)
                {
                    allPlants.Add(p);
                }
                else if (component is PlantCollection collection)
                {
                    allPlants.AddRange(collection.GetAllPlants());
                }
            }
            return allPlants;
        }

        /// <summary>
        /// Get all plants that are ready to advance to the next stage (flattened)
        /// </summary>
        public List<Plant> GetPlantsForGrowth()
        {
            var readyPlants = new List<Plant>();
            var allPlants = GetAllPlants();
            foreach (var plant in allPlants)
            {
                if (plant.IsReadyForNextStage())
                {
                    readyPlants.Add(plant);
                }
            }
            return readyPlants;
        }

        /// <summary>
        /// Advance a component to its next growth stage
        /// Works for both leaves and composites. We don't require the
        /// component to be a direct child of this iterator's list because
        /// the caller may have obtained the component via a flattened query.
        /// </summary>
        public void AdvanceComponentStage(IPlantComponent component)
        {
            if (component == null) return;
            component.AdvanceStage();
        }

        /// <summary>
        /// Advance a plant to its next growth stage (backward compat)
        /// </summary>
        public void AdvancePlantStage(Plant plant)
        {
            if (plant == null) return;
            plant.AdvanceStage();
        }

        /// <summary>
        /// Add a component to the iterator
        /// </summary>
        public void AddComponent(IPlantComponent component)
        {
            if (component != null && !components.Contains(component))
            {
                components.Add(component);
            }
        }

        /// <summary>
        /// Add a plant to the iterator (backward compat)
        /// </summary>
        public void AddPlant(Plant plant)
        {
            AddComponent(plant);
        }

        /// <summary>
        /// Remove a component from the iterator
        /// </summary>
        public bool RemoveComponent(IPlantComponent component)
        {
            return components.Remove(component);
        }

        /// <summary>
        /// Remove a plant from the iterator (backward compat)
        /// </summary>
        public bool RemovePlant(Plant plant)
        {
            return RemoveComponent(plant);
        }

        /// <summary>
        /// Get plant by ID (searches all leaves, including nested)
        /// </summary>
        public Plant? GetPlantById(int id)
        {
            var allPlants = GetAllPlants();
            foreach (var plant in allPlants)
            {
                if (plant.Id == id)
                    return plant;
            }
            return null;
        }
    }
}
