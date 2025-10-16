using GameShared.Types.Players;
using GameClient.Rendering;

namespace GameClient.Adapters
{
    public class PlayerDataAdapter : IRenderable
    {
        private readonly PlayerRole _player;

        public PlayerDataAdapter(PlayerRole player)
        {
            _player = player;
        }

        public int X => (int)_player.X;
        public int Y => (int)_player.Y;
        public string TextureName => _player.RoleType; // matches sprite registry
    }
}
