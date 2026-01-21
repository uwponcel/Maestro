namespace Maestro.Models
{
    /// <summary>
    /// Centralized timing constants for GW2 instrument interactions.
    /// These delays ensure the game has time to register key presses.
    /// </summary>
    public static class GameTimings
    {
        /// <summary>
        /// Delay after an octave change key press to let the game register it.
        /// Used everywhere: during playback, reset, and composer operations.
        /// </summary>
        public const int OctaveChangeDelayMs = 100;

        /// <summary>
        /// Delay before starting playback to give the player time to prepare.
        /// </summary>
        public const int PlaybackStartDelayMs = 300;
    }
}
