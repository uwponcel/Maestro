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

        public static string SerializeToJson(Song song)
        {
            var dto = new SongCompactJsonDto
            {
                Name = song.Name,
                Artist = song.Artist,
                Transcriber = song.Transcriber,
                Instrument = song.Instrument.ToString(),
                Notes = song.Notes
            };

            return JsonConvert.SerializeObject(dto, JsonSettings);
        }

        public static Song DeserializeJsonContent(string json)
        {
            var dto = JsonConvert.DeserializeObject<SongCompactJsonDto>(json, JsonSettings);

            Enum.TryParse<InstrumentType>(dto.Instrument, out var instrument);

            var song = new Song
            {
                Name = dto.Name,
                Artist = dto.Artist,
                Transcriber = dto.Transcriber,
                Instrument = instrument,
                SkipOctaveReset = dto.SkipOctaveReset
            };

            if (dto.Notes != null)
            {
                var commands = NoteParser.Parse(dto.Notes);
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
                    Transcriber = dto.Transcriber,
                    Instrument = instrument,
                    SkipOctaveReset = dto.SkipOctaveReset
                };

                if (dto.Notes != null)
                {
                    var commands = NoteParser.Parse(dto.Notes);
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

            [JsonProperty("transcriber")]
            public string Transcriber { get; set; }

            [JsonProperty("instrument")]
            public string Instrument { get; set; }

            [JsonProperty("notes")]
            public List<string> Notes { get; set; }

            [JsonProperty("skipOctaveReset")]
            public bool SkipOctaveReset { get; set; }
        }
    }
}
