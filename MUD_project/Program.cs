
using System.Text.Json;
using System.IO;

namespace MUD_project
{
    internal class Program
    {
        static void Main(string[] args)
        {
      
            string configText = File.ReadAllText("config.json");
            ServerConfig config = JsonSerializer.Deserialize<ServerConfig>(configText);

   
            Server server = new Server(config.Port);

            Console.WriteLine($"Server bezi na portu {config.Port}");
            Console.ReadLine();


        }
    }
}
