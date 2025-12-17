//./GameShared/Types/Players/Hunter.cs
using System.Drawing;
using GameShared.Strategies;
using GameShared.Interfaces;
using GameShared.Types.Players.Visitors;

namespace GameShared.Types.Players
{
    public class Hunter : PlayerRole
    {
        public Hunter() : base(new NormalMovement())
        {
        }
        public override PlayerRole DeepCopy()
        {
            var clone = new Hunter();
            CopyBasePropertiesTo(clone);
            return clone;
        }

        public IPlantVisitor GetPlantVisitor()
        {
            return new HunterVisitor();
        }
    }
}