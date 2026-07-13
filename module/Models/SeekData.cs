namespace Maestro.Models
{
    public class SeekData
    {
        /// <summary>
        /// Cumulative elapsed time (ms) at each command index. Only Wait commands contribute.
        /// </summary>
        public long[] CumulativeTimeMs { get; set; }

        /// <summary>
        /// Octave state (-1, 0, +1) at each command index.
        /// </summary>
        public int[] OctaveAtCommand { get; set; }

        /// <summary>
        /// Total song duration in milliseconds.
        /// </summary>
        public long TotalDurationMs { get; set; }
    }
}
