using TcpServer2;

namespace MUD_project
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server(100);
            Console.WriteLine("Server bezi");
            Console.ReadLine();


        }
    }
}
