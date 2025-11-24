using System.Diagnostics;

namespace GameClient.Rendering.Flyweight
{
    /// <summary>
    /// Benchmarking utility to demonstrate Flyweight benefits
    /// </summary>
    public static class PerformanceBenchmark
    {
        /// <summary>
        /// Compares memory usage: with vs without Flyweight
        /// </summary>
        public static void RunBenchmark(int entityCount = 100)
        {
            Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║              FLYWEIGHT PATTERN BENCHMARK                 ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

            // Scenario 1: WITHOUT Flyweight (load sprite per entity)
            Console.WriteLine($"🔴 WITHOUT Flyweight: Loading {entityCount} entities with separate sprites...");
            long memoryBefore1 = GC.GetTotalMemory(true);
            var stopwatch1 = Stopwatch.StartNew();

            var spritesWithout = new List<Image>();
            for (int i = 0; i < entityCount; i++)
            {
                spritesWithout.Add(LoadSpriteDirectly("../assets/slime.png"));
            }

            stopwatch1.Stop();
            long memoryAfter1 = GC.GetTotalMemory(true);
            long memoryUsedWithout = memoryAfter1 - memoryBefore1;

            Console.WriteLine($"   ⏱️  Time: {stopwatch1.ElapsedMilliseconds}ms");
            Console.WriteLine($"   💾 Memory: {memoryUsedWithout / 1024.0:F2} KB");
            Console.WriteLine($"   📊 Per entity: {(memoryUsedWithout / entityCount) / 1024.0:F2} KB\n");

            // Cleanup
            foreach (var sprite in spritesWithout) sprite.Dispose();
            spritesWithout.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Scenario 2: WITH Flyweight (shared sprite)
            Console.WriteLine($"🟢 WITH Flyweight: Loading {entityCount} entities with shared sprite...");
            SpriteCache.Instance.Clear();
            
            long memoryBefore2 = GC.GetTotalMemory(true);
            var stopwatch2 = Stopwatch.StartNew();

            var spritesWith = new List<SpriteData>();
            for (int i = 0; i < entityCount; i++)
            {
                spritesWith.Add(SpriteLoader.LoadSprite("../assets/slime.png"));
            }

            stopwatch2.Stop();
            long memoryAfter2 = GC.GetTotalMemory(true);
            long memoryUsedWith = memoryAfter2 - memoryBefore2;

            Console.WriteLine($"   ⏱️  Time: {stopwatch2.ElapsedMilliseconds}ms");
            Console.WriteLine($"   💾 Memory: {memoryUsedWith / 1024.0:F2} KB");
            Console.WriteLine($"   📊 Per entity: {(memoryUsedWith / entityCount) / 1024.0:F2} KB\n");

            // Results
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("📊 COMPARISON RESULTS:");
            Console.WriteLine($"   💰 Memory Saved: {(memoryUsedWithout - memoryUsedWith) / 1024.0:F2} KB " +
                            $"({(1.0 - (double)memoryUsedWith / memoryUsedWithout):P1} reduction)");
            Console.WriteLine($"   ⚡ Speed Improvement: {stopwatch1.ElapsedMilliseconds - stopwatch2.ElapsedMilliseconds}ms faster");
            Console.WriteLine($"   🎯 Efficiency Factor: {(double)memoryUsedWithout / memoryUsedWith:F2}x better");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            SpriteCache.Instance.PrintReport();

            // Cleanup
            SpriteCache.Instance.Clear();
        }

        private static Image LoadSpriteDirectly(string path)
        {
            if (File.Exists(path))
                return Image.FromFile(path);
            
            var bmp = new Bitmap(64, 64);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.Magenta);
            return bmp;
        }
    }
}