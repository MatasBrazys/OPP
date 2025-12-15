//./GameShared/Messages/AutoHarvestPlanMessage.cs
using System.Collections.Generic;

namespace GameShared.Messages
{
    /// <summary>
    /// Server response containing an ordered list of mature plants to harvest.
    /// </summary>
    public class AutoHarvestPlanMessage : GameMessage
    {
        public override string Type { get; set; } = "auto_harvest_plan";
        public List<AutoHarvestTarget> Targets { get; set; } = new List<AutoHarvestTarget>();
    }

    public class AutoHarvestTarget
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string PlantType { get; set; } = string.Empty;
    }
}
