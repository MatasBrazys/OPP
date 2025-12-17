using GameShared.Interfaces;
using GameShared.Types.GameObjects;

namespace GameShared.Types.Players.Visitors
{
    /// <summary>
    /// Mage visitor - collects 2 kg from each plant type
    /// </summary>
    public class MageVisitor : IPlantVisitor
    {
        public int VisitWheat(Wheat wheat)
        {
            return 2; // Mage collects 2 kg of wheat
        }

        public int VisitCarrot(Carrot carrot)
        {
            return 2; // Mage collects 2 kg of carrots
        }

        public int VisitPotato(Potato potato)
        {
            return 2; // Mage collects 2 kg of potatoes
        }
    }
}
