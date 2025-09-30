namespace GameShared.Types.GameObjects
{
    public class Tree : GameObject
    {
        public Tree()
        {
            Type = "Tree";
            IsDestructible = true;
        }

        public override void Interact()
        {
            // Tree cutting logic
        }
    }
}