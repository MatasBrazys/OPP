
//./GameShared/Messages/ErrorMessage.cs
namespace GameShared.Messages
{
    public class ErrorMessage : GameMessage
    {
        public string Code { get; set; } = "";
        public string Detail { get; set; } = "";
        public ErrorMessage() { Type = "error"; }
    }
}
