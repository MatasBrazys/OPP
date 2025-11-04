//./GameShared/Types/Players/Defender.cs
using System.Drawing;
using GameShared.Strategies;

namespace GameShared.Types.Players
{
    public class Defender : PlayerRole
    {
        public Defender() : base(new NormalMovement())
        {
        }
        public override PlayerRole DeepCopy()
        {
            var clone = new Defender();
            CopyBasePropertiesTo(clone);
            return clone;
        }
        
    }
}