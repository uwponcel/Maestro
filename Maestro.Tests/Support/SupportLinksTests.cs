using System;
using Maestro.Services;
using Xunit;

namespace Maestro.Tests.Support
{
    public class SupportLinksTests
    {
        [Fact]
        public void KoFiUrl_IsAbsoluteHttpsUrl()
        {
            Assert.True(Uri.TryCreate(SupportLinks.KoFiUrl, UriKind.Absolute, out var uri));
            Assert.Equal(Uri.UriSchemeHttps, uri.Scheme);
            Assert.Equal("ko-fi.com", uri.Host);
        }

        [Fact]
        public void GameAccountName_UsesGw2AccountFormat()
        {
            Assert.Matches(@"^[^.]+\.\d{4}$", SupportLinks.GameAccountName);
        }
    }
}
