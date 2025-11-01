//./GameServer/Facades/GameWorldFacade.cs
using GameShared.Factories;
using GameShared.Types.GameObjects;
using GameShared.Types.Map;
using GameShared.Types.Players;
using GameShared.Types.Enemies;
using GameServer.Combat;

using GameShared;
namespace GameServer.Facades
{
    public class GameWorldFacade
    {
        private readonly World _world;
        private readonly IPlayerFactory _playerFactory;
        private readonly IEnemyFactory _enemyFactory;

        private readonly GameObjectFactory _objectFactory;

        public GameWorldFacade(World world, IPlayerFactory playerFactory, GameObjectFactory objectFactory, IEnemyFactory enemyFactory)
        {
            _world = world;
            _playerFactory = playerFactory;
            _objectFactory = objectFactory;
            _enemyFactory = enemyFactory;
        }

        //player methods
        public PlayerRole CreatePlayer(string roleType, int id)
        {
            var (x, y) = FindPassableTile();
            var player = _playerFactory.CreatePlayer(roleType, id, x, y);
            if (roleType == "defender")
            {
                player.AttackStrategy = new DefenderAttackStrategy();

            }
            if (roleType == "mage")
            {

                player.AttackStrategy = new MageAttackStrategy();
                
            }    
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

        //enemy methods---

        //reik pridet strategijas enemy, kol kas kuria enemy be strategiju, o jei nori strategija,
        //  tai reikia sukurti enemy su strategija per factory
        public Enemy CreateEnemy(string type, int id)
        {
            var (x, y) = FindPassableTile();
            var enemy = _enemyFactory.CreateEnemy(type, id, x, y);
            _world.AddEntity(enemy);
            return enemy;
        }
        public void RemoveEnemy(Enemy enemy)
        {
            _world.RemoveEntity(enemy);
        }

        public List<Enemy> GetAllEnemies()
        {
            return _world.GetEnemies();
        }
        //map methods
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
        //movement methods
        public struct MoveResult
        {
            public bool Moved;
            public TileEnterResult? TileResult;
        }

        public TileEnterResult? TryMovePlayer(int playerId, int newX, int newY)
        {
            var player = _world.GetPlayer(playerId);
            if (player == null) return null;

            int tileX = newX / GameConstants.TILE_SIZE;
            int tileY = newY / GameConstants.TILE_SIZE;

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
            const int TileSize =  GameConstants.TILE_SIZE;

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
        //generic object methods
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
