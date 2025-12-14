using System.Diagnostics;

namespace GameShared.Types.Map
{
    /// <summary>
    /// Virtual proxy that delays map creation/loading until first use
    /// and logs load time/memory impact for reporting.
    /// </summary>
    public class LazyMapProxy : IMap
    {
        private readonly string _mapPath;
        private readonly object _lock = new object();
        private Map? _realMap;

        // Metrics captured on first load for demonstration/reporting
        private long _loadElapsedMs;
        private long _loadDeltaBytes;

        public LazyMapProxy(string mapPath)
        {
            _mapPath = mapPath;
        }

        public int Width => RealMap.Width;
        public int Height => RealMap.Height;

        public TileData GetTile(int x, int y) => RealMap.GetTile(x, y);

        public void SetTile(int x, int y, TileData newTile) => RealMap.SetTile(x, y, newTile);

        // Proxy controls when file loading occurs, so this is intentionally a no-op.
        public void LoadFromText(string filePath)
        {
            // Intentionally ignored; mapPath is provided via constructor.
        }

        public void LoadFromDimensions(int width, int height)
        {
            lock (_lock)
            {
                if (_realMap == null)
                {
                    _realMap = new Map();
                }
                _realMap.LoadFromDimensions(width, height);
            }
        }

        /// <summary>
        /// Expose metrics for testing/reporting.
        /// </summary>
        public long LoadElapsedMs => _loadElapsedMs;
        public long LoadDeltaBytes => _loadDeltaBytes;

        private Map RealMap
        {
            get
            {
                if (_realMap != null) return _realMap;

                lock (_lock)
                {
                    if (_realMap == null)
                    {
                        var before = GC.GetTotalMemory(forceFullCollection: false);
                        var sw = Stopwatch.StartNew();

                        var map = new Map();
                        map.LoadFromText(_mapPath);

                        sw.Stop();
                        var after = GC.GetTotalMemory(forceFullCollection: false);

                        _realMap = map;
                        _loadElapsedMs = sw.ElapsedMilliseconds;
                        _loadDeltaBytes = after - before;

                        Console.WriteLine($"[MapProxy] Loaded map from '{_mapPath}' in {_loadElapsedMs} ms, Δmem={_loadDeltaBytes} bytes");
                    }
                }

                return _realMap;
            }
        }
    }
}
