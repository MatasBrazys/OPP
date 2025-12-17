using GameShared.Interfaces;
using GameShared.Types.GameObjects;

namespace GameShared.Types.Players.Visitors
{
    /// <summary>
    /// Hunter visitor - collects 5 kg from each plant type
    /// </summary>
    public class HunterVisitor : IPlantVisitor
    {
        public int VisitWheat(Wheat wheat)
        {
            return 5; // Hunter collects 5 kg of wheat
        }

        public int VisitCarrot(Carrot carrot)
        {
            return 5; // Hunter collects 5 kg of carrots
        }

        public int VisitPotato(Potato potato)
        {
            return 5; // Hunter collects 5 kg of potatoes
        }
    }
}
