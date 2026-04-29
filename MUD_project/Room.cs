using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MUD_project
{
    internal class Room
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        // slovnik pro exits, klic je smer a hodnota je id mistnosti kam vede
        public Dictionary<string, string> Exits { get; set; }

        public List<string> Items { get; set; }

        public Room()
        {
            Exits = new Dictionary<string, string>();
            Items = new List<string>();
        }
    }
}
