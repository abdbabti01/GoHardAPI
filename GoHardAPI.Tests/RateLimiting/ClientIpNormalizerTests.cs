using System.Net;
using GoHardAPI.RateLimiting;
using Xunit;

namespace GoHardAPI.Tests.RateLimiting
{
    /// <summary>
    /// Narrow unit coverage for socket-peer-address normalization: the two wire forms
    /// of one IPv4 client collapse to one token, IPv6 collapses to its /64 prefix.
    /// No pipeline, no sleeps.
    /// </summary>
    public class ClientIpNormalizerTests
    {
        [Theory]
        [InlineData("203.0.113.7", "203.0.113.7")]
        [InlineData("::ffff:203.0.113.7", "203.0.113.7")]      // IPv4-mapped IPv6 -> IPv4
        [InlineData("::FFFF:203.0.113.7", "203.0.113.7")]
        public void IPv4_And_IPv4MappedIPv6_NormalizeToTheSameToken(string input, string expected)
        {
            Assert.Equal(expected, ClientIpNormalizer.Normalize(IPAddress.Parse(input)));
        }

        [Fact]
        public void IPv4_and_its_mapped_form_share_one_partition_token()
        {
            var plain = ClientIpNormalizer.Normalize(IPAddress.Parse("198.51.100.23"));
            var mapped = ClientIpNormalizer.Normalize(IPAddress.Parse("::ffff:198.51.100.23"));
            Assert.Equal(plain, mapped);
        }

        [Theory]
        [InlineData("2001:db8:abcd:1234::1", "2001:db8:abcd:1234::/64")]
        [InlineData("2001:db8:abcd:1234:5678:9abc:def0:1", "2001:db8:abcd:1234::/64")]
        [InlineData("2001:db8:abcd:1234::9999", "2001:db8:abcd:1234::/64")]
        public void IPv6_CollapsesToItsRoutingPrefix(string input, string expected)
        {
            Assert.Equal(expected, ClientIpNormalizer.Normalize(IPAddress.Parse(input)));
        }

        [Fact]
        public void TwoAddressesInOneIPv6_64_ShareAToken_ButADifferentPrefixDoesNot()
        {
            var a = ClientIpNormalizer.Normalize(IPAddress.Parse("2a00:1450:4009:81f::1"));
            var b = ClientIpNormalizer.Normalize(IPAddress.Parse("2a00:1450:4009:81f::abcd"));
            var other = ClientIpNormalizer.Normalize(IPAddress.Parse("2a00:1450:4009:820::1"));

            Assert.Equal(a, b);
            Assert.NotEqual(a, other);
        }

        [Fact]
        public void IPv6_scope_id_is_dropped()
        {
            var withScope = ClientIpNormalizer.Normalize(IPAddress.Parse("fe80::1%12"));
            var withoutScope = ClientIpNormalizer.Normalize(IPAddress.Parse("fe80::1"));
            Assert.Equal(withoutScope, withScope);
        }
    }
}
