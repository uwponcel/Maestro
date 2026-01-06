// Simple C# script to bundle songs into MessagePack format
using System;
using System.Collections.Generic;
using System.IO;
using MessagePack;
using MessagePack.Resolvers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class BuildSongs
{
    static Dictionary<string, int> InstrumentMap = new Dictionary<string, int>
    {
        { "Piano", 0 }, { "Harp", 1 }, { "Lute", 2 }, { "Bass", 3 }, { "Bell", 4 }
    };

    static Dictionary<string, int> CommandTypeMap = new Dictionary<string, int>
    {
        { "KeyDown", 0 }, { "KeyUp", 1 }, { "Wait", 2 }
    };

    static Dictionary<string, int> KeysMap = new Dictionary<string, int>
    {
        { "None", 0 }, { "NumPad0", 96 }, { "NumPad1", 97 }, { "NumPad2", 98 },
        { "NumPad3", 99 }, { "NumPad4", 100 }, { "NumPad5", 101 }, { "NumPad6", 102 },
        { "NumPad7", 103 }, { "NumPad8", 104 }, { "NumPad9", 105 },
        { "LeftAlt", 164 }, { "RightAlt", 165 }
    };

    static int GetValueOrDefault(Dictionary<string, int> dict, string key, int defaultValue)
    {
        int value;
        return dict.TryGetValue(key ?? "", out value) ? value : defaultValue;
    }

    static void Main(string[] args)
    {
        var songsPath = args.Length > 0 ? args[0] : @"..\Songs";
        var outputPath = args.Length > 1 ? args[1] : @"..\songs.bin";

        var jsonFiles = Directory.GetFiles(songsPath, "*.json");
        Console.WriteLine("Found " + jsonFiles.Length + " JSON files in " + songsPath);

        // Build songs as array of arrays (positional format)
        var songs = new List<object[]>();

        foreach (var file in jsonFiles)
        {
            Console.WriteLine("Processing: " + Path.GetFileName(file));

            var json = File.ReadAllText(file);
            var data = JObject.Parse(json);

            var bpmToken = data["bpm"];
            int bpm = 0;
            if (bpmToken != null && bpmToken.Type != JTokenType.Null)
            {
                int.TryParse(bpmToken.ToString(), out bpm);
            }

            var commands = new List<int[]>();
            foreach (var cmd in data["commands"])
            {
                commands.Add(new int[] {
                    GetValueOrDefault(CommandTypeMap, cmd["type"] != null ? cmd["type"].ToString() : "Wait", 2),
                    GetValueOrDefault(KeysMap, cmd["key"] != null ? cmd["key"].ToString() : "None", 0),
                    cmd["duration"] != null ? (int)cmd["duration"] : 0
                });
            }

            songs.Add(new object[] {
                data["name"] != null ? data["name"].ToString() : "",
                data["artist"] != null ? data["artist"].ToString() : "",
                GetValueOrDefault(InstrumentMap, data["instrument"] != null ? data["instrument"].ToString() : "Piano", 0),
                bpm,
                commands.ToArray()
            });
        }

        Console.WriteLine("");
        Console.WriteLine("Serializing " + songs.Count + " songs to MessagePack...");

        var options = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
        var bytes = MessagePackSerializer.Serialize(songs.ToArray(), options);
        File.WriteAllBytes(outputPath, bytes);

        Console.WriteLine("Created " + outputPath + " (" + (bytes.Length / 1024.0).ToString("F1") + " KB)");
        Console.WriteLine("Done!");
    }
}
