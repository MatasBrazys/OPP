using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameShared.Interfaces;

namespace GameShared.Types.GameObjects
{
    /// <summary>
    /// Potato plant with specific growth stages
    /// </summary>
    public class Potato : Plant
    {
        public Potato() : base() { }

        public Potato(int id, int x, int y) : base(id, x, y, "Potato")
        {
        }

        protected override void InitializeStages()
        {
            var stages = GetStages();
            stages.Clear();
            stages.Add(0, (1500, "Grass"));        // Seed stage: 1.5 seconds
            stages.Add(1, (2000, "PotatoPlant"));  // Sprout stage: 2 seconds
            stages.Add(2, (2500, "Potato"));       // Growing stage: 2.5 seconds
            stages.Add(3, (0, "Potato"));          // Mature stage: infinite
        }

        public override int Accept(IPlantVisitor visitor)
        {
            return visitor.VisitPotato(this);
        }
    }
}
