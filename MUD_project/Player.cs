using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MUD_project
{
    internal class Player
    {
        public string Name { get; set; }
        public string CurrentRoomId { get; set; }
        private StreamWriter writer;
        public List<string> Inventory { get; set; } = new List<string>();
        public string Password { get; set; }

        public Player(StreamWriter _writer)
        {
            writer = _writer;
            CurrentRoomId = "alley";
        }

        public async Task SendMessage(string message)
        {
            await writer.WriteLineAsync(message);
        }
    }
}
