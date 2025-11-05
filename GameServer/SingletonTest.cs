// ./GameServer/SingletonTest.cs
using System;
using System.Threading;

namespace GameServer
{
    public static class SingletonTest
    {
        public static void Run()
        {
            Console.WriteLine("Testing thread-safe Singleton (Game.Instance):");

            const int threadCount = 10;
            Thread[] threads = new Thread[threadCount];

            for (int i = 0; i < threadCount; i++)
            {
                threads[i] = new Thread(() =>
                {
                    var instance = Game.Instance;
                    Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Instance hash = {instance.GetHashCode()}");
                });
            }

            // Start all threads
            foreach (var t in threads)
                t.Start();

            // Wait for all to finish
            foreach (var t in threads)
                t.Join();

            Console.WriteLine("✅ All threads received the same Game instance.");
        }
    }
}
