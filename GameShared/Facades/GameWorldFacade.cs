using GameShared.Factories;
using GameShared.Types.GameObjects;
using GameShared.Types.Map;
using GameShared.Types.Players;

namespace GameShared.Facades
{
    public class GameWorldFacade
    {
        private readonly World _world;
        private readonly IPlayerFactory _playerFactory;
        private readonly GameObjectFactory _objectFactory;

        public GameWorldFacade(World world, IPlayerFactory playerFactory, GameObjectFactory objectFactory)
        {
            _world = world;
            _playerFactory = playerFactory;
            _objectFactory = objectFactory;
        }

        public PlayerRole CreatePlayer(string roleType, int id)
        {
            var (x, y) = FindPassableTile();
            var player = _playerFactory.CreatePlayer(roleType, id, x, y);
            _world.AddEntity(player);
            return player;
        }

        public void AddPlayer(PlayerRole player)
        {
            _world.AddEntity(player);
        }

        public void RemovePlayer(PlayerRole player)
        {
            _world.RemoveEntity(player);
        }

        public PlayerRole? GetPlayer(int id)
        {
            return _world.GetPlayer(id);
        }

        public List<PlayerRole> GetAllPlayers()
        {
            return _world.GetPlayers();
        }

        public TileData? GetTileAt(int x, int y)
        {
            return _world.Map.GetTile(x, y);
        }

        public void ReplaceTile(int x, int y, TileData newTile)
        {
            _world.Map.SetTile(x, y, newTile);
        }

        public (int width, int height) GetMapSize()
        {
            return (_world.Map.Width, _world.Map.Height);
        }

        public bool IsTilePassable(int x, int y)
        {
            var tile = _world.Map.GetTile(x, y);
            return tile.Passable;
        }

        public struct MoveResult
        {
            public bool Moved;
            public TileEnterResult? TileResult;
        }

        public TileEnterResult? TryMovePlayer(int playerId, int newX, int newY)
        {
            var player = _world.GetPlayer(playerId);
            if (player == null) return null;

            int tileX = newX / 128;
            int tileY = newY / 128;

            if (tileX < 0 || tileX >= _world.Map.Width || tileY < 0 || tileY >= _world.Map.Height)
                return null;

            var tile = _world.Map.GetTile(tileX, tileY);
            if (!player.CanMove(tile))
                return null;

            var result = tile.OnEnter(player);
            player.OnMoveTile(tile);
            player.X = newX;
            player.Y = newY;

            if (result.ReplaceWithGrass)
                _world.Map.SetTile(tileX, tileY, new GrassTile(tileX, tileY));

            return result;
        }

        private (int x, int y) FindPassableTile()
        {
            const int TileSize = 128;

            for (int ty = 0; ty < _world.Map.Height; ty++)
            {
                for (int tx = 0; tx < _world.Map.Width; tx++)
                {
                    if (_world.Map.GetTile(tx, ty).Passable)
                    {
                        return (tx * TileSize, ty * TileSize);
                    }
                }
            }

            return (0, 0);
        }

        public void AddObject(GameObject obj)
        {
            _world.AddEntity(obj);
        }

        public void RemoveObject(GameObject obj)
        {
            _world.RemoveEntity(obj);
        }

        public void UpdateWorld()
        {
            _world.Update();
        }
    }
}
