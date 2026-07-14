using Xunit;

namespace Maestro.Tests
{
    public class SmokeTest
    {
        [Fact]
        public void TestProject_IsWired()
        {
            Assert.Equal(4, 2 + 2);
        }
    }
}
