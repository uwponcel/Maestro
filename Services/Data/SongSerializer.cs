using System;
using System.Collections.Generic;
using System.IO;
using Maestro.Models;
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
            return DeserializeJsonContent(json);
        }

        public static Song DeserializeJsonContent(string json)
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

        public static List<Song> DeserializeJsonArray(string json)
        {
            var songs = new List<Song>();
            var dtos = JsonConvert.DeserializeObject<List<SongCompactJsonDto>>(json, JsonSettings);

            if (dtos == null) return songs;

            foreach (var dto in dtos)
            {
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

                songs.Add(song);
            }

            return songs;
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
