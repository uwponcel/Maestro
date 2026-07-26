using Maestro.Services.Practice;
using Xunit;

namespace Maestro.Tests.Practice
{
    public class PracticeClockTests
    {
        [Fact]
        public void Advance_AppliesSpeedMultiplier()
        {
            var clock = new PracticeClock();
            clock.Speed = 2.0f;
            clock.Advance(100);
            Assert.Equal(200, clock.CurrentMs);
        }

        [Fact]
        public void Pause_StopsAdvancement()
        {
            var clock = new PracticeClock();
            clock.Advance(100);
            clock.Pause();
            clock.Advance(100);
            Assert.Equal(100, clock.CurrentMs);
        }

        [Fact]
        public void Resume_RestartsAdvancement()
        {
            var clock = new PracticeClock();
            clock.Advance(100);
            clock.Pause();
            clock.Resume();
            clock.Advance(50);
            Assert.Equal(150, clock.CurrentMs);
        }

        [Fact]
        public void Seek_JumpsToAbsoluteTime()
        {
            var clock = new PracticeClock();
            clock.Advance(500);
            clock.Seek(200);
            Assert.Equal(200, clock.CurrentMs);
        }

        [Fact]
        public void StartAt_NegativeTime_CountdownPhase()
        {
            var clock = new PracticeClock();
            clock.Seek(-3000);
            clock.Advance(1000);
            Assert.Equal(-2000, clock.CurrentMs);
        }

        [Theory]
        [InlineData(0.0f, 0.1f)]
        [InlineData(5.0f, 2.0f)]
        [InlineData(0.75f, 0.75f)]
        public void Speed_Clamped(float input, float expected)
        {
            var clock = new PracticeClock { Speed = input };
            Assert.Equal(expected, clock.Speed);
        }
    }
}
