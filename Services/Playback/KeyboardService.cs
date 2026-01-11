using System.Collections.Generic;
using Blish_HUD;
using Blish_HUD.Controls.Extern;
using Blish_HUD.Input;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework.Input;
using BlishKeyboard = Blish_HUD.Controls.Intern.Keyboard;

namespace Maestro.Services.Playback
{
    public class KeyboardService
    {
        private readonly Dictionary<Keys, SettingEntry<KeyBinding>> _keyRemappings;
        private readonly Dictionary<Keys, SettingEntry<KeyBinding>> _sharpRemappings;
        private readonly HashSet<Keys> _activeSharpKeys;
        private readonly DebugLogger _debugLogger = new DebugLogger();
        private bool _altHeld;

        public KeyboardService(
            Dictionary<Keys, SettingEntry<KeyBinding>> keyRemappings,
            Dictionary<Keys, SettingEntry<KeyBinding>> sharpRemappings)
        {
            _keyRemappings = keyRemappings;
            _sharpRemappings = sharpRemappings;
            _activeSharpKeys = new HashSet<Keys>();
        }

        public void StartDebugLog(string songName) => _debugLogger.Start(songName);

        public void StopDebugLog() => _debugLogger.Stop();

        private static bool ShouldSendKeys =>
            GameService.GameIntegration.Gw2Instance.Gw2HasFocus &&
            !GameService.Gw2Mumble.UI.IsTextInputFocused;

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
                _debugLogger.LogNote(key, setting.Value.PrimaryKey);
                BlishKeyboard.Press((VirtualKeyShort)setting.Value.PrimaryKey, true);
            }
            else
            {
                _debugLogger.Log($"UNMAPPED: {key}");
            }
        }

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
                BlishKeyboard.Release((VirtualKeyShort)setting.Value.PrimaryKey, true);
            }
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
