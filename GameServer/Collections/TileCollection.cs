using GameShared.Types.Map;
using GameServer.Iterators;
using System;
using System.Collections;
using System.Collections.Generic;

namespace GameServer.Collections
{
    public class TileCollection : IteratorAggregate
    {
        private readonly List<TileData> _tiles;
        private readonly object _lock = new object();

        public TileCollection()
        {
            _tiles = new List<TileData>();
        }

        public TileCollection(IEnumerable<TileData> initialTiles)
        {
            _tiles = new List<TileData>(initialTiles ?? new List<TileData>());
        }

        public override IEnumerator GetEnumerator()
        {
            lock (_lock)
            {
                return GetIterator();
            }
        }
        /// <summary>
        /// Get a TileIterator for manual control of iteration
        /// </summary>
        public Iterators.TileIterator GetIterator()
        {
            lock (_lock)
            {
                return new Iterators.TileIterator(new List<TileData>(_tiles));
            }
        }

        /// <summary>
        /// Add a tile to the collection
        /// </summary>
        public void Add(TileData tile)
        {
            if (tile != null)
            {
                lock (_lock)
                {
                    if (!_tiles.Contains(tile))
                    {
                        _tiles.Add(tile);
                    }
                }
            }
        }

        /// <summary>
        /// Remove a tile from the collection
        /// </summary>
        public bool Remove(TileData tile)
        {
            lock (_lock)
            {
                return _tiles.Remove(tile);
            }
        }

        /// <summary>
        /// Clear all tiles from the collection
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _tiles.Clear();
            }
        }

        /// <summary>
        /// Check if collection contains a tile
        /// </summary>
        public bool Contains(TileData tile)
        {
            lock (_lock)
            {
                return _tiles.Contains(tile);
            }
        }

        /// <summary>
        /// Get count of tiles
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _tiles.Count;
                }
            }
        }

        public List<TileData> GetAll()
        {
            lock (_lock)
            {
                return new List<TileData>(_tiles);
            }
        }

        /// <summary>
        /// Get tiles filtered by type
        /// </summary>
        public List<TileData> GetTilesByType(string tileType)
        {
            lock (_lock)
            {
                var iterator = GetIterator();
                return iterator.FilterByType(tileType);
            }
        }

        /// <summary>
        /// Get tiles filtered by passability
        /// </summary>
        public List<TileData> GetPassableTiles()
        {
            lock (_lock)
            {
                var iterator = GetIterator();
                return iterator.FilterByPassability(true);
            }
        }

        /// <summary>
        /// Get tiles filtered by plantability
        /// </summary>
        public List<TileData> GetPlantableTiles()
        {
            lock (_lock)
            {
                var iterator = GetIterator();
                return iterator.FilterByPlantability(true);
            }
        }
    }
}
