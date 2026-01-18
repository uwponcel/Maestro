using System;
using System.Collections.Generic;
using System.Linq;

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
        public string CommunityId { get; set; }
        public int Downloads { get; set; }

        public bool IsCommunityDownloaded => !string.IsNullOrEmpty(CommunityId);

        public string DisplayDownloads
        {
            get
            {
                if (Downloads <= 0) return null;
                if (Downloads >= 1000000)
                    return $"{Downloads / 1000000.0:F1}M";
                if (Downloads >= 1000)
                    return $"{Downloads / 1000.0:F1}k";
                return Downloads.ToString();
            }
        }

        public string DisplayName => $"{Name} - {Artist}";

        public string DisplayDuration
        {
            get
            {
                var totalMs = Commands.Where(c => c.Type == CommandType.Wait).Sum(c => c.Duration);
                if (totalMs <= 0) return null;
                var span = TimeSpan.FromMilliseconds(totalMs);
                return span.TotalHours >= 1
                    ? span.ToString(@"h\:mm\:ss")
                    : span.ToString(@"m\:ss");
            }
        }
    }
}
