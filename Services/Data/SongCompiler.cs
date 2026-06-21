using System.Collections.Generic;
using Maestro.Models;

namespace Maestro.Services.Data
{
    /// <summary>
    /// Routes note compilation, duration, and seek-data to the melodic NoteParser
    /// or the percussion DrumParser based on the instrument's IsPercussion flag.
    /// </summary>
    public static class SongCompiler
    {
        private static bool IsPercussion(InstrumentType instrument) =>
            InstrumentCatalog.Get(instrument).IsPercussion;

        public static NoteParser.ParseResult ParseWithMapping(List<string> notes, InstrumentType instrument) =>
            IsPercussion(instrument) ? DrumParser.ParseWithMapping(notes) : NoteParser.ParseWithMapping(notes);

        public static List<SongCommand> Parse(List<string> notes, InstrumentType instrument) =>
            IsPercussion(instrument) ? DrumParser.Parse(notes) : NoteParser.Parse(notes);

        public static long CalculateDurationMs(List<string> notes, InstrumentType instrument) =>
            IsPercussion(instrument) ? DrumParser.CalculateDurationMs(notes) : NoteParser.CalculateDurationMs(notes);

        public static SeekData ComputeSeekData(List<SongCommand> commands, InstrumentType instrument) =>
            IsPercussion(instrument) ? DrumParser.ComputeSeekData(commands) : NoteParser.ComputeSeekData(commands);
    }
}
