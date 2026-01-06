using System.Collections.Generic;

namespace Maestro.Models
{
    public class Song
    {
        public string Name { get; set; }
        public string Artist { get; set; }
        public InstrumentType Instrument { get; set; }
        public int? Bpm { get; set; }
        public List<SongCommand> Commands { get; set; } = new List<SongCommand>();

        public string DisplayName => $"{Name} - {Artist}";
    }
}
