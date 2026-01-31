using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Maestro.Services.Data
{
    /// <summary>
    /// Parses AHK v1 scripts (from GW2 Music Box and PianoTomas) into Maestro's compact note format.
    /// </summary>
    public static class AhkParser
    {
        private const int InstantNoteDurationMs = 1;
        private const int PianoTomasMaxPressDurationMs = 150;

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

        private static readonly Dictionary<string, string> FKeyToSharp = new Dictionary<string, string>
        {
            { "F1", "C#" },
            { "F2", "D#" },
            { "F3", "F#" },
            { "F4", "G#" },
            { "F5", "A#" }
        };

        private static readonly Regex KeyDownPattern = new Regex(
            @"\{(Numpad[1-8]|F[1-5])\s+down\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex KeyUpPattern = new Regex(
            @"\{(Numpad[1-8]|F[1-5])\s+up\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex QuickPressPattern = new Regex(
            @"SendInput\s*\{(Numpad[09])\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex SleepPattern = new Regex(
            @"Sleep,\s*(\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex SendInputKeyDownPattern = new Regex(
            @"SendInput.*down",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex SendInputKeyUpPattern = new Regex(
            @"^SendInput.*up\}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private class PendingNote
        {
            public string Note { get; set; }
            public string Key { get; set; }
        }

        /// <summary>
        /// Pre-processes AHK script lines to normalize PianoTomas timing into MusicBox format.
        /// Converts PianoTomas timing (key down → short sleep → key up → long sleep)
        /// into MusicBox timing (key down → combined sleep → key up).
        /// MusicBox-format scripts pass through unchanged since they never have the short-sleep pattern.
        /// </summary>
        private static string[] NormalizePianoTomas(string[] lines)
        {
            var output = new List<string>();
            var i = 0;

            while (i < lines.Length)
            {
                var trimmed = lines[i].Trim();

                // Pass through non-command lines (hotkey definitions, braces, empty-ish)
                if (trimmed.StartsWith("PlaySong", StringComparison.OrdinalIgnoreCase) ||
                    trimmed == "{" || trimmed == "}" || trimmed == string.Empty ||
                    (trimmed.Length >= 2 && trimmed[1] == ':' && trimmed[0] != 'S') ||
                    trimmed.StartsWith("'::"))
                {
                    output.Add(lines[i]);
                    i++;
                    continue;
                }

                // Check if this is a key-down line
                if (SendInputKeyDownPattern.IsMatch(trimmed))
                {
                    // Collect ALL consecutive key down lines (handles chords)
                    var keyDownLines = new List<string>();
                    while (i < lines.Length && SendInputKeyDownPattern.IsMatch(lines[i].Trim()))
                    {
                        keyDownLines.Add(lines[i]);
                        i++;
                    }

                    // Next should be short Sleep (press duration ≤150ms) for PianoTomas pattern
                    if (i < lines.Length)
                    {
                        var sleepMatch = SleepPattern.Match(lines[i].Trim());
                        if (sleepMatch.Success && int.Parse(sleepMatch.Groups[1].Value) <= PianoTomasMaxPressDurationMs)
                        {
                            var pressDuration = int.Parse(sleepMatch.Groups[1].Value);
                            i++;

                            // Collect ALL consecutive key up lines
                            var keyUpLines = new List<string>();
                            while (i < lines.Length && SendInputKeyUpPattern.IsMatch(lines[i].Trim()))
                            {
                                keyUpLines.Add(lines[i]);
                                i++;
                            }

                            // Next should be real duration Sleep
                            if (i < lines.Length)
                            {
                                var gapMatch = SleepPattern.Match(lines[i].Trim());
                                if (gapMatch.Success)
                                {
                                    var totalDuration = pressDuration + int.Parse(gapMatch.Groups[1].Value);
                                    i++;

                                    // Skip trailing duplicate key ups AND accumulate additional sleeps
                                    while (i < lines.Length)
                                    {
                                        var nextTrimmed = lines[i].Trim();
                                        if (SendInputKeyUpPattern.IsMatch(nextTrimmed))
                                        {
                                            i++;
                                        }
                                        else
                                        {
                                            var extraSleep = SleepPattern.Match(nextTrimmed);
                                            if (extraSleep.Success)
                                            {
                                                totalDuration += int.Parse(extraSleep.Groups[1].Value);
                                                i++;
                                            }
                                            else
                                            {
                                                break;
                                            }
                                        }
                                    }

                                    // Output MusicBox format: key downs → total duration → key ups
                                    output.AddRange(keyDownLines);
                                    output.Add($"Sleep, {totalDuration}");
                                    output.AddRange(keyUpLines);
                                    continue;
                                }
                            }

                            // Pattern didn't fully match (no gap sleep) - output what we have
                            output.AddRange(keyDownLines);
                            output.Add($"Sleep, {pressDuration}");
                            output.AddRange(keyUpLines);
                            continue;
                        }
                    }

                    // Not PianoTomas pattern (no short sleep after key downs) - pass through as-is
                    output.AddRange(keyDownLines);
                    continue;
                }

                // All other lines: pass through
                output.Add(lines[i]);
                i++;
            }

            return output.ToArray();
        }

        /// <summary>
        /// Parses AHK script content into compact note format.
        /// Notes with Sleep get that duration. Notes released before Sleep (grace notes) get 1ms.
        /// Automatically detects and normalizes PianoTomas format scripts.
        /// </summary>
        public static List<string> ParseToCompact(string ahkContent)
        {
            var rawLines = ahkContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var lines = NormalizePianoTomas(rawLines);
            var result = new List<string>();
            var heldNotes = new List<PendingNote>();
            var instantNotes = new List<string>();
            int currentOctave = 0;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("PlaySong", StringComparison.OrdinalIgnoreCase) ||
                    trimmed == "{" || trimmed == "}")
                    continue;

                var keyDownMatches = KeyDownPattern.Matches(trimmed);
                foreach (Match keyDownMatch in keyDownMatches)
                {
                    var key = keyDownMatch.Groups[1].Value;

                    string note;
                    if (FKeyToSharp.TryGetValue(key, out var sharpNote))
                        note = sharpNote;
                    else if (NumpadToNote.TryGetValue(key, out var naturalNote))
                        note = naturalNote;
                    else
                        continue;

                    var noteWithOctave = ApplyOctaveModifier(note, currentOctave);
                    heldNotes.Add(new PendingNote
                    {
                        Note = noteWithOctave,
                        Key = key
                    });
                }

                var keyUpMatches = KeyUpPattern.Matches(trimmed);
                foreach (Match keyUpMatch in keyUpMatches)
                {
                    var key = keyUpMatch.Groups[1].Value;
                    var matchingNote = heldNotes.Find(n => n.Key == key);
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
                        currentOctave = Math.Min(currentOctave + 1, 1);
                    else if (numpad == "Numpad0")
                        currentOctave = Math.Max(currentOctave - 1, -1);
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
