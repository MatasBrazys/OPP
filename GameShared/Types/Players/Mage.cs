//./GameShared/Types/Players/Hunter.cs
using System.Drawing;
using GameShared.Strategies;

namespace GameShared.Types.Players
{
    public class Mage : PlayerRole
    {
        public Mage() : base(new NormalMovement())
        {
        }
        public override PlayerRole DeepCopy()
        {
            var clone = new Mage();
            CopyBasePropertiesTo(clone);
            return clone;
        }
      
    }
}