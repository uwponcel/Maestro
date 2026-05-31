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
                new Color(126, 200, 227), new Color(90, 176, 208),
                sharpsEnabled: true, minOctave: -1, maxOctave: 1, octaveLabels: ThreeOctaveLabels),

            new InstrumentInfo(InstrumentType.Harp, "Harp",
                new Color(184, 212, 168), new Color(140, 196, 144),
                sharpsEnabled: false, minOctave: -1, maxOctave: 1, octaveLabels: ThreeOctaveLabels),

            new InstrumentInfo(InstrumentType.Lute, "Lute",
                new Color(232, 193, 112), new Color(212, 166, 86),
                sharpsEnabled: false, minOctave: -1, maxOctave: 1, octaveLabels: ThreeOctaveLabels),

            new InstrumentInfo(InstrumentType.Bass, "Bass",
                new Color(212, 132, 140), new Color(192, 112, 120),
                sharpsEnabled: false, minOctave: 0, maxOctave: 1,
                octaveLabels: new[] { "Low", "High" }),

            new InstrumentInfo(InstrumentType.Flute, "Flute",
                new Color(175, 160, 220), new Color(135, 118, 190),
                sharpsEnabled: false, minOctave: -1, maxOctave: 0,
                octaveLabels: new[] { "Low", "Middle" }),

            new InstrumentInfo(InstrumentType.Bell, "Bell (3 octaves)",
                new Color(150, 196, 190), new Color(108, 156, 150),
                sharpsEnabled: false, minOctave: -1, maxOctave: 1, octaveLabels: ThreeOctaveLabels),

            new InstrumentInfo(InstrumentType.BellMagnanimous, "Bell (2 octaves)",
                new Color(176, 208, 200), new Color(130, 176, 168),
                sharpsEnabled: false, minOctave: 0, maxOctave: 1,
                octaveLabels: new[] { "Middle", "High" }),
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
