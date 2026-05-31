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
        BellMagnanimous   // = Magnanimous Choir Bell; C major, 2 octaves (Middle/High)
    }
}
