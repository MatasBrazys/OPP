//./GameServer/Facades/GameWorldFacade.cs
using GameServer.Factories;
using GameShared.Types.GameObjects;
using GameShared.Types.Map;
using GameShared.Types.Players;
using GameShared.Types.Enemies;
using GameServer.Combat;
using GameServer.Collections;
using GameServer.Iterators;

using GameShared;
namespace GameServer.Facades
{
    public class GameWorldFacade
    {
        private readonly World _world;
        private readonly IPlayerFactory _playerFactory;
        private readonly IEnemyFactory _enemyFactory;
        private readonly PlantCollection _plants;
        private PlantIterator? _plantIterator;

        // Event for notifying about plant updates
        public event Action<Plant, string>? OnPlantGrew;

        public GameWorldFacade(World world, IPlayerFactory playerFactory, IEnemyFactory enemyFactory)
        {
            _world = world;
            _playerFactory = playerFactory;
            _enemyFactory = enemyFactory;
            _plants = new PlantCollection();
        }

        //player methods
        public PlayerRole CreatePlayer(string roleType, int id)
        {
            var (x, y) = FindPassableTile();
            var player = _playerFactory.CreatePlayer(roleType, id, x, y); 
            _world.Add(player);
            return player;
        }

        public void AddPlayer(PlayerRole player)
        {
            _world.Add(player);
        }

        public void RemovePlayer(PlayerRole player)
        {
            _world.Remove(player);
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
            _world.Add(enemy);
            return enemy;
        }
        public void RemoveEnemy(Enemy enemy)
        {
            _world.Remove(enemy);
        }

        public List<Enemy> GetAllEnemies()
        {
            return _world.GetEnemies();
        }

        // ===== PLANT METHODS =====

        /// <summary>
        /// Plant a new plant at the specified tile position
        /// </summary>
        public Plant PlantSeed(int tileX, int tileY, string plantType = "Plant")
        {
            // Create new plant
            Plant newPlant = plantType.ToLower() switch
            {
                "wheat" => new Wheat(0, tileX, tileY),
                _ => new Plant(0, tileX, tileY, plantType)
            };

            // Add to collection
            _plants.Add(newPlant);

            // Update the map tile to show the plant's initial state
            var currentTile = _world.Map.GetTile(tileX, tileY);
            string initialTileType = newPlant.GetCurrentTileType();
            UpdatePlantTile(tileX, tileY, initialTileType);

            Console.WriteLine($"Planted {newPlant.PlantType} at ({tileX}, {tileY}): {newPlant}");

            return newPlant;
        }

        /// <summary>
        /// Remove a plant from the collection and map
        /// </summary>
        public void HarvestPlant(Plant plant)
        {
            if (plant != null)
            {
                _plants.Remove(plant);
                // Replace plant tile with grass
                _world.Map.SetTile(plant.X, plant.Y, new GrassTile(plant.X, plant.Y));
                Console.WriteLine($"Harvested plant at ({plant.X}, {plant.Y})");
            }
        }

        /// <summary>
        /// Get all plants
        /// </summary>
        public List<Plant> GetAllPlants()
        {
            return _plants.GetIterator().GetAllPlants();
        }

        /// <summary>
        /// Update plant growth stages and notify clients
        /// Should be called periodically from the game loop
        /// </summary>
        public List<Plant> UpdatePlantGrowth()
        {
            var updatedPlants = new List<Plant>();

            if (_plantIterator == null)
            {
                _plantIterator = _plants.GetIterator();
            }

            // Get all plants ready for growth
            var readyPlants = _plantIterator.GetPlantsForGrowth();

            foreach (var plant in readyPlants)
            {
                _plantIterator.AdvancePlantStage(plant);
                
                // Update the tile on the map to reflect new growth stage
                string newTileType = plant.GetCurrentTileType();
                UpdatePlantTile(plant.X, plant.Y, newTileType);

                updatedPlants.Add(plant);

                Console.WriteLine($"Plant grew: {plant}");

                // Trigger event for broadcasting to clients
                OnPlantGrew?.Invoke(plant, newTileType);
            }

            return updatedPlants;
        }

        /// <summary>
        /// Update the tile representation of a plant on the map
        /// </summary>
        private void UpdatePlantTile(int tileX, int tileY, string tileType)
        {
            TileData newTile;
            
            if (tileType == "Wheat")
                newTile = new WheatTile(tileX, tileY);
            else if (tileType == "WheatPlant")
                newTile = new WheatPlantTile(tileX, tileY);
            else
                newTile = new GrassTile(tileX, tileY);

            _world.Map.SetTile(tileX, tileY, newTile);
        }

        /// <summary>
        /// Get a specific plant by ID
        /// </summary>
        public Plant? GetPlantById(int plantId)
        {
            return _plants.GetIterator().GetPlantById(plantId);
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
            _world.Add(obj);
        }

        public void RemoveObject(GameObject obj)
        {
            _world.Remove(obj);
        }

        public void UpdateWorld()
        {
            _world.Update();
        }

        public void DemoLeafCannotHaveChildren()
        {
            Console.WriteLine("\n\n=== Composite Pattern Demo: Leaf Safety ===");

            // Use existing facade methods to create leaves
            var demoPlayer = CreatePlayer("Defender", 999);
            var demoEnemy = CreateEnemy("slime", 9999);

            try
            {
                Console.WriteLine("Attempting to add a child to PlayerRole (leaf)...");
                demoPlayer.Add(demoEnemy); // should throw
            }
            catch (NotSupportedException ex)
            {
                Console.WriteLine($"Expected exception caught: {ex.Message}");
            }

            try
            {
                Console.WriteLine("Attempting to add a child to Enemy (leaf)...");
                demoEnemy.Add(demoPlayer); // should throw
            }
            catch (NotSupportedException ex)
            {
                Console.WriteLine($"Expected exception caught: {ex.Message}");
            }

            Console.WriteLine("=== Demo complete: leaves cannot have children. === \n\n");

            RemoveEnemy(demoEnemy);
            RemovePlayer(demoPlayer);
        }
    }
}
