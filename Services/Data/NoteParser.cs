using System;
using System.Collections.Generic;
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

        // Pattern: Note[^|#][+/-]:Duration[.]
        // Examples: C:4, C#:8, D+:16, E-:4, F#-:8., C^:4 (high C = NumPad8)
        private static readonly Regex NotePattern = new Regex(
            @"([A-GR])(\^|#)?([+-])?:(\d+)(\.)?",
            RegexOptions.Compiled);

        public static List<SongCommand> Parse(List<string> noteLines, int bpm)
        {
            var commands = new List<SongCommand>();
            int currentOctave = 0; // 0 = default, 1 = up, -1 = down

            foreach (var line in noteLines)
            {
                var matches = NotePattern.Matches(line);

                foreach (Match match in matches)
                {
                    var note = match.Groups[1].Value;
                    var modifier = match.Groups[2].Success ? match.Groups[2].Value : null;
                    var isSharp = modifier == "#";
                    var isHighC = modifier == "^" && note == "C";
                    var octaveModifier = match.Groups[3].Success ? match.Groups[3].Value : null;
                    var durationValue = int.Parse(match.Groups[4].Value);
                    var isDotted = match.Groups[5].Success;

                    // Calculate duration in ms
                    var durationMs = CalculateDurationMs(bpm, durationValue, isDotted);

                    // Handle rest
                    if (note == "R")
                    {
                        commands.Add(SongCommand.WaitCmd(durationMs));
                        continue;
                    }

                    // Determine target octave
                    int targetOctave = 0;
                    if (octaveModifier == "+") targetOctave = 1;
                    else if (octaveModifier == "-") targetOctave = -1;

                    // Add octave change commands if needed
                    // GW2 octave system is SEQUENTIAL: low (-1) ↔ middle (0) ↔ high (+1)
                    // NumPad9 = one step UP, NumPad0 = one step DOWN
                    // NO shortcuts - must pass through each octave!
                    if (targetOctave != currentOctave)
                    {
                        int steps = targetOctave - currentOctave;
                        Keys octaveKey = steps > 0 ? Keys.NumPad9 : Keys.NumPad0;
                        int pressCount = Math.Abs(steps);

                        for (int i = 0; i < pressCount; i++)
                        {
                            commands.Add(SongCommand.KeyDownCmd(octaveKey));
                            commands.Add(SongCommand.KeyUpCmd(octaveKey));
                        }

                        currentOctave = targetOctave;
                    }

                    // Get the key for this note
                    Keys noteKey;
                    bool needsAlt = false;

                    if (isHighC)
                    {
                        // C^ = High C (NumPad8) - C from the next octave up
                        noteKey = Keys.NumPad8;
                    }
                    else if (isSharp)
                    {
                        var sharpNote = note + "#";
                        if (SharpToKey.TryGetValue(sharpNote, out noteKey))
                        {
                            needsAlt = true;
                        }
                        else
                        {
                            // Invalid sharp note, skip
                            continue;
                        }
                    }
                    else
                    {
                        if (!NoteToKey.TryGetValue(note, out noteKey))
                        {
                            // Invalid note, skip
                            continue;
                        }
                    }

                    // Add key commands
                    if (needsAlt)
                    {
                        commands.Add(SongCommand.KeyDownCmd(Keys.LeftAlt));
                    }

                    commands.Add(SongCommand.KeyDownCmd(noteKey));
                    commands.Add(SongCommand.KeyUpCmd(noteKey));

                    if (needsAlt)
                    {
                        commands.Add(SongCommand.KeyUpCmd(Keys.LeftAlt));
                    }

                    // Add wait for duration
                    commands.Add(SongCommand.WaitCmd(durationMs));
                }
            }

            return commands;
        }

        private static int CalculateDurationMs(int bpm, int noteValue, bool isDotted)
        {
            // ms = (60000 / BPM) * (4 / note_value)
            double ms = (60000.0 / bpm) * (4.0 / noteValue);

            if (isDotted)
            {
                ms *= 1.5;
            }

            return (int)Math.Round(ms);
        }
    }
}
