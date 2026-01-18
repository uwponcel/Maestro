using System;

namespace Maestro.Models
{
    public class CommunitySong
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Artist { get; set; }
        public string Transcriber { get; set; }
        public string Instrument { get; set; }
        public int NoteCount { get; set; }
        public long DurationMs { get; set; }
        public int Downloads { get; set; }
        public DateTime CreatedAt { get; set; }

        public string DisplayDuration
        {
            get
            {
                var span = TimeSpan.FromMilliseconds(DurationMs);
                return span.TotalHours >= 1
                    ? $"{(int)span.TotalHours}:{span.Minutes:D2}:{span.Seconds:D2}"
                    : $"{span.Minutes}:{span.Seconds:D2}";
            }
        }

        public string DisplayDownloads
        {
            get
            {
                if (Downloads >= 1000000)
                    return $"{Downloads / 1000000.0:F1}M";
                if (Downloads >= 1000)
                    return $"{Downloads / 1000.0:F1}k";
                return Downloads.ToString();
            }
        }

        public InstrumentType InstrumentType
        {
            get
            {
                if (Enum.TryParse<InstrumentType>(Instrument, true, out var type))
                    return type;
                return InstrumentType.Harp;
            }
        }
    }
}
