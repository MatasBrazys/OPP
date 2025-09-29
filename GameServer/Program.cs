using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                new Server().Start(5000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server error: {ex}");
            }
        }
    }
}
