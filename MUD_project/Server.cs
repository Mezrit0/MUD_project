using MUD_project;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TcpServer2
{
    public class Server
    {
        private TcpListener myServer;
        private bool isRunning;

        private List<Room> worldRooms = new List<Room>();
        private List<Player> activePlayers = new List<Player>();

        public Server(int port)
        {
            myServer = new TcpListener(System.Net.IPAddress.Any, port);
            myServer.Start();
            isRunning = true;
            Console.WriteLine("Server is running on port: " + port);

            LoadWorld();

            StartServer();
        }
        private void LoadWorld()
        {
            try
            {
                // precteme obsah JSON souboru
                string jsonText = File.ReadAllText("world.json");

                // prevedeme JSON text na seznam mistnosti 
                worldRooms = JsonSerializer.Deserialize<List<Room>>(jsonText);

                Console.WriteLine("The world has successfully loaded, number of rooms: " + worldRooms.Count);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occured while loading the world " + e.Message);
            }
        }

        private async Task StartServer()
        {
            while (isRunning)
            {
                // cekame na pripojeni
                TcpClient client = await myServer.AcceptTcpClientAsync();

                // tohle zajisti ze kazdy hrac bezi zvlast
                Task.Run(delegate { HandleClient(client); });
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
            writer.AutoFlush = true;

            Player currentPlayer = new Player(writer);

            await currentPlayer.SendMessage("Welcome to Night City");
            await currentPlayer.SendMessage("Enter your name:");

            // nastavime jmeno hrace, pomoci jeho inputu
            currentPlayer.Name = await reader.ReadLineAsync();

            // pridame hrace do seznamu aktivnich hracu, lock zajisti ze se do seznamu nepridava vice hracu najednou
            lock (activePlayers)
            {
                activePlayers.Add(currentPlayer);
            }
            await currentPlayer.SendMessage("Connected as " + currentPlayer.Name);

            string currentRoomId = "alley";


            bool clientConnect = true;
            while (clientConnect)
            {
                // asynchronni cteni
                string data = await reader.ReadLineAsync();

                if (data == null || data.ToLower() == "exit")
                {
                    clientConnect = false;
                }
                else
                {
                    string command = data.ToLower().Trim();

                    Room currentRoom = null;
                    foreach (Room room in worldRooms)
                    {
                        // koukneme jestli id místnosti odpovida místnosti, kde se hrac nachazi, pokud ano, ulozime ji do currentRoom
                        if (room.Id == currentPlayer.CurrentRoomId)
                        {
                            currentRoom = room;
                            break; 
                        }
                    }

                    if (command == "scan")
                    {
                        await currentPlayer.SendMessage("--- " + currentRoom.Name + " ---");
                        await currentPlayer.SendMessage(currentRoom.Description);

                        //  checkujem pro vice hracu ve stejne mistnosti, nez se znovu vypise popis mistnosti
                        await currentPlayer.SendMessage("Scanning for other players");

                        List<string> peopleHere = new List<string>();
                        lock (activePlayers)
                        {
                            foreach (Player other in activePlayers)
                            {
                                if (other.CurrentRoomId == currentPlayer.CurrentRoomId && other != currentPlayer)
                                {
                                    peopleHere.Add(other.Name);
                                }
                            }
                        }

                       
                        foreach (string name in peopleHere)
                        {
                            await currentPlayer.SendMessage("found: " + name + " is standing here.");
                        }

                        string exitsString = "Available exits: ";
                        foreach (var exit in currentRoom.Exits)
                        {
                            exitsString = exitsString + exit.Key + " ";
                        }
                        await currentPlayer.SendMessage(exitsString);
                    }
                    else if (command.StartsWith("say "))
                    {
                        // trimneme "say " z příkazu a získáme zprávu, kterou chce hrac říct
                        string message = data.Substring(4).Trim();
                        if (!string.IsNullOrEmpty(message))
                        {
                            string formattedMsg = currentPlayer.Name + " says: " + message;

                            // najdem vsechny playery, kteri jsou ve stejne mistnosti jako hrac, ktery chce mluvit
                            List<Player> targets = new List<Player>();
                            lock (activePlayers)
                            {
                                foreach (Player p in activePlayers)
                                {
                                    if (p.CurrentRoomId == currentPlayer.CurrentRoomId)
                                    {
                                        targets.Add(p);
                                    }
                                }
                            }

                            // posle vsem tem playerum v mistnosti tu zpravu
                            foreach (Player target in targets)
                            {
                            
                                await target.SendMessage(formattedMsg);
                            }
                        }
                    }
                    else if (command.StartsWith("go "))
                    {
                        // trimneme "go " z příkazu a získáme směr
                        string direction = command.Substring(3).Trim();

                        // Zkontrolujeme, jestli východ existuje v aktuální místnosti
                        if (currentRoom.Exits.ContainsKey(direction))
                        {
                            // pokud existuje exit, zmenime currentRoomId na id místnosti, kam vede
                            currentPlayer.CurrentRoomId = currentRoom.Exits[direction];
                            await writer.WriteLineAsync("You moved to the " + direction);
                            await writer.WriteLineAsync("Type (scan) to see where you are.");
                        }
                        else
                        {
                            await writer.WriteLineAsync("You cannot go that way");
                        }
                    }
                    else
                    {
                        await writer.WriteLineAsync("Unknown command");
                    }

                }
            }
            // odstranime hrace ze seznamu aktivnich hracu, kdyz se odpoji, lock zajisti ze se ze seznamu neodstranuje vice hracu najednou
            lock (activePlayers)
            {
                activePlayers.Remove(currentPlayer);
            }
            client.Close();
            Console.WriteLine("Player " + currentPlayer.Name + " disconnected.");
        }
    }
}
