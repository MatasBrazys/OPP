// ./GameClient/Managers/SpriteManager.cs
using System.Diagnostics;
using System.Drawing;
using GameClient.Rendering;
using GameClient.Rendering.Flyweight;

namespace GameClient.Managers
{
    /// <summary>
    /// Updated SpriteManager using Flyweight pattern
    /// </summary>
    public static class SpriteManager
    {
        public static void RegisterDefaultSprites()
        {
            var stopwatch = Stopwatch.StartNew();
            long memoryBefore = GC.GetTotalMemory(true);

            // Load tiles using flyweight
            SpriteRegistry.Register("Grass", 
                SpriteLoader.LoadSprite("../assets/grass.png").Image);
            SpriteRegistry.Register("Tree", 
                SpriteLoader.LoadSprite("../assets/tree.png").Image);
            SpriteRegistry.Register("House", 
                SpriteLoader.LoadSprite("../assets/house.png").Image);
            SpriteRegistry.Register("Apple", 
                SpriteLoader.LoadSprite("../assets/apple.png").Image);
            SpriteRegistry.Register("Fish", 
                SpriteLoader.LoadSprite("../assets/fish.png").Image);
            SpriteRegistry.Register("Water", 
                SpriteLoader.LoadSprite("../assets/water.png").Image);
            SpriteRegistry.Register("Sand", 
                SpriteLoader.LoadSprite("../assets/sand.png").Image);
            SpriteRegistry.Register("Cherry", 
                SpriteLoader.LoadSprite("../assets/cherry.jpg").Image);
            SpriteRegistry.Register("Wheat", 
                SpriteLoader.LoadSprite("../assets/Wheat.png").Image);
            SpriteRegistry.Register("WheatPlant", 
                SpriteLoader.LoadSprite("../assets/WheatPlant.png").Image);
            SpriteRegistry.Register("Carrot", 
                SpriteLoader.LoadSprite("../assets/Carrots.png").Image);
            SpriteRegistry.Register("CarrotPlant", 
                SpriteLoader.LoadSprite("../assets/PlantedCarrots.png").Image);
            SpriteRegistry.Register("Potato", 
                SpriteLoader.LoadSprite("../assets/Potatos.png").Image);
            SpriteRegistry.Register("PotatoPlant", 
                SpriteLoader.LoadSprite("../assets/PlantedPotatos.png").Image);

            // Load players
            SpriteRegistry.Register("Mage", 
                SpriteLoader.LoadSprite("../assets/mage.png").Image);
            SpriteRegistry.Register("Hunter", 
                SpriteLoader.LoadSprite("../assets/hunter.png").Image);
            SpriteRegistry.Register("Defender", 
                SpriteLoader.LoadSprite("../assets/defender.png").Image);

            // Load enemies
            SpriteRegistry.Register("Slime", 
                SpriteLoader.LoadSprite("../assets/slime.png").Image);

            stopwatch.Stop();
            long memoryAfter = GC.GetTotalMemory(true);
            long memoryUsed = memoryAfter - memoryBefore;

            Console.WriteLine($"\n⏱️ [PERFORMANCE] Sprite loading completed in {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"💾 [MEMORY] Estimated memory used: {memoryUsed / 1024.0:F2} KB");
        }
    }
}