using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Maestro.Models;
using Microsoft.Xna.Framework.Input;

namespace Maestro.Services.Data
{
    /// <summary>
    /// Compiles Drum Set note lines into key commands. Percussion model: no octave,
    /// tap+wait one-shots, cymbals via Alt, and auto-alternation of paired keys
    /// (Bass 1/2, Snare 3/4, Ghost 6/7) on consecutive hits.
    /// </summary>
    public static class DrumParser
    {
        // <code>:<ms>. 2-char codes before 1-char; "rd" before "R". Case-insensitive
        // match; the captured code is lowered before lookup for lenient hand-editing.
        private static readonly Regex DrumPattern = new Regex(
            @"(ht|mt|ft|cr|rd|hc|ho|hf|b|s|x|g|R)\s*:\s*(\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private class ParsedHit
        {
            public DrumSoundInfo Info { get; set; }
            public int DurationMs { get; set; }
            public bool IsRest { get; set; }
        }

        /// <summary>Pure musical duration: sum of per-line max durations.</summary>
        public static long CalculateDurationMs(List<string> noteLines)
        {
            long total = 0;
            foreach (var line in noteLines)
            {
                int maxDuration = 0;
                foreach (Match m in DrumPattern.Matches(line))
                {
                    var d = int.Parse(m.Groups[2].Value);
                    if (d > maxDuration) maxDuration = d;
                }
                total += maxDuration;
            }
            return total;
        }

        public static NoteParser.ParseResult ParseWithMapping(List<string> noteLines)
        {
            var commands = new List<SongCommand>();
            var mapping = new List<int>();
            var useSecondary = new Dictionary<DrumSound, bool>();
            int noteLineIndex = 0;

            foreach (var line in noteLines)
            {
                var hits = ParseHitsFromLine(line);

                if (hits.Count == 0)
                {
                    noteLineIndex++;
                    continue;
                }

                if (hits.Any(h => h.IsRest))
                {
                    commands.Add(SongCommand.WaitCmd(hits.Max(h => h.DurationMs)));
                    mapping.Add(noteLineIndex);
                    noteLineIndex++;
                    continue;
                }

                foreach (var hit in hits)
                {
                    var info = hit.Info;
                    var key = info.PrimaryKey;
                    if (info.HasPair)
                    {
                        var second = useSecondary.TryGetValue(info.Sound, out var s) && s;
                        key = second ? info.SecondaryKey : info.PrimaryKey;
                        useSecondary[info.Sound] = !second;
                    }

                    if (info.NeedsAlt)
                    {
                        commands.Add(SongCommand.KeyDownCmd(Keys.LeftAlt));
                        mapping.Add(noteLineIndex);
                    }
                    commands.Add(SongCommand.KeyDownCmd(key));
                    mapping.Add(noteLineIndex);
                    commands.Add(SongCommand.KeyUpCmd(key));
                    mapping.Add(noteLineIndex);
                    if (info.NeedsAlt)
                    {
                        commands.Add(SongCommand.KeyUpCmd(Keys.LeftAlt));
                        mapping.Add(noteLineIndex);
                    }
                }

                commands.Add(SongCommand.WaitCmd(hits.Max(h => h.DurationMs)));
                mapping.Add(noteLineIndex);

                noteLineIndex++;
            }

            return new NoteParser.ParseResult
            {
                Commands = commands,
                CommandToNoteLineIndex = mapping.ToArray()
            };
        }

        public static List<SongCommand> Parse(List<string> noteLines) =>
            ParseWithMapping(noteLines).Commands;

        /// <summary>Seek data for percussion: no octave tracking, every wait counts.</summary>
        public static SeekData ComputeSeekData(List<SongCommand> commands)
        {
            var count = commands.Count;
            var cumulativeTimeMs = new long[count];
            var octaveAtCommand = new int[count];
            long elapsed = 0;

            for (int i = 0; i < count; i++)
            {
                cumulativeTimeMs[i] = elapsed;
                octaveAtCommand[i] = 0;
                if (commands[i].Type == CommandType.Wait)
                    elapsed += commands[i].Duration;
            }

            return new SeekData
            {
                CumulativeTimeMs = cumulativeTimeMs,
                OctaveAtCommand = octaveAtCommand,
                TotalDurationMs = elapsed
            };
        }

        private static List<ParsedHit> ParseHitsFromLine(string line)
        {
            var hits = new List<ParsedHit>();
            foreach (Match m in DrumPattern.Matches(line))
            {
                var code = m.Groups[1].Value;
                var duration = int.Parse(m.Groups[2].Value);

                if (string.Equals(code, "R", StringComparison.OrdinalIgnoreCase))
                {
                    hits.Add(new ParsedHit { IsRest = true, DurationMs = duration });
                    continue;
                }

                if (DrumMapping.TryFromCode(code.ToLowerInvariant(), out var info))
                {
                    hits.Add(new ParsedHit { Info = info, DurationMs = duration });
                }
            }
            return hits;
        }
    }
}
