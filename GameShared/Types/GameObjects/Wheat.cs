using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameShared.Interfaces;

namespace GameShared.Types.GameObjects
{
    /// <summary>
    /// Wheat plant with specific growth stages
    /// </summary>
    public class Wheat : Plant
    {
        public Wheat() : base() { }

        public Wheat(int id, int x, int y) : base(id, x, y, "Wheat")
        {
        }

        protected override void InitializeStages()
        {
            var stages = GetStages();
            stages.Clear();
            stages.Add(0, (1500, "Grass"));        // Seed stage: 1.5 seconds
            stages.Add(1, (2000, "WheatPlant"));   // Sprout stage: 2 seconds
            stages.Add(2, (2500, "Wheat"));        // Growing stage: 2.5 seconds
            stages.Add(3, (0, "Wheat"));           // Mature stage: infinite
        }

        public override int Accept(IPlantVisitor visitor)
        {
            return visitor.VisitWheat(this);
        }
    }
}
