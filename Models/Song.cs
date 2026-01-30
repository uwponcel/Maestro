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
        public bool IsCreated { get; set; }
        public bool SkipOctaveReset { get; set; }
        public string CommunityId { get; set; }
        public bool IsUploaded { get; set; }
        public bool IsSubmittal { get; set; }

        public bool IsCommunityDownloaded => !string.IsNullOrEmpty(CommunityId) && !IsUserImported && !IsCreated;

        public string DisplayName => $"{Name} - {Artist}";

        public long DurationMs => Commands.Where(c => c.Type == CommandType.Wait).Sum(c => c.Duration);

        public string DisplayDuration
        {
            get
            {
                if (DurationMs <= 0) return null;
                var span = TimeSpan.FromMilliseconds(DurationMs);
                return span.TotalHours >= 1
                    ? span.ToString(@"h\:mm\:ss")
                    : span.ToString(@"m\:ss");
            }
        }
    }
}
