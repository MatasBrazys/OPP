using System;
using System.Collections.Generic;
using GameShared.Types.Map;
using GameShared.Interfaces;

namespace GameShared.Types.GameObjects
{
    /// <summary>
    /// Represents a plant that grows through multiple stages over time.
    /// </summary>
    public class Plant : IPlantComponent
    {
        public int Id { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int CurrentStage { get; set; } = 0;
        public DateTime PlantedTime { get; set; }
        public DateTime NextStageTime { get; set; }
        public string PlantType { get; set; } = "Plant";

        // IPlantComponent: leaf behavior
        public IEnumerable<IPlantComponent> GetChildren()
        {
            return new List<IPlantComponent>();
        }

        public void Add(IPlantComponent component)
        {
            throw new NotSupportedException("Cannot add child to Plant leaf");
        }

        public bool Remove(IPlantComponent component)
        {
            return false;
        }

        /// <summary>
        /// Defines the growth stages: stage -> (duration in milliseconds, tile type)
        /// </summary>
        private Dictionary<int, (long DurationMs, string TileType)> _stages = new();

        public Plant()
        {
            Id = 0;
            X = 0;
            Y = 0;
            PlantedTime = DateTime.UtcNow;
            NextStageTime = DateTime.UtcNow;
            InitializeStages();
        }

        public Plant(int id, int x, int y, string plantType = "Plant")
        {
            Id = id;
            X = x;
            Y = y;
            PlantType = plantType;
            PlantedTime = DateTime.UtcNow;
            CurrentStage = 0;
            InitializeStages();
            UpdateNextStageTime();
        }

        /// <summary>
        /// Initialize growth stages. Override in derived classes for custom stages
        /// Stage 0: Seed (1 second)
        /// Stage 1: Sprout (2 seconds)
        /// Stage 2: Growing (3 seconds)
        /// Stage 3: Mature (final)
        /// </summary>
        protected virtual void InitializeStages()
        {
            _stages = new Dictionary<int, (long, string)>
            {
                { 0, (1000, "Grass") },      // Stage 0: Seed (1 second), displayed as grass
                { 1, (2000, "WheatPlant") }, // Stage 1: Sprout (2 seconds), displayed as wheat plant
                { 2, (3000, "Wheat") },      // Stage 2: Growing (3 seconds), displayed as wheat
                { 3, (0, "Wheat") }          // Stage 3: Mature (infinite, displayed as wheat)
            };
        }

        public Dictionary<int, (long DurationMs, string TileType)> GetStages() => _stages;

        public bool IsReadyForNextStage()
        {
            return CurrentStage < _stages.Count - 1 && DateTime.UtcNow >= NextStageTime;
        }

        public void AdvanceStage()
        {
            if (CurrentStage < _stages.Count - 1)
            {
                CurrentStage++;
                UpdateNextStageTime();
            }
        }

        private void UpdateNextStageTime()
        {
            if (_stages.TryGetValue(CurrentStage, out var stageInfo))
            {
                NextStageTime = DateTime.UtcNow.AddMilliseconds(stageInfo.DurationMs);
            }
        }

        public string GetCurrentTileType()
        {
            if (_stages.TryGetValue(CurrentStage, out var stageInfo))
            {
                return stageInfo.TileType;
            }
            return "Grass";
        }

        public bool IsMatured()
        {
            return CurrentStage >= _stages.Count - 1;
        }

        /// <summary>
        /// Accept method for the Visitor pattern.
        /// Override in derived classes to call the appropriate visitor method.
        /// </summary>
        public virtual int Accept(IPlantVisitor visitor)
        {
            // Base implementation returns 0
            return 0;
        }

        public override string ToString()
        {
            return $"Plant[ID:{Id}, Type:{PlantType}, Pos:({X},{Y}), Stage:{CurrentStage}/{_stages.Count - 1}]";
        }
    }
}
