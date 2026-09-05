using System.Net;
using System.Security.Claims;
using GoHardAPI.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace GoHardAPI.Tests.RateLimiting
{
    /// <summary>
    /// Deterministic unit coverage for the one client-identity resolver every limiter
    /// partition key flows through. No pipeline, no sleeps. The resolver never reads a
    /// forwarding header — identity is the socket peer address or the validated JWT
    /// user id.
    /// </summary>
    public class ClientPartitionKeyResolverTests
    {
        private static readonly ClientPartitionKeyResolver Resolver = new();

        private static HttpContext Context(
            string? peerIp = null,
            (string name, StringValues value)[]? headers = null,
            params Claim[] claims)
        {
            var ctx = new DefaultHttpContext();
            if (peerIp is not null)
            {
                ctx.Connection.RemoteIpAddress = IPAddress.Parse(peerIp);
            }
            if (headers is not null)
            {
                foreach (var (name, value) in headers)
                {
                    ctx.Request.Headers[name] = value;
                }
            }
            if (claims.Length > 0)
            {
                ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            }
            return ctx;
        }

        private static (string name, StringValues value)[] H(string name, params string[] values)
            => new[] { (name, new StringValues(values)) };

        // ---- authenticated global partition -----------------------------------------

        [Fact]
        public void Global_AuthenticatedRequest_PartitionsByUserId_NotByIp()
        {
            var a = Resolver.ResolveGlobalPartitionKey(Context("203.0.113.1", null, new Claim(ClaimTypes.NameIdentifier, "5")));
            var b = Resolver.ResolveGlobalPartitionKey(Context("203.0.113.1", null, new Claim(ClaimTypes.NameIdentifier, "6")));

            Assert.Equal("u:5", a);
            Assert.Equal("u:6", b);   // same IP, different user -> different partition
        }

        [Fact]
        public void Global_SameUser_FromDifferentIps_SharesOnePartition()
        {
            var first = Resolver.ResolveGlobalPartitionKey(Context("203.0.113.1", null, new Claim(ClaimTypes.NameIdentifier, "9")));
            var second = Resolver.ResolveGlobalPartitionKey(Context("198.51.100.7", null, new Claim(ClaimTypes.NameIdentifier, "9")));
            Assert.Equal("u:9", first);
            Assert.Equal(first, second);
        }

        [Theory]
        [InlineData("not-a-number")]
        [InlineData("0")]
        [InlineData("-3")]
        [InlineData("")]
        public void Global_BadIdentityClaim_FallsBackToIpPartition(string claimValue)
        {
            var key = Resolver.ResolveGlobalPartitionKey(Context("203.0.113.4", null, new Claim(ClaimTypes.NameIdentifier, claimValue)));
            Assert.Equal("ip:203.0.113.4", key);
        }

        [Fact]
        public void Global_AnonymousRequest_PartitionsByNormalizedSocketPeer()
        {
            Assert.Equal("ip:203.0.113.4", Resolver.ResolveGlobalPartitionKey(Context("203.0.113.4")));
            Assert.Equal("ip:203.0.113.4", Resolver.ResolveGlobalPartitionKey(Context("::ffff:203.0.113.4")));
        }

        [Fact]
        public void UserKey_CannotBeSetFromBodyQueryOrHeader()
        {
            var ctx = Context("203.0.113.4", H("X-User-Id", "999"), new Claim(ClaimTypes.NameIdentifier, "7"));
            ctx.Request.QueryString = new QueryString("?userId=999");
            ctx.Items["userId"] = 999;
            Assert.Equal("u:7", Resolver.ResolveGlobalPartitionKey(ctx));
        }

        // ---- forwarding headers are inert ------------------------------------------

        [Fact]
        public void ForwardingHeaders_AreNeverRead()
        {
            var ctx = Context("203.0.113.50", new[]
            {
                ("X-Forwarded-For", new StringValues("1.1.1.1")),
                ("X-Real-IP", new StringValues("2.2.2.2")),
                ("X-Railway-Edge", new StringValues("railway/us-west")),
                ("X-Envoy-External-Address", new StringValues("3.3.3.3")),
            });
            Assert.Equal("ip:203.0.113.50", Resolver.ResolveClientIpPartitionKey(ctx));
        }

        [Fact]
        public void ChangingForwardingHeaders_CannotMintNewPartitions()
        {
            var clean = Resolver.ResolveClientIpPartitionKey(Context("203.0.113.50"));
            var spoofA = Resolver.ResolveClientIpPartitionKey(Context("203.0.113.50", H("X-Real-IP", "9.9.9.9")));
            var spoofB = Resolver.ResolveClientIpPartitionKey(Context("203.0.113.50", H("X-Forwarded-For", "8.8.8.8, 7.7.7.7")));
            var spoofC = Resolver.ResolveClientIpPartitionKey(Context("203.0.113.50", H("X-Real-IP", "not-an-ip")));
            Assert.Equal(clean, spoofA);
            Assert.Equal(clean, spoofB);
            Assert.Equal(clean, spoofC);
        }

        // ---- bounded fallback when no address exists at all ------------------------

        [Fact]
        public void NoSocketAddress_UsesBoundedConstantKey()
        {
            Assert.Equal(ClientPartitionKeyResolver.UnknownClientKey,
                Resolver.ResolveClientIpPartitionKey(new DefaultHttpContext()));
            Assert.Equal("ip:unknown", ClientPartitionKeyResolver.UnknownClientKey);
        }

        [Fact]
        public void NoSocketAddress_WithSpoofedHeaders_StillUsesTheOneBoundedKey()
        {
            var a = Resolver.ResolveClientIpPartitionKey(Context(null, H("X-Real-IP", "1.2.3.4")));
            var b = Resolver.ResolveClientIpPartitionKey(Context(null, H("X-Real-IP", "5.6.7.8")));
            Assert.Equal(ClientPartitionKeyResolver.UnknownClientKey, a);
            Assert.Equal(a, b);
        }

        [Fact]
        public void Resolver_IsDeterministic_ForOneInput()
        {
            for (var i = 0; i < 8; i++)
            {
                Assert.Equal("ip:203.0.113.9", Resolver.ResolveClientIpPartitionKey(Context("203.0.113.9")));
            }
        }
    }
}
