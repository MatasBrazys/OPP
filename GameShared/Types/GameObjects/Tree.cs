//./GameShared/Types/GameObjects/Tree.cs
namespace GameShared.Types.GameObjects
{
    public class Tree : GameObject
    {
        public Tree()
        {
            Type = "TreeObject";
            IsDestructible = true;
        }

        public override void Interact()
        {
            // Tree cutting logic
        }
    }
}