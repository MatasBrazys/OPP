using GameShared.Types.GameObjects;

namespace GameShared.Factories
{
    public class GameObjectFactory
    {
        public GameObject CreateObject(string type, int x, int y)
        {
            GameObject gameObject = type.ToLower() switch
            {
                "tree" => new Tree(),
                "house" => new House(),
                // Add other game objects
                _ => throw new ArgumentException("Invalid game object type")
            };

            gameObject.X = x;
            gameObject.Y = y;

            return gameObject;
        }
    }
}