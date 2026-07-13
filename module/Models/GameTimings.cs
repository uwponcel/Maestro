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

        /// <summary>
        /// Threshold (ms) for detecting potential phantom note risk.
        /// When a note is held longer than this and followed by an octave change,
        /// it is logged as a phantom risk in debug builds.
        /// </summary>
        public const int PhantomNoteThresholdMs = 500;

        /// <summary>
        /// Optional gap (ms) inserted between KeyUp(Note) and KeyDown(OctaveKey) for long notes.
        /// Set to 0 to disable. May help prevent phantom notes at slow tempos.
        /// </summary>
        public const int PostLongNoteGapMs = 0;
    }
}
