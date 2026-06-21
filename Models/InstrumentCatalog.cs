using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Maestro.Models
{
    /// <summary>
    /// Central registry of every supported instrument. Add a new instrument by adding a
    /// row here (and a value to <see cref="InstrumentType"/>); all pickers, colors, and
    /// Creator/playback octave logic read from this table.
    /// </summary>
    public static class InstrumentCatalog
    {
        private static readonly string[] ThreeOctaveLabels = { "Lower (-)", "Middle", "Upper (+)" };

        private static readonly IReadOnlyList<InstrumentInfo> _all = new List<InstrumentInfo>
        {
            new InstrumentInfo(InstrumentType.Piano, "Piano",
                new Color(79, 155, 224), new Color(53, 122, 192),   // sapphire
                sharpsEnabled: true, minOctave: -1, maxOctave: 1, octaveLabels: ThreeOctaveLabels),

            new InstrumentInfo(InstrumentType.Harp, "Harp",
                new Color(107, 194, 136), new Color(62, 154, 99),   // emerald
                sharpsEnabled: false, minOctave: -1, maxOctave: 1, octaveLabels: ThreeOctaveLabels),

            new InstrumentInfo(InstrumentType.Lute, "Lute",
                new Color(227, 165, 58), new Color(190, 132, 32),   // amber
                sharpsEnabled: false, minOctave: -1, maxOctave: 1, octaveLabels: ThreeOctaveLabels),

            new InstrumentInfo(InstrumentType.Bass, "Bass",
                new Color(224, 106, 124), new Color(184, 72, 94),   // garnet rose
                sharpsEnabled: false, minOctave: 0, maxOctave: 1,
                octaveLabels: new[] { "Low", "High" }),

            new InstrumentInfo(InstrumentType.Flute, "Flute",
                new Color(165, 121, 224), new Color(126, 84, 190),  // amethyst
                sharpsEnabled: false, minOctave: -1, maxOctave: 0,
                octaveLabels: new[] { "Low", "Middle" }),

            new InstrumentInfo(InstrumentType.Bell, "Bell (3 octaves)",
                new Color(63, 194, 178), new Color(42, 148, 136),   // turquoise
                sharpsEnabled: false, minOctave: -1, maxOctave: 1, octaveLabels: ThreeOctaveLabels),

            new InstrumentInfo(InstrumentType.BellMagnanimous, "Bell (2 octaves)",
                new Color(116, 214, 190), new Color(73, 174, 151),  // mint (bell family)
                sharpsEnabled: false, minOctave: 0, maxOctave: 1,
                octaveLabels: new[] { "Middle", "High" }),

            new InstrumentInfo(InstrumentType.DrumSet, "Drum Set",
                new Color(198, 110, 64), new Color(160, 82, 45),    // copper/bronze
                sharpsEnabled: false, minOctave: 0, maxOctave: 0,
                octaveLabels: new[] { "Kit" },
                listedInPickers: true, isPercussion: true),
        };

        private static readonly Dictionary<InstrumentType, InstrumentInfo> _byType =
            _all.ToDictionary(i => i.Type);

        private static readonly IReadOnlyList<InstrumentInfo> _pickable =
            _all.Where(i => i.ListedInPickers).ToList().AsReadOnly();

        static InstrumentCatalog()
        {
            foreach (InstrumentType type in System.Enum.GetValues(typeof(InstrumentType)))
            {
                if (!_byType.ContainsKey(type))
                {
                    throw new System.InvalidOperationException(
                        $"InstrumentCatalog is missing a row for InstrumentType.{type}");
                }
            }
        }

        /// <summary>All instruments, in enum order.</summary>
        public static IReadOnlyList<InstrumentInfo> All => _all;

        /// <summary>Instruments that appear in pickers, in enum order.</summary>
        public static IReadOnlyList<InstrumentInfo> Pickable => _pickable;

        /// <summary>Descriptor for a type. Every InstrumentType value has a row.</summary>
        public static InstrumentInfo Get(InstrumentType type) => _byType[type];

        /// <summary>Maps a display name (e.g. "Bell (2 octaves)") back to its type.</summary>
        public static bool TryFromDisplayName(string displayName, out InstrumentType type)
        {
            foreach (var info in _all)
            {
                if (info.DisplayName == displayName)
                {
                    type = info.Type;
                    return true;
                }
            }

            type = default(InstrumentType);
            return false;
        }
    }
}
