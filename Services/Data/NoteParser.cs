using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Maestro.Models;
using Microsoft.Xna.Framework.Input;

namespace Maestro.Services.Data
{
    public static class NoteParser
    {
        private static readonly Dictionary<string, Keys> NoteToKey = new Dictionary<string, Keys>
        {
            { "C", Keys.NumPad1 },
            { "D", Keys.NumPad2 },
            { "E", Keys.NumPad3 },
            { "F", Keys.NumPad4 },
            { "G", Keys.NumPad5 },
            { "A", Keys.NumPad6 },
            { "B", Keys.NumPad7 }
        };

        private static readonly Dictionary<string, Keys> SharpToKey = new Dictionary<string, Keys>
        {
            { "C#", Keys.NumPad1 },
            { "D#", Keys.NumPad2 },
            { "F#", Keys.NumPad3 },
            { "G#", Keys.NumPad4 },
            { "A#", Keys.NumPad5 }
        };

        // Pattern: Note[^|#][+/-]:Duration (duration is milliseconds)
        // Examples: C:75, C#:150, D+:300, E-:500, C^:75 (high C = NumPad8)
        private static readonly Regex NotePattern = new Regex(
            @"([A-GR])(\^|#)?([+-])?:(\d+)",
            RegexOptions.Compiled);

        private class ParsedNote
        {
            public Keys Key { get; set; }
            public int TargetOctave { get; set; }
            public int DurationMs { get; set; }
            public bool NeedsAlt { get; set; }
            public bool IsRest { get; set; }
        }

        public static List<SongCommand> Parse(List<string> noteLines)
        {
            var commands = new List<SongCommand>();
            int currentOctave = 0;

            foreach (var line in noteLines)
            {
                // Parse all notes in line - all notes on same line form ONE chord
                var notes = ParseNotesFromLine(line);

                if (notes.Count == 0)
                    continue;

                // Handle rest
                if (notes.Any(n => n.IsRest))
                {
                    commands.Add(SongCommand.WaitCmd(notes.Max(n => n.DurationMs)));
                    continue;
                }

                // Press all keys DOWN, handling octave changes inline (like AHK does)
                // This keeps all notes pressed together even across octave changes
                foreach (var note in notes)
                {
                    // Change octave if needed for this note
                    if (note.TargetOctave != currentOctave)
                    {
                        int steps = note.TargetOctave - currentOctave;
                        Keys octaveKey = steps > 0 ? Keys.NumPad9 : Keys.NumPad0;
                        for (int i = 0; i < Math.Abs(steps); i++)
                        {
                            commands.Add(SongCommand.KeyDownCmd(octaveKey));
                            commands.Add(SongCommand.KeyUpCmd(octaveKey));
                        }
                        currentOctave = note.TargetOctave;
                    }

                    if (note.NeedsAlt)
                        commands.Add(SongCommand.KeyDownCmd(Keys.LeftAlt));
                    commands.Add(SongCommand.KeyDownCmd(note.Key));
                }

                // Wait for chord duration (use max duration)
                commands.Add(SongCommand.WaitCmd(notes.Max(n => n.DurationMs)));

                // Release all keys UP (reverse order for clean release)
                foreach (var note in notes.AsEnumerable().Reverse())
                {
                    commands.Add(SongCommand.KeyUpCmd(note.Key));
                    if (note.NeedsAlt)
                        commands.Add(SongCommand.KeyUpCmd(Keys.LeftAlt));
                }
            }

            return commands;
        }

        private static List<ParsedNote> ParseNotesFromLine(string line)
        {
            var notes = new List<ParsedNote>();
            var matches = NotePattern.Matches(line);

            foreach (Match match in matches)
            {
                var note = match.Groups[1].Value;
                var modifier = match.Groups[2].Success ? match.Groups[2].Value : null;
                var isSharp = modifier == "#";
                var isHighC = modifier == "^" && note == "C";
                var octaveModifier = match.Groups[3].Success ? match.Groups[3].Value : null;
                var durationMs = int.Parse(match.Groups[4].Value);

                int targetOctave = octaveModifier == "+" ? 1 : (octaveModifier == "-" ? -1 : 0);

                if (note == "R")
                {
                    notes.Add(new ParsedNote { IsRest = true, TargetOctave = targetOctave, DurationMs = durationMs });
                    continue;
                }

                Keys noteKey;
                bool needsAlt = false;

                if (isHighC)
                {
                    noteKey = Keys.NumPad8;
                }
                else if (isSharp)
                {
                    if (!SharpToKey.TryGetValue(note + "#", out noteKey))
                        continue;
                    needsAlt = true;
                }
                else
                {
                    if (!NoteToKey.TryGetValue(note, out noteKey))
                        continue;
                }

                notes.Add(new ParsedNote
                {
                    Key = noteKey,
                    TargetOctave = targetOctave,
                    DurationMs = durationMs,
                    NeedsAlt = needsAlt
                });
            }

            return notes;
        }
    }
}
