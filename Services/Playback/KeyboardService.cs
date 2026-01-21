using System.Collections.Generic;
using System.Threading;
using Blish_HUD;
using Blish_HUD.Controls.Extern;
using Blish_HUD.Input;
using Blish_HUD.Settings;
using Maestro.Models;
using Maestro.UI.Main;
using Microsoft.Xna.Framework.Input;
using BlishKeyboard = Blish_HUD.Controls.Intern.Keyboard;

namespace Maestro.Services.Playback
{
    /// <summary>
    /// Handles keyboard input for playing notes on GW2 instruments.
    /// Maps note requests to the user's configured keybindings and sends input to the game.
    /// </summary>
    public class KeyboardService
    {
        private readonly Dictionary<Keys, SettingEntry<KeyBinding>> _keyRemappings;
        private readonly Dictionary<Keys, SettingEntry<KeyBinding>> _sharpRemappings;
        private readonly HashSet<Keys> _activeSharpKeys;
        private readonly HashSet<Keys> _heldKeys;
        private readonly DebugLogger _debugLogger = new DebugLogger();
        private bool _altHeld;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyboardService"/> class.
        /// </summary>
        /// <param name="keyRemappings">User keybinding settings for natural notes and octave controls.</param>
        /// <param name="sharpRemappings">User keybinding settings for sharp notes.</param>
        public KeyboardService(
            Dictionary<Keys, SettingEntry<KeyBinding>> keyRemappings,
            Dictionary<Keys, SettingEntry<KeyBinding>> sharpRemappings)
        {
            _keyRemappings = keyRemappings;
            _sharpRemappings = sharpRemappings;
            _activeSharpKeys = new HashSet<Keys>();
            _heldKeys = new HashSet<Keys>();
        }

        /// <summary>
        /// Starts debug logging for song playback analysis.
        /// </summary>
        /// <param name="songName">The name of the song being played.</param>
        public void StartDebugLog(string songName) => _debugLogger.Start(songName);

        /// <summary>
        /// Stops debug logging and writes the log file.
        /// </summary>
        public void StopDebugLog() => _debugLogger.Stop();

        private static bool ShouldSendKeys =>
            GameService.GameIntegration.Gw2Instance.Gw2HasFocus &&
            !GameService.Gw2Mumble.UI.IsTextInputFocused &&
            !SongFilterBar.IsTextInputFocused;

        /// <summary>
        /// Presses a key down using the user's configured keybindings.
        /// </summary>
        /// <param name="key">The internal key to press.</param>
        public void KeyDown(Keys key)
        {
            if (!ShouldSendKeys)
                return;

            if (key == Keys.LeftAlt)
            {
                _altHeld = true;
                _debugLogger.Log("[ALT DOWN]");
                return;
            }

            if (_altHeld && _sharpRemappings.TryGetValue(key, out var sharpSetting))
            {
                _activeSharpKeys.Add(key);
                _debugLogger.LogSharp(key, sharpSetting.Value);
                SendKeyBindingDown(sharpSetting.Value);
                return;
            }

            if (_keyRemappings.TryGetValue(key, out var setting))
            {
                _heldKeys.Add(key);
                _debugLogger.LogNote(key, setting.Value.PrimaryKey);
                BlishKeyboard.Press((VirtualKeyShort)setting.Value.PrimaryKey, true);
            }
            else
            {
                _debugLogger.Log($"UNMAPPED: {key}");
            }
        }

        /// <summary>
        /// Releases a key using the user's configured keybindings.
        /// </summary>
        /// <param name="key">The internal key to release.</param>
        public void KeyUp(Keys key)
        {
            if (key == Keys.LeftAlt)
            {
                _altHeld = false;
                return;
            }

            if (_activeSharpKeys.Remove(key) && _sharpRemappings.TryGetValue(key, out var sharpSetting))
            {
                SendKeyBindingUp(sharpSetting.Value);
                return;
            }

            if (_keyRemappings.TryGetValue(key, out var setting))
            {
                _heldKeys.Remove(key);
                BlishKeyboard.Release((VirtualKeyShort)setting.Value.PrimaryKey, true);
            }
        }

        /// <summary>
        /// Releases all currently held keys. Used when stopping playback or switching context.
        /// </summary>
        public void ReleaseAllKeys()
        {
            foreach (var key in _heldKeys)
            {
                if (_keyRemappings.TryGetValue(key, out var setting))
                {
                    BlishKeyboard.Release((VirtualKeyShort)setting.Value.PrimaryKey, true);
                }
            }
            _heldKeys.Clear();

            foreach (var key in _activeSharpKeys)
            {
                if (_sharpRemappings.TryGetValue(key, out var sharpSetting))
                {
                    SendKeyBindingUp(sharpSetting.Value);
                }
            }
            _activeSharpKeys.Clear();

            _altHeld = false;
        }

