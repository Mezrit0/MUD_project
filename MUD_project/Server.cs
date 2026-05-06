using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MUD_project
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

            Log("Server started on port " + port);
            LoadWorld();
            // Spustime hlavni smycku serveru na pozadi
            _ = StartServer();
        }

        // Metoda pro vypis do konzole a zaroven do souboru (Bod I2)
        private void Log(string message)
        {
            string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            Console.WriteLine(logLine);
            try
            {
                File.AppendAllText("server_log.txt", logLine + Environment.NewLine);
            }
            catch { }
        }

        // Ulozeni hrace do JSONu (Bod I3)
        private void SavePlayer(Player p)
        {
            try
            {
                if (!Directory.Exists("players")) Directory.CreateDirectory("players");
                string json = JsonSerializer.Serialize(p);
                // Trimujeme jmeno pro nazev souboru, aby tam nebyly mezery
                File.WriteAllText($"players/{p.Name.ToLower().Trim()}.json", json);
                Log($"Player {p.Name} state saved.");
            }
            catch (Exception e)
            {
                Log($"Error saving player {p.Name}: {e.Message}");
            }
        }

        // Nacitani sveta z externiho souboru (Bod I1)
        private void LoadWorld()
        {
            try
            {
                string jsonText = File.ReadAllText("world.json");
                worldRooms = JsonSerializer.Deserialize<List<Room>>(jsonText);
                Log("The world loaded, rooms: " + worldRooms.Count);
            }
            catch (Exception e)
            {
                Log("Error loading world: " + e.Message);
            }
        }

        private async Task StartServer()
        {
            while (isRunning)
            {
                // Cekani na noveho klienta (Bod I4 - vlastni klient se sem pripoji)
                TcpClient client = await myServer.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClient(client));
            }
        }

        private async Task BroadcastToRoom(string roomId, string message, Player exceptPlayer = null)
        {
            List<Player> targets;
            lock (activePlayers)
            {
                targets = activePlayers.Where(p => p.CurrentRoomId == roomId && p != exceptPlayer).ToList();
            }
            foreach (Player target in targets)
            {
                await target.SendMessage(message);
            }
        }


        private async Task HandleClient(TcpClient client)
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
            {
                Player currentPlayer = new Player(writer);

                
                await writer.WriteLineAsync("Welcome to Night City");
                await writer.WriteLineAsync("Enter your name:");

                string rawName = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(rawName)) return;
              
                currentPlayer.Name = rawName.Trim();

                await writer.WriteLineAsync("Enter your password:");
                string rawPass = await reader.ReadLineAsync();
                if (rawPass == null) return;

                // Sifrovani hesla do Base64 pro persistence (Bod I3)
                string pass = rawPass.Trim();
                byte[] passBytes = Encoding.UTF8.GetBytes(pass);
                string encryptedPass = Convert.ToBase64String(passBytes);

                string path = $"players/{currentPlayer.Name.ToLower()}.json";
                if (File.Exists(path))
                {
      
                    string savedData = File.ReadAllText(path);
                    Player loaded = JsonSerializer.Deserialize<Player>(savedData);

          
                    if (loaded.Password.Trim() != encryptedPass)
                    {
                        await writer.WriteLineAsync("Wrong password. Disconnecting...");
                        Log($"Failed login for: {currentPlayer.Name}");
                        return;
                    }

                    currentPlayer.CurrentRoomId = loaded.CurrentRoomId;
                    currentPlayer.Inventory = loaded.Inventory ?? new List<string>();
                    currentPlayer.Password = loaded.Password;
                    await writer.WriteLineAsync("Welcome back! Your progress was loaded.");
                }
                else
                {
                    // Registrace noveho hrace
                    currentPlayer.Password = encryptedPass;
                    currentPlayer.CurrentRoomId = "alley";
                    currentPlayer.Inventory = new List<string>();
                    await writer.WriteLineAsync("New account created!");
                }

                lock (activePlayers) { activePlayers.Add(currentPlayer); }
                Log($"Player {currentPlayer.Name} connected.");

                await writer.WriteLineAsync("--- READY ---");

                // HLAVNI SMYCKA HRY
                bool clientConnect = true;
                while (clientConnect)
                {
                    string data = await reader.ReadLineAsync();
                    if (data == null || data.ToLower().Trim() == "exit") break;

                    string command = data.ToLower().Trim();
                    Log($"{currentPlayer.Name} issued command: {command}");

                    Room currentRoom = worldRooms.FirstOrDefault(r => r.Id == currentPlayer.CurrentRoomId);
                    if (currentRoom == null) continue;

                    // Prikaz pro zobrazeni mistnosti
                    if (command == "scan")
                    {
                        await currentPlayer.SendMessage("--- " + currentRoom.Name + " ---");
                        await currentPlayer.SendMessage(currentRoom.Description);
                        if (currentRoom.Items != null && currentRoom.Items.Count > 0)
                            await currentPlayer.SendMessage("Items: " + string.Join(", ", currentRoom.Items));

                        string exitsString = "Exits: " + string.Join(", ", currentRoom.Exits.Keys);
                        await currentPlayer.SendMessage(exitsString);
                    }
                    // Prikaz pro zobrazeni vsech commandu
                    else if (command == "help")
                    {
                        await currentPlayer.SendMessage("Commands: scan, go [dir], say [msg], take [item], use [item], inv, exit");
                    }
                    // Pohyb 
                    else if (command.StartsWith("go "))
                    {
                        string direction = command.Substring(3).Trim();
                        if (currentRoom.Exits.ContainsKey(direction))
                        {
                            string nextRoomId = currentRoom.Exits[direction];

                            // Mechanika M11
                            if (nextRoomId == "office" && !currentPlayer.Inventory.Contains("keycard"))
                            {
                                await currentPlayer.SendMessage("The door is locked! You need a 'keycard'.");
                            }
                            else
                            {
                                await BroadcastToRoom(currentRoom.Id, $"{currentPlayer.Name} left towards {direction}.", currentPlayer);
                                currentPlayer.CurrentRoomId = nextRoomId;
                                await BroadcastToRoom(currentPlayer.CurrentRoomId, $"{currentPlayer.Name} arrived.", currentPlayer);
                                await currentPlayer.SendMessage("You moved to " + direction);

                                // Bod P1 - Dokonceni hry
                                if (currentPlayer.CurrentRoomId == "finish")
                                {
                                    await currentPlayer.SendMessage("CONGRATULATIONS! You've reached the end of the game!");
                                    break;
                                }
                            }
                        }
                        else await currentPlayer.SendMessage("Cannot go that way.");
                    }
                    // Sebrani predmetu
                    else if (command.StartsWith("take "))
                    {
                        string itemName = command.Substring(5).Trim();
                        string item = currentRoom.Items.FirstOrDefault(i => i.ToLower() == itemName);
                        if (item != null)
                        {
                            currentRoom.Items.Remove(item);
                            currentPlayer.Inventory.Add(item);
                            await currentPlayer.SendMessage($"Picked up: {item}");
                        }
                        else await currentPlayer.SendMessage("Item not found.");
                    }
                    // Inventar
                    else if (command == "inv")
                    {
                        await currentPlayer.SendMessage("Inventory: " + (currentPlayer.Inventory.Count > 0 ? string.Join(", ", currentPlayer.Inventory) : "empty"));
                    }
                    // Mechanika M10
                    else if (command.StartsWith("use "))
                    {
                        string itemName = command.Substring(4).Trim();
                        string itemInInv = currentPlayer.Inventory.FirstOrDefault(i => i.ToLower() == itemName);

                        if (itemInInv != null)
                        {
                            // Quest 
                            if (itemInInv == "chip" && currentPlayer.CurrentRoomId == "tech_lab")
                            {
                                currentPlayer.Inventory.Remove(itemInInv);
                                currentPlayer.Inventory.Add("keycard");
                                await currentPlayer.SendMessage("You gave the chip to the technician. He gave you a 'keycard'!");
                            }
                            else await currentPlayer.SendMessage("You can't use that here.");
                        }
                        else await currentPlayer.SendMessage("You don't have that.");
                    }
                    else await currentPlayer.SendMessage("Unknown command");
                }

                // Ulozeni stavu pri odpojeni
                SavePlayer(currentPlayer);
                lock (activePlayers) { activePlayers.Remove(currentPlayer); }
                Log($"Player {currentPlayer.Name} disconnected.");
            }
        }
    }
}