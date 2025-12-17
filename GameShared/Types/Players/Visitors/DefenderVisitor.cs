using GameShared.Interfaces;
using GameShared.Types.GameObjects;

namespace GameShared.Types.Players.Visitors
{
    /// <summary>
    /// Defender visitor - collects 4 kg from each plant type
    /// </summary>
    public class DefenderVisitor : IPlantVisitor
    {
        public int VisitWheat(Wheat wheat)
        {
            return 4; // Defender collects 4 kg of wheat
        }

        public int VisitCarrot(Carrot carrot)
        {
            return 4; // Defender collects 4 kg of carrots
        }

        public int VisitPotato(Potato potato)
        {
            return 4; // Defender collects 4 kg of potatoes
        }
    }
}
