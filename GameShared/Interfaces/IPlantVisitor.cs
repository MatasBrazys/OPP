using GameShared.Types.GameObjects;

namespace GameShared.Interfaces
{
    /// <summary>
    /// Visitor interface for collecting different types of plants.
    /// Each player role implements this to define how much of each plant type they can collect.
    /// </summary>
    public interface IPlantVisitor
    {
        /// <summary>
        /// Visit a Wheat plant and return the amount collected in kg
        /// </summary>
        int VisitWheat(Wheat wheat);

        /// <summary>
        /// Visit a Carrot plant and return the amount collected in kg
        /// </summary>
        int VisitCarrot(Carrot carrot);

        /// <summary>
        /// Visit a Potato plant and return the amount collected in kg
        /// </summary>
        int VisitPotato(Potato potato);
    }
}
