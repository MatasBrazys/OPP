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
        /// This demonstrates the Composite pattern in action:
        /// - Creates individual plants (leaves)
        /// - Groups them into field collections (composites)
        /// - Combines fields into a farm (composite of composites)
        /// </summary>
        public void CreateDemoFarmWithComposites()
        {
            Console.WriteLine("\n=== Creating Demo Farm with Composite Plant Groups ===\n");

            // Create North Field (composite 1)
            var northField = new PlantCollection();
            var wheat1 = new Wheat(0, 2, 2);
            var wheat2 = new Wheat(0, 2, 4);
            var wheat3 = new Wheat(0, 4, 2);

            northField.Add(wheat1);
            northField.Add(wheat2);
            northField.Add(wheat3);

            Console.WriteLine($"North Field created with {northField.Count} plants");

            // Create South Field (composite 2)
            var southField = new PlantCollection();
            var carrot1 = new Plant(0, 2, 15, "Carrot");
            var carrot2 = new Plant(0, 4, 15, "Carrot");
            var carrot3 = new Plant(0, 6, 15, "Carrot");

            southField.Add(carrot1);
            southField.Add(carrot2);
            southField.Add(carrot3);

            Console.WriteLine($"South Field created with {southField.Count} plants");

            var farm = new PlantCollection();
            farm.Add(northField);   // Add entire field group!
            farm.Add(southField);   // Add another field group!

            Console.WriteLine($"Farm created as composite with {farm.Count} field groups");

            _plants.Add(farm);

            Console.WriteLine($"Farm added to world. Total top-level components: {_plants.Count}\n");

            Console.WriteLine("Demonstrating uniform operations on nested composite:");

            if (farm.IsReadyForNextStage())
            {
                Console.WriteLine("Farm is ready for growth - advancing all plants in all fields");
                farm.AdvanceStage();
            }

            Console.WriteLine($"Farm maturity status: {(farm.IsMatured() ? "All plants matured" : "Some plants still growing")}\n");

            // Demonstrate getting all leaves from nested structure
            var allPlants = farm.GetAllPlants();
            Console.WriteLine($"Total individual plants in farm: {allPlants.Count}");
            foreach (var plant in allPlants)
            {
                Console.WriteLine($"  - {plant.PlantType} at ({plant.X}, {plant.Y}) - Stage: {plant.CurrentStage}");
            }
            Console.WriteLine("\n=== Demo of Composite Plant Groups ===\n");
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

            // Always create a fresh iterator to include newly planted plants
            _plantIterator = _plants.GetIterator();

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
            const int TileSize = GameConstants.TILE_SIZE;

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

        /// <summary>
        /// Get all plantable tiles on the map
        /// </summary>
        public List<TileData> GetPlantableTiles()
        {
            var plantableTiles = new List<TileData>();
            for (int y = 0; y < _world.Map.Height; y++)
            {
                for (int x = 0; x < _world.Map.Width; x++)
                {
                    var tile = _world.Map.GetTile(x, y);
                    if (tile.Plantable)
                    {
                        plantableTiles.Add(tile);
                    }
                }
            }
            return plantableTiles;
        }

        /// <summary>
        /// Find all plants that are matured and ready to harvest
        /// </summary>
        public List<Plant> GetMaturePlants()
        {
            return _plants.GetIterator().GetAllPlants()
                .Where(p => p.IsMatured())
                .ToList();
        }

        /// <summary>
        /// Get a plant by its specific coordinates
        /// </summary>
        public Plant? GetPlantAtTile(int tileX, int tileY)
        {
            var iterator = _plants.GetIterator();
            return iterator.GetAllPlants().FirstOrDefault(p => p.X == tileX && p.Y == tileY);
        }

        /// <summary>
        /// Find all plants of a specific type
        /// </summary>
        public List<Plant> GetPlantsByType(string plantType)
        {
            return _plants.GetIterator().GetAllPlants()
                .Where(p => p.PlantType.Equals(plantType, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Check how many plants are currently in the game world
        /// </summary>
        public int GetTotalPlantCount()
        {
            return _plants.GetIterator().GetAllPlants().Count;
        }

        /// <summary>
        /// Find the first plant ready for growth update
        /// </summary>
        public Plant? GetNextPlantForGrowth()
        {
            var readyPlants = _plants.GetIterator().GetPlantsForGrowth();
            return readyPlants.FirstOrDefault();
        }

        /// <summary>
        /// Get statistics about current plants in the world
        /// </summary>
        public Dictionary<string, int> GetPlantStatistics()
        {
            var iterator = _plants.GetIterator();
            var allPlants = iterator.GetAllPlants();

            var stats = new Dictionary<string, int>
            {
                { "Total", allPlants.Count },
                { "ReadyForGrowth", iterator.GetPlantsForGrowth().Count },
                { "Matured", allPlants.Count(p => p.IsMatured()) }
            };

            // Count by plant type
            foreach (var plantType in allPlants.Select(p => p.PlantType).Distinct())
            {
                if (!stats.ContainsKey(plantType))
                {
                    stats[plantType] = 0;
                }
                stats[plantType] = allPlants.Count(p => p.PlantType == plantType);
            }

            return stats;
        }
    }
}
