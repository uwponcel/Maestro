using System.Collections.Generic;
using Blish_HUD.Input;
using Blish_HUD.Settings;
using Maestro.Services.Playback;
using Microsoft.Xna.Framework.Input;
using Xunit;

namespace Maestro.Tests.Playback
{
    public class KeyboardServiceWasJustSentTests
    {
        private KeyboardService NewService() => new KeyboardService(
            new Dictionary<Keys, SettingEntry<KeyBinding>>(),
            new Dictionary<Keys, SettingEntry<KeyBinding>>());

        [Fact]
        public void WasJustSent_WithoutMark_ReturnsFalse()
        {
            Assert.False(NewService().WasJustSent(Keys.NumPad9));
        }

        [Fact]
        public void WasJustSent_AfterMark_ReturnsTrueOnceThenFalse()
        {
            var s = NewService();
            s.MarkJustSent(Keys.NumPad9);
            Assert.True(s.WasJustSent(Keys.NumPad9));
            Assert.False(s.WasJustSent(Keys.NumPad9));
        }

        [Fact]
        public void WasJustSent_DifferentKey_DoesNotConsumeMark()
        {
            var s = NewService();
            s.MarkJustSent(Keys.NumPad9);
            Assert.False(s.WasJustSent(Keys.NumPad0));
            Assert.True(s.WasJustSent(Keys.NumPad9));
        }
    }
}
