using System;
using System.Collections.Generic;
using System.IO;
using Maestro.Models;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Maestro.Services
{
    public static class SongSerializer
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Converters = { new StringEnumConverter() }
        };

        public static List<Song> DeserializeBundle(Stream stream)
        {
            var options = MessagePackSerializerOptions.Standard
                .WithResolver(ContractlessStandardResolver.Instance);

            var rawData = MessagePackSerializer.Deserialize<object[][]>(stream, options);
            var songs = new List<Song>();

            foreach (var songData in rawData)
            {
                var song = new Song
                {
                    Name = songData[0]?.ToString() ?? "",
                    Artist = songData[1]?.ToString() ?? "",
                    Instrument = (InstrumentType)Convert.ToInt32(songData[2]),
                    Bpm = Convert.ToInt32(songData[3])
                };

                var commandsData = songData[4] as object[];
                if (commandsData != null)
                {
                    foreach (var cmdData in commandsData)
                    {
                        var cmdArray = cmdData as object[];
                        if (cmdArray != null && cmdArray.Length >= 3)
                        {
                            song.Commands.Add(new SongCommand
                            {
                                Type = (CommandType)Convert.ToInt32(cmdArray[0]),
                                Key = (Keys)Convert.ToInt32(cmdArray[1]),
                                Duration = Convert.ToInt32(cmdArray[2])
                            });
                        }
                    }
                }

                songs.Add(song);
            }

            return songs;
        }

        public static void SerializeBundle(List<Song> songs, string filePath)
        {
            var dtos = new List<SongBundleDto>();

            foreach (var song in songs)
            {
                var dto = new SongBundleDto
                {
                    Name = song.Name,
                    Artist = song.Artist,
                    Instrument = (int)song.Instrument,
                    Bpm = song.Bpm ?? 0,
                    Commands = new List<CommandBundleDto>()
                };

                foreach (var cmd in song.Commands)
                {
                    dto.Commands.Add(new CommandBundleDto
                    {
                        Type = (int)cmd.Type,
                        Key = (int)cmd.Key,
                        Duration = cmd.Duration
                    });
                }

                dtos.Add(dto);
            }

            var bytes = MessagePackSerializer.Serialize(dtos);
            File.WriteAllBytes(filePath, bytes);
        }

        public static Song DeserializeJson(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var dto = JsonConvert.DeserializeObject<SongJsonDto>(json, JsonSettings);

            Enum.TryParse<InstrumentType>(dto.Instrument, out var instrument);

            var song = new Song
            {
                Name = dto.Name,
                Artist = dto.Artist,
                Instrument = instrument,
                Bpm = dto.Bpm
            };

            foreach (var cmd in dto.Commands)
            {
                song.Commands.Add(new SongCommand
                {
                    Type = cmd.Type,
                    Key = cmd.Key,
                    Duration = cmd.Duration
                });
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

        [MessagePackObject]
        public class SongBundleDto
        {
            [Key(0)]
            public string Name { get; set; }

            [Key(1)]
            public string Artist { get; set; }

            [Key(2)]
            public int Instrument { get; set; }

            [Key(3)]
            public int Bpm { get; set; }

            [Key(4)]
            public List<CommandBundleDto> Commands { get; set; }
        }

        [MessagePackObject]
        public class CommandBundleDto
        {
            [Key(0)]
            public int Type { get; set; }

            [Key(1)]
            public int Key { get; set; }

            [Key(2)]
            public int Duration { get; set; }
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
    }
}
