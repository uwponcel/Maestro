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

        /// <summary>
        /// Calculates the pure musical duration from compact notes (no octave change overhead).
        /// Each line is a chord/note group; duration is the max of all notes on that line.
        /// </summary>
        public static long CalculateDurationMs(List<string> noteLines)
        {
            long total = 0;
            foreach (var line in noteLines)
            {
                var matches = NotePattern.Matches(line);
                int maxDuration = 0;
                foreach (Match match in matches)
                {
                    var duration = int.Parse(match.Groups[4].Value);
                    if (duration > maxDuration)
                        maxDuration = duration;
                }
                total += maxDuration;
            }
            return total;
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
                        int absSteps = Math.Abs(steps);
                        Keys octaveKey = steps > 0 ? NoteMapping.OctaveUpKey : NoteMapping.OctaveDownKey;

                        // Multi-step octave changes (e.g. high→low = 2 steps) need longer delays
                        // between each step, same as reset, because GW2 can miss rapid consecutive
                        // octave changes and drift into chord territory.
                        var delay = absSteps > 1
                            ? GameTimings.OctaveResetDelayMs
                            : GameTimings.OctaveChangeDelayMs;

                        for (int i = 0; i < absSteps; i++)
                        {
                            commands.Add(SongCommand.KeyDownCmd(octaveKey));
                            commands.Add(SongCommand.KeyUpCmd(octaveKey));
                            commands.Add(SongCommand.WaitCmd(delay));
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
                    noteKey = NoteMapping.HighCKey;
                }
                else if (isSharp)
                {
                    if (!NoteMapping.TryParse(note, out var sharpNoteName))
                        continue;
                    var sharpKey = NoteMapping.GetSharpKey(sharpNoteName);
                    if (!sharpKey.HasValue)
                        continue;
                    noteKey = sharpKey.Value;
                    needsAlt = true;
                }
                else
                {
                    if (!NoteMapping.TryParse(note, out var naturalNoteName))
                        continue;
                    var naturalKey = NoteMapping.GetNaturalKey(naturalNoteName);
                    if (!naturalKey.HasValue)
                        continue;
                    noteKey = naturalKey.Value;
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
