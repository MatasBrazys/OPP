// File: GameClient/Program.cs
namespace GameClient
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new GameClientForm());
        }
    }
}
