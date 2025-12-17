using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameShared.Types.GameObjects
{
    /// <summary>
    /// Carrot plant with specific growth stages
    /// </summary>
    public class Carrot : Plant
    {
        public Carrot() : base() { }

        public Carrot(int id, int x, int y) : base(id, x, y, "Carrot")
        {
        }

        protected override void InitializeStages()
        {
            var stages = GetStages();
            stages.Clear();
            stages.Add(0, (1500, "Grass"));        // Seed stage: 1.5 seconds
            stages.Add(1, (2000, "CarrotPlant"));  // Sprout stage: 2 seconds
            stages.Add(2, (2500, "Carrot"));       // Growing stage: 2.5 seconds
            stages.Add(3, (0, "Carrot"));          // Mature stage: infinite
        }
    }
}
