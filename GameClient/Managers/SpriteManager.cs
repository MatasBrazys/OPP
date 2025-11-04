// ./GameClient/Managers/SpriteManager.cs
using System.Drawing;
using GameClient.Rendering;

namespace GameClient.Managers
{
    public static class SpriteManager
    {
        // Thin wrapper around your SpriteRegistry usage for a central spot to load assets.
        public static void RegisterDefaultSprites()
        {
            SpriteRegistry.Register("Grass", Image.FromFile("../assets/grass.png"));
            SpriteRegistry.Register("Tree", Image.FromFile("../assets/tree.png"));
            SpriteRegistry.Register("House", Image.FromFile("../assets/house.png"));
            SpriteRegistry.Register("Apple", Image.FromFile("../assets/apple.png"));
            SpriteRegistry.Register("Fish", Image.FromFile("../assets/fish.png"));
            SpriteRegistry.Register("Water", Image.FromFile("../assets/water.png"));
            SpriteRegistry.Register("Sand", Image.FromFile("../assets/sand.png"));
            SpriteRegistry.Register("Cherry", Image.FromFile("../assets/cherry.jpg"));

            SpriteRegistry.Register("Mage", Image.FromFile("../assets/mage.png"));
            SpriteRegistry.Register("Hunter", Image.FromFile("../assets/hunter.png"));
            SpriteRegistry.Register("Defender", Image.FromFile("../assets/defender.png"));

            SpriteRegistry.Register("Slime", Image.FromFile("../assets/slime.png"));

            // optional: register effect sprites like slash, arrow, fireball if present
            
        }
    }
}
