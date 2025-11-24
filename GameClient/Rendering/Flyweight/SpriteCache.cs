// File: GameClient/Rendering/Flyweight/SpriteCache.cs

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.Linq;

namespace GameClient.Rendering.Flyweight
{
    /// <summary>
    /// FLYWEIGHT PATTERN - Flyweight Factory
    /// Manages shared sprite instances to minimize memory usage
    /// </summary>
    public class SpriteCache
    {
        private static readonly Lazy<SpriteCache> _instance = new(() => new SpriteCache());
        public static SpriteCache Instance => _instance.Value;

        private readonly Dictionary<string, SpriteData> _spriteCache = new();
        private readonly object _lock = new();

        // Performance metrics
        private int _cacheHits = 0;
        private int _cacheMisses = 0;
        private long _totalMemorySaved = 0;

        private SpriteCache() { }

        /// <summary>
        /// Gets or loads a sprite. Returns shared instance if already cached.
        /// </summary>
        public SpriteData GetSprite(string key, Func<Image> loader, int estimatedSize = 0)
        {
            lock (_lock)
            {
                if (_spriteCache.TryGetValue(key, out var cached))
                {
                    _cacheHits++;
                    cached.IncrementReferenceCount();
                    
                    // Calculate memory saved by reusing
                    if (estimatedSize > 0)
                        _totalMemorySaved += estimatedSize;
                    
                    return cached;
                }

                // Cache miss - load new sprite
                _cacheMisses++;
                var sprite = new SpriteData(key, loader(), estimatedSize);
                _spriteCache[key] = sprite;
                return sprite;
            }
        }

        /// <summary>
        /// Gets sprite by key (assumes already loaded)
        /// </summary>
        public SpriteData? GetSprite(string key)
        {
            lock (_lock)
            {
                if (_spriteCache.TryGetValue(key, out var sprite))
                {
                    _cacheHits++;
                    return sprite;
                }
                _cacheMisses++;
                return null;
            }
        }

        /// <summary>
        /// Releases a reference to a sprite
        /// </summary>
        public void ReleaseSprite(string key)
        {
            lock (_lock)
            {
                if (_spriteCache.TryGetValue(key, out var sprite))
                {
                    sprite.DecrementReferenceCount();
                }
            }
        }

        /// <summary>
        /// Cleans up sprites with zero references
        /// </summary>
        public void CleanUnused()
        {
            lock (_lock)
            {
                var toRemove = _spriteCache
                    .Where(kvp => kvp.Value.ReferenceCount == 0)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in toRemove)
                {
                    _spriteCache[key].Dispose();
                    _spriteCache.Remove(key);
                }

                if (toRemove.Count > 0)
                    Console.WriteLine($"🧹 [FLYWEIGHT] Cleaned {toRemove.Count} unused sprites");
            }
        }

        /// <summary>
        /// Clears all cached sprites
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                foreach (var sprite in _spriteCache.Values)
                    sprite.Dispose();
                
                _spriteCache.Clear();
                Console.WriteLine("🗑️ [FLYWEIGHT] Cleared all sprites from cache");
            }
        }

        /// <summary>
        /// Gets current cache statistics
        /// </summary>
        public CacheStatistics GetStatistics()
        {
            lock (_lock)
            {
                return new CacheStatistics
                {
                    CachedSprites = _spriteCache.Count,
                    TotalReferences = _spriteCache.Values.Sum(s => s.ReferenceCount),
                    CacheHits = _cacheHits,
                    CacheMisses = _cacheMisses,
                    HitRate = _cacheHits + _cacheMisses > 0 
                        ? (double)_cacheHits / (_cacheHits + _cacheMisses) 
                        : 0.0,
                    EstimatedMemorySavedKB = _totalMemorySaved / 1024.0,
                    TotalEstimatedMemoryKB = _spriteCache.Values.Sum(s => s.EstimatedSizeBytes) / 1024.0
                };
            }
        }

        /// <summary>
        /// Prints detailed memory and performance report
        /// </summary>
        public void PrintReport()
        {
            var stats = GetStatistics();
            
            Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║          FLYWEIGHT PATTERN - PERFORMANCE REPORT          ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
            Console.WriteLine($"  Cached Sprites:        {stats.CachedSprites}");
            Console.WriteLine($"  Total References:      {stats.TotalReferences}");
            Console.WriteLine($"  Cache Hits:            {stats.CacheHits}");
            Console.WriteLine($"  Cache Misses:          {stats.CacheMisses}");
            Console.WriteLine($"  Hit Rate:              {stats.HitRate:P2}");
            Console.WriteLine($"  Memory Saved:          {stats.EstimatedMemorySavedKB:F2} KB");
            Console.WriteLine($"  Total Memory Used:     {stats.TotalEstimatedMemoryKB:F2} KB");
            Console.WriteLine($"  Memory Efficiency:     {(stats.EstimatedMemorySavedKB / Math.Max(1, stats.TotalEstimatedMemoryKB)):F2}x");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            // Detailed sprite breakdown
            lock (_lock)
            {
                if (_spriteCache.Any())
                {
                    Console.WriteLine("📊 Sprite Usage Breakdown:");
                    foreach (var kvp in _spriteCache.OrderByDescending(x => x.Value.ReferenceCount))
                    {
                        var sprite = kvp.Value;
                        Console.WriteLine($"   • {kvp.Key,-20} | Refs: {sprite.ReferenceCount,3} | " +
                                        $"Size: {sprite.EstimatedSizeBytes / 1024.0:F1} KB");
                    }
                    Console.WriteLine();
                }
            }
        }
    }

    /// <summary>
    /// FLYWEIGHT PATTERN - Concrete Flyweight
    /// Represents shared sprite data
    /// </summary>
    public class SpriteData : IDisposable
    {
        public string Key { get; }
        public Image Image { get; }
        public int EstimatedSizeBytes { get; }
        public int ReferenceCount { get; private set; }
        public DateTime LoadedAt { get; }

        public SpriteData(string key, Image image, int estimatedSize = 0)
        {
            Key = key;
            Image = image;
            EstimatedSizeBytes = estimatedSize > 0 ? estimatedSize : EstimateImageSize(image);
            ReferenceCount = 1;
            LoadedAt = DateTime.UtcNow;
        }

        public void IncrementReferenceCount() => ReferenceCount++;
        public void DecrementReferenceCount() => ReferenceCount = Math.Max(0, ReferenceCount - 1);

        private static int EstimateImageSize(Image image)
        {
            // Rough estimate: width * height * 4 bytes (RGBA)
            return image.Width * image.Height * 4;
        }

        public void Dispose()
        {
            Image?.Dispose();
        }
    }

    /// <summary>
    /// Statistics for cache performance
    /// </summary>
    public struct CacheStatistics
    {
        public int CachedSprites { get; set; }
        public int TotalReferences { get; set; }
        public int CacheHits { get; set; }
        public int CacheMisses { get; set; }
        public double HitRate { get; set; }
        public double EstimatedMemorySavedKB { get; set; }
        public double TotalEstimatedMemoryKB { get; set; }
    }
}