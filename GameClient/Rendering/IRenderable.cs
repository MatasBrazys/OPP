//gameclient/rendering/irenderable.cs
namespace GameClient.Rendering
{
    public interface IRenderable
    {
        string TextureName { get; }
        int X { get; }
        int Y { get; }
        
    }
}