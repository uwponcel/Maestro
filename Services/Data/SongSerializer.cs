using System;
using System.Collections.Generic;
using System.IO;
using Maestro.Models;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Maestro.Services.Data
{
    public static class SongSerializer
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Converters = { new StringEnumConverter() }
        };

        public static Song DeserializeJson(string filePath)
        {
            var json = File.ReadAllText(filePath);

            // Detect format by checking for "notes" property
            var rawObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            if (rawObject != null && rawObject.ContainsKey("notes"))
            {
                return DeserializeCompactFormat(json);
            }

            return DeserializeLegacyFormat(json);
        }

        private static Song DeserializeLegacyFormat(string json)
        {
            var dto = JsonConvert.DeserializeObject<SongJsonDto>(json, JsonSettings);

            Enum.TryParse<InstrumentType>(dto.Instrument, out var instrument);

            var song = new Song
            {
                Name = dto.Name,
                Artist = dto.Artist,
                Instrument = instrument,
                Bpm = dto.Bpm
            };

            if (dto.Commands != null)
            {
                foreach (var cmd in dto.Commands)
                {
                    song.Commands.Add(new SongCommand
                    {
                        Type = cmd.Type,
                        Key = cmd.Key,
                        Duration = cmd.Duration
                    });
                }
            }

            return song;
        }

        private static Song DeserializeCompactFormat(string json)
        {
            var dto = JsonConvert.DeserializeObject<SongCompactJsonDto>(json, JsonSettings);

            Enum.TryParse<InstrumentType>(dto.Instrument, out var instrument);

            var song = new Song
            {
                Name = dto.Name,
                Artist = dto.Artist,
                Instrument = instrument,
                Bpm = dto.Bpm
            };

            if (dto.Notes != null && dto.Bpm.HasValue)
            {
                var commands = NoteParser.Parse(dto.Notes, dto.Bpm.Value);
                song.Commands.AddRange(commands);
            }

            return song;
        }

        public static void SerializeJson(Song song, string filePath)
        {
            var dto = new SongJsonDto
            {
                Name = song.Name,
                Artist = song.Artist,
                Instrument = song.Instrument.ToString(),
                Bpm = song.Bpm,
                Commands = new List<CommandJsonDto>()
            };

            foreach (var cmd in song.Commands)
            {
                dto.Commands.Add(new CommandJsonDto
                {
                    Type = cmd.Type,
                    Key = cmd.Key,
                    Duration = cmd.Duration
                });
            }

            var json = JsonConvert.SerializeObject(dto, JsonSettings);
            File.WriteAllText(filePath, json);
        }

        private class SongJsonDto
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("artist")]
            public string Artist { get; set; }

            [JsonProperty("instrument")]
            public string Instrument { get; set; }

            [JsonProperty("bpm")]
            public int? Bpm { get; set; }

            [JsonProperty("commands")]
            public List<CommandJsonDto> Commands { get; set; }
        }

        private class CommandJsonDto
        {
            [JsonProperty("type")]
            [JsonConverter(typeof(StringEnumConverter))]
            public CommandType Type { get; set; }

            [JsonProperty("key")]
            [JsonConverter(typeof(StringEnumConverter))]
            public Keys Key { get; set; }

            [JsonProperty("duration")]
            public int Duration { get; set; }
        }

        private class SongCompactJsonDto
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("artist")]
            public string Artist { get; set; }

            [JsonProperty("instrument")]
            public string Instrument { get; set; }

            [JsonProperty("bpm")]
            public int? Bpm { get; set; }

            [JsonProperty("notes")]
            public List<string> Notes { get; set; }
        }
    }
}
