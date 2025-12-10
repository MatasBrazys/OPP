using System.Collections.Generic;

namespace GameShared.Types.GameObjects
{
    /// <summary>
    /// Component interface for Composite pattern for plants/plant groups.
    /// </summary>
    public interface IPlantComponent
    {
        int Id { get; }
        string PlantType { get; }

        IEnumerable<IPlantComponent> GetChildren();
        void Add(IPlantComponent component);
        bool Remove(IPlantComponent component);

        bool IsReadyForNextStage();
        void AdvanceStage();
        string GetCurrentTileType();
        bool IsMatured();
    }
}
