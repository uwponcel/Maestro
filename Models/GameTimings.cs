namespace Maestro.Models
{
    /// <summary>
    /// Centralized timing constants for GW2 instrument interactions.
    /// </summary>
    public static class GameTimings
    {
        /// <summary>
        /// Delay after an octave change during playback.
        /// AHK scripts work with no explicit delay, so we keep this minimal.
        /// </summary>
        public const int OctaveChangeDelayMs = 10;

        /// <summary>
        /// Delay after an octave change during reset (before playback starts).
        /// Needs to be longer because we send multiple rapid octave changes with no notes between.
        /// </summary>
        public const int OctaveResetDelayMs = 100;

        /// <summary>
        /// Delay before starting playback to give the player time to prepare.
        /// </summary>
        public const int PlaybackStartDelayMs = 300;
    }
}
