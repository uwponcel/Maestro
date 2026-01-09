using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Maestro.Services.Data
{
    public static class AhkParser
    {
        private static readonly Dictionary<string, string> NumpadToNote = new Dictionary<string, string>
        {
            { "Numpad1", "C" },
            { "Numpad2", "D" },
            { "Numpad3", "E" },
            { "Numpad4", "F" },
            { "Numpad5", "G" },
            { "Numpad6", "A" },
            { "Numpad7", "B" },
            { "Numpad8", "C^" }
        };

        private static readonly Regex KeyDownPattern = new Regex(
            @"SendInput\s*\{(Numpad\d)\s+down\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex QuickPressPattern = new Regex(
            @"SendInput\s*\{(Numpad[09])\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex SleepPattern = new Regex(
            @"Sleep,\s*(\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static List<string> ParseToCompact(string ahkContent)
        {
            var lines = ahkContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<string>();
            var currentNotes = new List<string>();
            int currentOctave = 0;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Skip function wrapper lines
                if (trimmed.StartsWith("PlaySong", StringComparison.OrdinalIgnoreCase) ||
                    trimmed == "{" || trimmed == "}")
                    continue;

                var keyDownMatch = KeyDownPattern.Match(trimmed);
                if (keyDownMatch.Success)
                {
                    var numpad = keyDownMatch.Groups[1].Value;
                    if (NumpadToNote.TryGetValue(numpad, out var note))
                    {
                        var noteWithOctave = ApplyOctaveModifier(note, currentOctave);
                        currentNotes.Add(noteWithOctave);
                    }
                    continue;
                }

                var quickPressMatch = QuickPressPattern.Match(trimmed);
                if (quickPressMatch.Success)
                {
                    var numpad = quickPressMatch.Groups[1].Value;
                    if (numpad == "Numpad9")
                        currentOctave++;
                    else if (numpad == "Numpad0")
                        currentOctave--;
                    continue;
                }

                var sleepMatch = SleepPattern.Match(trimmed);
                if (sleepMatch.Success && currentNotes.Count > 0)
                {
                    var durationMs = sleepMatch.Groups[1].Value;

                    // Output notes with direct millisecond duration
                    var noteLine = string.Join(" ",
                        currentNotes.ConvertAll(n => $"{n}:{durationMs}"));
                    result.Add(noteLine);
                    currentNotes.Clear();
                }
            }

            return result;
        }

        private static string ApplyOctaveModifier(string note, int octave)
        {
            if (octave == 0)
                return note;

            var modifier = octave > 0 ? "+" : "-";

            if (note == "C^")
                return $"C^{modifier}";

            return $"{note}{modifier}";
        }
    }
}
