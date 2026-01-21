using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace Maestro.Models
{
    /// <summary>
    /// Provides centralized mapping between musical notes and keyboard keys for GW2 instruments.
    /// </summary>
    /// <remarks>
    /// GW2 Instrument Key Layout:
    /// - Natural notes: NumPad1-7 (C, D, E, F, G, A, B)
    /// - High C: NumPad8
    /// - Sharp notes: Alt + NumPad1-5 (C#, D#, F#, G#, A#) - note: no E# or B#
    /// - Octave down: NumPad0
    /// - Octave up: NumPad9
    /// </remarks>
    public static class NoteMapping
    {
        private static readonly Dictionary<NoteName, Keys> NaturalNoteKeys = new Dictionary<NoteName, Keys>
        {
            { NoteName.C, Keys.NumPad1 },
            { NoteName.D, Keys.NumPad2 },
            { NoteName.E, Keys.NumPad3 },
            { NoteName.F, Keys.NumPad4 },
            { NoteName.G, Keys.NumPad5 },
            { NoteName.A, Keys.NumPad6 },
            { NoteName.B, Keys.NumPad7 }
        };

        private static readonly Dictionary<NoteName, Keys> SharpNoteKeys = new Dictionary<NoteName, Keys>
        {
            { NoteName.C, Keys.NumPad1 },
            { NoteName.D, Keys.NumPad2 },
            { NoteName.F, Keys.NumPad3 },
            { NoteName.G, Keys.NumPad4 },
            { NoteName.A, Keys.NumPad5 }
        };

        /// <summary>
        /// The keyboard key for high C (octave above middle C).
        /// </summary>
        public const Keys HighCKey = Keys.NumPad8;

        /// <summary>
        /// The keyboard key to shift up one octave.
        /// </summary>
        public const Keys OctaveUpKey = Keys.NumPad9;

        /// <summary>
        /// The keyboard key to shift down one octave.
        /// </summary>
        public const Keys OctaveDownKey = Keys.NumPad0;

        /// <summary>
        /// Gets the keyboard key for a natural note.
        /// </summary>
        /// <param name="note">The note name.</param>
        /// <returns>The corresponding keyboard key, or null if not found.</returns>
        public static Keys? GetNaturalKey(NoteName note)
        {
            return NaturalNoteKeys.TryGetValue(note, out var key) ? key : (Keys?)null;
        }

        /// <summary>
        /// Gets the keyboard key for a sharp note.
        /// </summary>
        /// <param name="note">The note name.</param>
        /// <returns>The corresponding keyboard key, or null for notes without sharps (E, B).</returns>
        public static Keys? GetSharpKey(NoteName note)
        {
            return SharpNoteKeys.TryGetValue(note, out var key) ? key : (Keys?)null;
        }

        /// <summary>
        /// Tries to parse a note name string to NoteName enum.
        /// </summary>
        /// <param name="note">The note string (e.g., "C", "D#", "Ab").</param>
        /// <param name="result">The parsed NoteName if successful.</param>
        /// <returns>True if parsing succeeded, false otherwise.</returns>
        public static bool TryParse(string note, out NoteName result)
        {
            result = default;
            if (string.IsNullOrEmpty(note))
                return false;

            return Enum.TryParse(note.Substring(0, 1), true, out result);
        }
    }
}
