using System.Collections.Generic;
using Blish_HUD;
using Blish_HUD.Controls.Extern;
using Microsoft.Xna.Framework.Input;
using BlishKeyboard = Blish_HUD.Controls.Intern.Keyboard;

namespace Maestro.Services
{
    public class KeyboardService
    {
        private static readonly Logger Logger = Logger.GetLogger<KeyboardService>();

        private static readonly Dictionary<Keys, VirtualKeyShort> KeyMapping = new Dictionary<Keys, VirtualKeyShort>
        {
            { Keys.NumPad0, VirtualKeyShort.NUMPAD0 },
            { Keys.NumPad1, VirtualKeyShort.NUMPAD1 },
            { Keys.NumPad2, VirtualKeyShort.NUMPAD2 },
            { Keys.NumPad3, VirtualKeyShort.NUMPAD3 },
            { Keys.NumPad4, VirtualKeyShort.NUMPAD4 },
            { Keys.NumPad5, VirtualKeyShort.NUMPAD5 },
            { Keys.NumPad6, VirtualKeyShort.NUMPAD6 },
            { Keys.NumPad7, VirtualKeyShort.NUMPAD7 },
            { Keys.NumPad8, VirtualKeyShort.NUMPAD8 },
            { Keys.NumPad9, VirtualKeyShort.NUMPAD9 }
        };

        public void KeyDown(Keys key)
        {
            if (!KeyMapping.TryGetValue(key, out var virtualKey))
            {
                Logger.Warn($"Unknown key: {key}");
                return;
            }

            BlishKeyboard.Press(virtualKey, true);
        }

        public void KeyUp(Keys key)
        {
            if (!KeyMapping.TryGetValue(key, out var virtualKey))
            {
                Logger.Warn($"Unknown key: {key}");
                return;
            }

            BlishKeyboard.Release(virtualKey, true);
        }
    }
}
