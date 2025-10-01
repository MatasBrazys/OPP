using GameShared.Map;

namespace GameServer.GameLogic;

public class GameTile
{
    public TileData Tile { get; private set; }
    public GameTile(TileData tile)
    {
        Tile = tile;
    }

    public void Tick()
    {
        if (Tile is TreeTile tree)
        {
            // Example logic: Regrow tree after being harvested
            if (!tree.CanHarvest())
            {
                // Logic to regrow or reset the tree state
            }
        }
        // house ar grass gali turėti server-side eventus
    }
    public bool TryHarvestTree()
    {
        if (Tile is TreeTile tree && tree.CanHarvest())
        {
            tree.Harvest();
           
            return true;
        }
        return false;
    } 
}