//./GameShared/Messages/InputMessage.cs
namespace GameShared.Messages
{
    public class InputMessage : GameMessage
    {
        public int Dx { get; set; }
        public int Dy { get; set; }
        public InputMessage() { Type = "input"; }
    }
}
