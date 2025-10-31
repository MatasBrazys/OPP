//  ./GameShared/Messages/WelcomeMessage.cs
using System.Threading.Tasks;
namespace GameShared.Messages
{
    public class WelcomeMessage : GameMessage
    {
        public int Id { get; set; }
        public WelcomeMessage() { Type = "welcome"; }
    }
}
