namespace Maestro.Models
{
    public enum InstrumentType
    {
        Piano,
        Harp,
        Lute,
        Bass,
        Flute,            // C major, no sharps, 2 octaves (Low/Middle)
        Bell,             // = Choir Bell; C major, 3 octaves (legacy value kept)
        BellMagnanimous,  // = Magnanimous Choir Bell; C major, 2 octaves (Middle/High)
        DrumSet           // = Drum Set; percussion, single octave, 12 sounds
    }

    /// <summary>Convenience helpers for <see cref="InstrumentType"/>.</summary>
    public static class InstrumentTypeExtensions
    {
        /// <summary>
        /// Friendly catalog name for UI display (e.g. "Drum Set", "Bell (2 octaves)").
        /// Use this for any user-facing label; never interpolate the enum directly
        /// (its ToString() yields code names like "DrumSet" / "BellMagnanimous").
        /// </summary>
        public static string DisplayName(this InstrumentType type) =>
            InstrumentCatalog.Get(type).DisplayName;
    }
}
