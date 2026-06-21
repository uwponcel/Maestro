using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Maestro.Models
{
    /// <summary>
    /// Immutable per-instrument descriptor. The single source of truth for how an
    /// <see cref="InstrumentType"/> behaves: display name, accent colors, whether the
    /// Creator offers sharps, the octave range and labels, and whether it appears in
    /// instrument pickers. Octave-reset behavior is derived from <see cref="MinOctave"/>
    /// (an instrument whose MinOctave is 0 resets to its bottom; others step up to
    /// octave 0 = middle).
    /// </summary>
    public sealed class InstrumentInfo
    {
        public InstrumentType Type { get; }
        public string DisplayName { get; }
        public Color Accent { get; }
        public Color AccentDark { get; }
        public bool SharpsEnabled { get; }
        public int MinOctave { get; }
        public int MaxOctave { get; }

        /// <summary>
        /// True for percussion instruments (Drum Set): no octave, no sharps,
        /// sounds map to fixed keys. Gates the parser and octave-reset logic.
        /// </summary>
        public bool IsPercussion { get; }

        /// <summary>
        /// Label per octave position, indexed from <see cref="MinOctave"/> to
        /// <see cref="MaxOctave"/> inclusive. Length must equal MaxOctave - MinOctave + 1.
        /// </summary>
        public IReadOnlyList<string> OctaveLabels { get; }

        /// <summary>Whether this instrument appears in the create/filter/import pickers.</summary>
        public bool ListedInPickers { get; }

        public InstrumentInfo(
            InstrumentType type,
            string displayName,
            Color accent,
            Color accentDark,
            bool sharpsEnabled,
            int minOctave,
            int maxOctave,
            string[] octaveLabels,
            bool listedInPickers = true,
            bool isPercussion = false)
        {
            Type = type;
            DisplayName = displayName;
            Accent = accent;
            AccentDark = accentDark;
            SharpsEnabled = sharpsEnabled;
            MinOctave = minOctave;
            MaxOctave = maxOctave;
            OctaveLabels = Array.AsReadOnly(octaveLabels);
            ListedInPickers = listedInPickers;
            IsPercussion = isPercussion;
        }
    }
}
