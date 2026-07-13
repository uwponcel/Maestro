using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace Maestro.Models
{
    /// <summary>
    /// Visual/semantic family of a drum sound. Single source of truth for pad and
    /// note-chip coloring (see <see cref="MaestroTheme"/>.GetDrumGroupColor).
    /// </summary>
    public enum DrumGroup
    {
        Drums,
        Toms,
        Cymbals
    }

    /// <summary>
    /// Immutable descriptor for one Drum Set sound: its short code, display name,
    /// whether it needs Alt (cymbals), and the GW2 key(s) Maestro sends. Sounds
    /// with two keys alternate on consecutive hits so fast rolls don't stutter on
    /// GW2 skill recharge.
    /// </summary>
    public sealed class DrumSoundInfo
    {
        public DrumSound Sound { get; }
        public string Code { get; }
        public string DisplayName { get; }
        public bool NeedsAlt { get; }
        public Keys PrimaryKey { get; }
        public Keys SecondaryKey { get; }

        /// <summary>True when this sound has a distinct second key to alternate to.</summary>
        public bool HasPair => SecondaryKey != PrimaryKey;

        /// <summary>Semantic family: Cymbals (Alt sounds), Toms, or Drums. Drives coloring.</summary>
        public DrumGroup Group =>
            NeedsAlt ? DrumGroup.Cymbals
            : (Sound == DrumSound.HighTom || Sound == DrumSound.MidTom || Sound == DrumSound.FloorTom)
                ? DrumGroup.Toms
                : DrumGroup.Drums;

        public DrumSoundInfo(DrumSound sound, string code, string displayName,
            bool needsAlt, Keys primaryKey, Keys? secondaryKey = null)
        {
            Sound = sound;
            Code = code;
            DisplayName = displayName;
            NeedsAlt = needsAlt;
            PrimaryKey = primaryKey;
            SecondaryKey = secondaryKey ?? primaryKey;
        }
    }

    /// <summary>
    /// Registry of every Drum Set sound. Cymbals reuse the "sharp" key mechanism
    /// (Alt + NumPad1-5); toms reuse NumPad8/9/0. Code lookup is case-sensitive; all codes are canonical lowercase.
    /// </summary>
    public static class DrumMapping
    {
        private static readonly IReadOnlyList<DrumSoundInfo> _all = new List<DrumSoundInfo>
        {
            new DrumSoundInfo(DrumSound.Bass,       "b",  "Bass",       false, Keys.NumPad1, Keys.NumPad2),
            new DrumSoundInfo(DrumSound.Snare,      "s",  "Snare",      false, Keys.NumPad3, Keys.NumPad4),
            new DrumSoundInfo(DrumSound.CrossStick, "x",  "Cross Stick", false, Keys.NumPad5),
            new DrumSoundInfo(DrumSound.Ghost,      "g",  "Ghost",      false, Keys.NumPad6, Keys.NumPad7),
            new DrumSoundInfo(DrumSound.HighTom,    "ht", "High Tom",   false, Keys.NumPad8),
            new DrumSoundInfo(DrumSound.MidTom,     "mt", "Mid Tom",    false, Keys.NumPad9),
            new DrumSoundInfo(DrumSound.FloorTom,   "ft", "Floor Tom",  false, Keys.NumPad0),
            new DrumSoundInfo(DrumSound.Crash,      "cr", "Crash",      true,  Keys.NumPad1),
            new DrumSoundInfo(DrumSound.Ride,       "rd", "Ride",       true,  Keys.NumPad2),
            new DrumSoundInfo(DrumSound.HatClosed,  "hc", "Hat Closed", true,  Keys.NumPad3),
            new DrumSoundInfo(DrumSound.HatOpen,    "ho", "Hat Open",   true,  Keys.NumPad4),
            new DrumSoundInfo(DrumSound.HatFoot,    "hf", "Hat Foot",   true,  Keys.NumPad5),
        };

        private static readonly Dictionary<DrumSound, DrumSoundInfo> _bySound =
            _all.ToDictionary(i => i.Sound);

        private static readonly Dictionary<string, DrumSoundInfo> _byCode =
            _all.ToDictionary(i => i.Code, StringComparer.Ordinal);

        static DrumMapping()
        {
            foreach (DrumSound sound in Enum.GetValues(typeof(DrumSound)))
            {
                if (!_bySound.ContainsKey(sound))
                {
                    throw new InvalidOperationException(
                        $"DrumMapping is missing a row for DrumSound.{sound}");
                }
            }
        }

        /// <summary>All sounds, in enum order.</summary>
        public static IReadOnlyList<DrumSoundInfo> All => _all;

        /// <summary>Descriptor for a sound.</summary>
        public static DrumSoundInfo Get(DrumSound sound) => _bySound[sound];

        /// <summary>Maps a short code (case-sensitive, canonical lowercase) to its sound.</summary>
        public static bool TryFromCode(string code, out DrumSoundInfo info) =>
            _byCode.TryGetValue(code, out info);
    }
}
