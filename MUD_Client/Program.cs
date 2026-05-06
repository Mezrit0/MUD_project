using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MUD_Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Konfigurace připojení (můžeš pak dát do configu, ale pro test stačí takto)
            string ip = "127.0.0.1";
            int port = 1235;

            try
            {
                using (TcpClient client = new TcpClient(ip, port))
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                {
                    Console.WriteLine($"Connected to Night City at {ip}:{port}");
                    Console.WriteLine("Type 'exit' to quit.");

                    // Task pro čtení zpráv ze serveru (běží na pozadí)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            while (true)
                            {
                                string message = await reader.ReadLineAsync();
                                if (message == null) break;
                                Console.WriteLine(message);
                            }
                        }
                        catch { Console.WriteLine("Disconnected from server."); }
                    });

                    // Hlavní smyčka pro posílání příkazů ze vstupu konzole
                    while (true)
                    {
                        string input = Console.ReadLine();
                        if (string.IsNullOrEmpty(input)) continue;

                        // TADY JE TA OPRAVA:
                        // Musime pridat \n a Flush, aby server vedel, ze jsme dopsali
                        await writer.WriteAsync(input + "\n");
                        await writer.FlushAsync();

                        if (input.ToLower().Trim() == "exit") break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}