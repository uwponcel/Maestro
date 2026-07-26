using Maestro.Services.Playback;
using Microsoft.Xna.Framework.Input;

namespace Maestro.Services.Practice
{
    public class KeyboardServiceKeySender : IKeySender
    {
        private readonly KeyboardService _keyboard;

        public KeyboardServiceKeySender(KeyboardService keyboard)
        {
            _keyboard = keyboard;
        }

        public void SendOctaveUp()
        {
            _keyboard.MarkJustSent(_keyboard.GetConfiguredPrimaryKey(Keys.NumPad9));
            _keyboard.PlayOctaveChange(up: true);
        }

        public void SendOctaveDown()
        {
            _keyboard.MarkJustSent(_keyboard.GetConfiguredPrimaryKey(Keys.NumPad0));
            _keyboard.PlayOctaveChange(up: false);
        }
    }
}
