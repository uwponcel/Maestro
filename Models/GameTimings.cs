namespace Maestro.Models
{
    /// <summary>
    /// Centralized timing constants for GW2 instrument interactions.
    /// </summary>
    public static class GameTimings
    {
        /// <summary>
        /// Delay after an octave change during playback.
        /// GW2 can miss rapid octave keypresses, causing drift over long songs.
        /// </summary>
        public const int OctaveChangeDelayMs = 50;

        /// <summary>
        /// Delay after an octave change during reset (before playback starts).
        /// Needs to be longer because we send multiple rapid octave changes with no notes between.
        /// </summary>
        public const int OctaveResetDelayMs = 150;

        /// <summary>
        /// Delay before starting playback to give the player time to prepare.
        /// </summary>
        public const int PlaybackStartDelayMs = 300;
    }
}
