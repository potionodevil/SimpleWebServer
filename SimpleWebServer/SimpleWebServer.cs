using System;
using System.Threading.Tasks;
using Intyaa.Server;

namespace SimpelWebServer
{
    public class SimpleWebServer
    {
        static async Task Main(string[] args)
        {
            Server.Start();
            Console.ReadLine();
        }
    }
}