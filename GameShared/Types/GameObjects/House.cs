namespace GameShared.Types.GameObjects
{
    public class House : GameObject
    {
        public House()
        {
            Type = "House";
            IsDestructible = true;
        }

        public override void Interact()
        {
            // House interaction logic
        }
    }
}