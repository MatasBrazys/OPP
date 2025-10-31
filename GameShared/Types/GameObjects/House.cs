//./GameShared/Types/GameObjects/House.cs
namespace GameShared.Types.GameObjects
{
    public class House : GameObject
    {
        public House()
        {
            Type = "HouseObject";
            IsDestructible = true;
        }

        public override void Interact()
        {
            // House interaction logic
        }
    }
}