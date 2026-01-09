using System.Collections.Generic;

namespace Maestro.Models
{
    public class Song
    {
        public string Name { get; set; }
        public string Artist { get; set; }
        public string Transcriber { get; set; }
        public InstrumentType Instrument { get; set; }
        public List<SongCommand> Commands { get; set; } = new List<SongCommand>();
        public List<string> Notes { get; set; } = new List<string>();
        public bool IsUserImported { get; set; }
        public bool SkipOctaveReset { get; set; }

        public string DisplayName => $"{Name} - {Artist}";
    }
}
