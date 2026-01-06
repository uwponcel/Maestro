using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Maestro.Models;
using Microsoft.Xna.Framework.Input;

namespace Maestro.Services
{
    public static class SongParser
    {
        private static readonly Dictionary<string, Keys> NumpadKeyMap = new Dictionary<string, Keys>
        {
            { "Numpad0", Keys.NumPad0 },
            { "Numpad1", Keys.NumPad1 },
            { "Numpad2", Keys.NumPad2 },
            { "Numpad3", Keys.NumPad3 },
            { "Numpad4", Keys.NumPad4 },
            { "Numpad5", Keys.NumPad5 },
            { "Numpad6", Keys.NumPad6 },
            { "Numpad7", Keys.NumPad7 },
            { "Numpad8", Keys.NumPad8 },
            { "Numpad9", Keys.NumPad9 }
        };

        public static Song ParseAhkFile(string filePath, InstrumentType instrument)
        {
            var content = File.ReadAllText(filePath);
            return ParseAhkContent(content, Path.GetFileNameWithoutExtension(filePath), instrument);
        }

        public static Song ParseAhkContent(string content, string fileName, InstrumentType instrument)
        {
            var song = new Song { Instrument = instrument };

            ParseMetadata(song, fileName);
            ParseCommands(content, song);

            return song;
        }

        private static void ParseMetadata(Song song, string fileName)
        {
            var parts = fileName.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);

            // Check if last part is BPM (e.g., "Song - Artist - 66")
            if (parts.Length >= 3 && int.TryParse(parts[parts.Length - 1].Trim(), out var bpm))
            {
                song.Bpm = bpm;
                song.Name = parts[0].Trim();
                song.Artist = string.Join(" - ", parts.Skip(1).Take(parts.Length - 2)).Trim();
            }
            else if (parts.Length >= 2)
            {
                song.Name = parts[0].Trim();
                song.Artist = string.Join(" - ", parts.Skip(1)).Trim();
            }
            else
            {
                song.Name = fileName;
                song.Artist = "Unknown";
            }
        }

        private static void ParseCommands(string content, Song song)
        {
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                // Skip comments, empty lines, and function wrappers
                if (string.IsNullOrEmpty(line) || line.StartsWith(";")) continue;
                if (line.Contains("PlaySong()") || line == "{" || line == "}") continue;

                if (line.StartsWith("SendInput"))
                {
                    ParseSendInput(line, song);
                }
                else if (line.StartsWith("Sleep,"))
                {
                    var match = Regex.Match(line, @"Sleep,\s*(\d+)");
                    if (match.Success)
                    {
                        var duration = int.Parse(match.Groups[1].Value);
                        song.Commands.Add(SongCommand.WaitCmd(duration));
                    }
                }
            }
        }

        private static void ParseSendInput(string line, Song song)
        {
            // Handle Alt modifier for sharp notes: {Alt down}{Numpad3}{Alt up}
            if (line.Contains("{Alt down}"))
            {
                var keyMatch = Regex.Match(line, @"\{(Numpad\d)\}");
                if (keyMatch.Success && NumpadKeyMap.TryGetValue(keyMatch.Groups[1].Value, out var altKey))
                {
                    song.Commands.Add(SongCommand.KeyDownCmd(Keys.LeftAlt));
                    song.Commands.Add(SongCommand.KeyDownCmd(altKey));
                    song.Commands.Add(SongCommand.KeyUpCmd(altKey));
                    song.Commands.Add(SongCommand.KeyUpCmd(Keys.LeftAlt));
                }
                return;
            }

            // Match key with optional down/up: {Numpad5}, {Numpad5 down}, {Numpad5 up}
            var keyMatch2 = Regex.Match(line, @"\{(Numpad\d)(?:\s+(down|up))?\}");

            if (!keyMatch2.Success) return;

            var keyName = keyMatch2.Groups[1].Value;
            if (!NumpadKeyMap.TryGetValue(keyName, out var key)) return;

            var modifier = keyMatch2.Groups[2].Value;

            if (string.IsNullOrEmpty(modifier))
            {
                // No down/up = quick tap (press then release)
                song.Commands.Add(SongCommand.KeyDownCmd(key));
                song.Commands.Add(SongCommand.KeyUpCmd(key));
            }
            else if (modifier == "down")
            {
                song.Commands.Add(SongCommand.KeyDownCmd(key));
            }
            else if (modifier == "up")
            {
                song.Commands.Add(SongCommand.KeyUpCmd(key));
            }
        }
    }
}
