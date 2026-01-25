using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Maestro.Services.Data
{
    /// <summary>
    /// Parses AHK v1 scripts (from GW2 Music Box) into Maestro's compact note format.
    /// </summary>
    public static class AhkParser
    {
        private const int InstantNoteDurationMs = 1;

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

        private static readonly Dictionary<string, string> NumpadToSharp = new Dictionary<string, string>
        {
            { "Numpad1", "C#" },
            { "Numpad2", "D#" },
            { "Numpad3", "F#" },
            { "Numpad4", "G#" },
            { "Numpad5", "A#" }
        };

        private static readonly Regex KeyDownPattern = new Regex(
            @"\{(Numpad[1-8])\s+down\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex KeyUpPattern = new Regex(
            @"\{(Numpad[1-8])\s+up\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex AltDownPattern = new Regex(
            @"LAlt\s+down",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex AltUpPattern = new Regex(
            @"LAlt\s+up",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex QuickPressPattern = new Regex(
            @"SendInput\s*\{(Numpad[09])\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex SleepPattern = new Regex(
            @"Sleep,\s*(\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private class PendingNote
        {
            public string Note { get; set; }
            public string NumpadKey { get; set; }
            public bool IsSharp { get; set; }
        }

        /// <summary>
        /// Parses AHK script content into compact note format.
        /// Notes with Sleep get that duration. Notes released before Sleep (grace notes) get 1ms.
        /// </summary>
        public static List<string> ParseToCompact(string ahkContent)
        {
            var lines = ahkContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<string>();
            var heldNotes = new List<PendingNote>();
            var instantNotes = new List<string>();
            int currentOctave = 0;
            bool altHeld = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("PlaySong", StringComparison.OrdinalIgnoreCase) ||
                    trimmed == "{" || trimmed == "}")
                    continue;

                if (AltDownPattern.IsMatch(trimmed))
                    altHeld = true;
                if (AltUpPattern.IsMatch(trimmed))
                    altHeld = false;

                bool hasAltOnLine = AltDownPattern.IsMatch(trimmed);

                var keyDownMatches = KeyDownPattern.Matches(trimmed);
                foreach (Match keyDownMatch in keyDownMatches)
                {
                    var numpad = keyDownMatch.Groups[1].Value;
                    bool isSharp = altHeld || hasAltOnLine;

                    string note;
                    if (isSharp && NumpadToSharp.TryGetValue(numpad, out var sharpNote))
                        note = sharpNote;
                    else if (NumpadToNote.TryGetValue(numpad, out var naturalNote))
                        note = naturalNote;
                    else
                        continue;

                    var noteWithOctave = ApplyOctaveModifier(note, currentOctave);
                    heldNotes.Add(new PendingNote
                    {
                        Note = noteWithOctave,
                        NumpadKey = numpad,
                        IsSharp = isSharp
                    });
                }

                var keyUpMatches = KeyUpPattern.Matches(trimmed);
                foreach (Match keyUpMatch in keyUpMatches)
                {
                    var numpad = keyUpMatch.Groups[1].Value;
                    var matchingNote = heldNotes.Find(n => n.NumpadKey == numpad);
                    if (matchingNote != null)
                    {
                        heldNotes.Remove(matchingNote);
                        instantNotes.Add(matchingNote.Note);
                    }
                }

                var quickPressMatch = QuickPressPattern.Match(trimmed);
                if (quickPressMatch.Success)
                {
                    var numpad = quickPressMatch.Groups[1].Value;
                    if (numpad == "Numpad9")
                        currentOctave++;
                    else if (numpad == "Numpad0")
                        currentOctave--;
                }

                var sleepMatch = SleepPattern.Match(trimmed);
                if (sleepMatch.Success)
                {
                    var durationMs = sleepMatch.Groups[1].Value;

                    foreach (var instantNote in instantNotes)
                        result.Add($"{instantNote}:{InstantNoteDurationMs}");
                    instantNotes.Clear();

                    if (heldNotes.Count > 0)
                    {
                        var noteLine = string.Join(" ", heldNotes.ConvertAll(n => $"{n.Note}:{durationMs}"));
                        result.Add(noteLine);
                        heldNotes.Clear();
                    }
                }
            }

            foreach (var instantNote in instantNotes)
                result.Add($"{instantNote}:{InstantNoteDurationMs}");

            return result;
        }

        private static string ApplyOctaveModifier(string note, int octave)
        {
            if (octave == 0)
                return note;

            var modifier = octave > 0 ? "+" : "-";
            return $"{note}{modifier}";
        }
    }
}
