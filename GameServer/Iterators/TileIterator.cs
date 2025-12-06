using GameShared.Types.Map;
using GameShared.Types.Map.Decorators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameServer.Iterators
{
    public class TileIterator : Iterator
    {
        private readonly List<TileData> _tiles;
        private int _index;

        public TileIterator(IEnumerable<TileData> tiles)
        {
            _tiles = tiles?.ToList() ?? new List<TileData>();
            Reset();
        }

        public override int Key()
        {
            return _index;
        }

        public override object Current()
        {
            if (_index < 0 || _index >= _tiles.Count)
                throw new IndexOutOfRangeException("Iterator is out of bounds");
            return _tiles[_index];
        }

        public override bool MoveNext()
        {
            _index++;
            return _index < _tiles.Count;
        }

        public override void Reset()
        {
            _index = -1;
        }

        /// <summary>
        /// Get the inner tile if this is decorated tile, otherwise return the tile itself
        /// </summary>
        public static TileData Unwrap(TileData tile)
        {
            while (tile is TileLoggingDecorator decorator)
            {
                tile = decorator.Inner;
            }
            return tile;
        }

        /// <summary>
        /// Get the current tile as it is (cam be decorated)
        /// </summary>
        public TileData CurrentTile()
        {
            return (TileData)Current();
        }

        /// <summary>
        /// Get the current tile unwrapped (removes all decorators)
        /// </summary>
        public TileData CurrentTileUnwrapped()
        {
            return Unwrap(CurrentTile());
        }

        public List<TileData> FilterByType(string tileType)
        {
            return _tiles
                .Select(t => new { Original = t, Unwrapped = Unwrap(t) })
                .Where(x => x.Unwrapped.TileType == tileType)
                .Select(x => x.Original)
                .ToList();
        }

        public List<TileData> FilterByPassability(bool passable)
        {
            return _tiles
                .Select(t => new { Original = t, Unwrapped = Unwrap(t) })
                .Where(x => x.Unwrapped.Passable == passable)
                .Select(x => x.Original)
                .ToList();
        }

        /// <summary>
        /// Get tiles filtered by plantability
        /// </summary>
        public List<TileData> FilterByPlantability(bool plantable)
        {
            return _tiles
                .Select(t => new { Original = t, Unwrapped = Unwrap(t) })
                .Where(x => x.Unwrapped.Plantable == plantable)
                .Select(x => x.Original)
                .ToList();
        }
        /// <summary>
        /// Get decorator stack for a tile (from outer to inner)
        /// </summary>
        public List<TileLoggingDecorator> GetDecoratorStack(TileData tile)
        {
            var stack = new List<TileLoggingDecorator>();
            var current = tile;
            
            while (current is TileLoggingDecorator decorator)
            {
                stack.Add(decorator);
                current = decorator.Inner;
            }
            
            return stack;
        }
        public List<TileData> GetAll()
        {
            return new List<TileData>(_tiles);
        }

        public int Count => _tiles.Count;
    }
}