        /// <summary>
        /// Plays a single note instantly (press and release).
        /// </summary>
        /// <param name="key">The internal key (NumPad1-8 for notes).</param>
        /// <param name="isSharp">Whether this is a sharp note (requires Alt modifier).</param>
        public void PlayNote(Keys key, bool isSharp = false)
        {
            if (!ShouldSendKeys)
                return;

            if (isSharp && _sharpRemappings.TryGetValue(key, out var sharpSetting))
            {
                SendKeyBindingDown(sharpSetting.Value);
                SendKeyBindingUp(sharpSetting.Value);
            }
            else
            {
                KeyDown(key);
                KeyUp(key);
            }
        }

        /// <summary>
        /// Plays a note by its musical name.
        /// </summary>
        /// <param name="note">The note name (C, D, E, F, G, A, B).</param>
        /// <param name="isSharp">Whether to play the sharp variant.</param>
        /// <param name="isHighC">Whether this is the high C (octave above middle C).</param>
        public void PlayNoteByName(string note, bool isSharp = false, bool isHighC = false)
        {
            if (!ShouldSendKeys)
                return;

            if (isHighC)
            {
                PlayNote(NoteMapping.HighCKey);
                return;
            }

            if (!NoteMapping.TryParse(note, out var noteName))
                return;

            if (isSharp)
            {
                var sharpKey = NoteMapping.GetSharpKey(noteName);
                if (sharpKey.HasValue)
                    PlayNote(sharpKey.Value, true);
            }
            else
            {
                var naturalKey = NoteMapping.GetNaturalKey(noteName);
                if (naturalKey.HasValue)
                    PlayNote(naturalKey.Value);
            }
        }

        /// <summary>
        /// Changes the in-game instrument octave.
        /// </summary>
        /// <param name="up">True to go up one octave, false to go down.</param>
        public void PlayOctaveChange(bool up)
        {
            if (!ShouldSendKeys)
                return;

            var key = up ? NoteMapping.OctaveUpKey : NoteMapping.OctaveDownKey;
            KeyDown(key);
            KeyUp(key);
        }

        /// <summary>
        /// Resets the in-game instrument to middle octave.
        /// Goes to lowest octave then up once to ensure consistent starting position.
        /// </summary>
        public void ResetToMiddleOctave()
        {
            if (!ShouldSendKeys)
                return;

            for (var i = 0; i < 5; i++)
            {
                KeyDown(NoteMapping.OctaveDownKey);
                KeyUp(NoteMapping.OctaveDownKey);
                Thread.Sleep(GameTimings.OctaveChangeDelayMs);
            }

            KeyDown(NoteMapping.OctaveUpKey);
            KeyUp(NoteMapping.OctaveUpKey);
            Thread.Sleep(GameTimings.OctaveChangeDelayMs);
        }

        private static void SendKeyBindingDown(KeyBinding binding)
        {
            if (binding.ModifierKeys.HasFlag(ModifierKeys.Alt))
                BlishKeyboard.Press(VirtualKeyShort.LMENU, true);
            if (binding.ModifierKeys.HasFlag(ModifierKeys.Ctrl))
                BlishKeyboard.Press(VirtualKeyShort.LCONTROL, true);
            if (binding.ModifierKeys.HasFlag(ModifierKeys.Shift))
                BlishKeyboard.Press(VirtualKeyShort.LSHIFT, true);

            BlishKeyboard.Press((VirtualKeyShort)binding.PrimaryKey, true);
        }

        private static void SendKeyBindingUp(KeyBinding binding)
        {
            BlishKeyboard.Release((VirtualKeyShort)binding.PrimaryKey, true);

            if (binding.ModifierKeys.HasFlag(ModifierKeys.Shift))
                BlishKeyboard.Release(VirtualKeyShort.LSHIFT, true);
            if (binding.ModifierKeys.HasFlag(ModifierKeys.Ctrl))
                BlishKeyboard.Release(VirtualKeyShort.LCONTROL, true);
            if (binding.ModifierKeys.HasFlag(ModifierKeys.Alt))
                BlishKeyboard.Release(VirtualKeyShort.LMENU, true);
        }
    }
}
