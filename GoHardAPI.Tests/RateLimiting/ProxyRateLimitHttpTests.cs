using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using GoHardAPI.Tests.Infrastructure;
using Xunit;

namespace GoHardAPI.Tests.RateLimiting
{
    /// <summary>
    /// End-to-end HTTP coverage of the app-wide <c>GlobalLimiter</c> (separate authed /
    /// anonymous aggregate ceilings) and the <c>UseAuthentication → UseRateLimiter →
    /// UseAuthorization</c> order, through the real <c>Program.cs</c> pipeline. Tiny
    /// deterministic windows — no sleeps. Forwarding headers are sent but never trusted.
    /// (Per-credential-identity auth limiting is covered by
    /// <see cref="AuthAttemptRateLimitHttpTests"/>.)
    /// </summary>
    public class ProxyRateLimitHttpTests
    {
        private const string SessionsUrl = "/api/v1/sessions";

        // An unmapped, anonymous path: routing 404s it, but the GlobalLimiter (which is
        // unconditional) still runs, so a throttled request is 429 instead of 404.
        private const string GlobalProbeUrl = "/__ratelimit_probe__";

        private static Dictionary<string, string?> GlobalUserLimit(int permit) => new()
        {
            ["RateLimiting:Global:PermitLimit"] = permit.ToString(),
            ["RateLimiting:Global:WindowSeconds"] = "3600",
        };

        private static Dictionary<string, string?> GlobalAnonLimit(int permit) => new()
        {
            ["RateLimiting:GlobalAnonymous:PermitLimit"] = permit.ToString(),
            ["RateLimiting:GlobalAnonymous:WindowSeconds"] = "3600",
        };

        private static ProxyRateLimitFactory Factory(params IEnumerable<KeyValuePair<string, string?>>[] overrideSets)
        {
            var f = new ProxyRateLimitFactory();
            foreach (var set in overrideSets)
            {
                foreach (var kv in set)
                {
                    f.ConfigOverrides[kv.Key] = kv.Value;
                }
            }
            return f;
        }

        private static HttpClient InvalidTokenClient(ProxyRateLimitFactory f, string peer)
        {
            var c = f.CreateClientWithPeer(peer);
            c.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not.a.valid.jwt");
            return c;
        }

        // ---- global limiter: authenticated -> per user; anonymous -> per socket IP -

        [Fact]
        public async Task AuthenticatedUsers_GetIndependentGlobalPartitions_OnASharedPeer()
        {
            using var factory = Factory(GlobalUserLimit(3));
            var userA = factory.CreateClientForUser(4001, peerIp: "203.0.113.1");
            var userB = factory.CreateClientForUser(4002, peerIp: "203.0.113.1"); // SAME peer IP

            for (var i = 0; i < 3; i++)
            {
                Assert.Equal(HttpStatusCode.OK, (await userA.GetAsync(SessionsUrl)).StatusCode);
            }
            Assert.Equal((HttpStatusCode)429, (await userA.GetAsync(SessionsUrl)).StatusCode);

            Assert.Equal(HttpStatusCode.OK, (await userB.GetAsync(SessionsUrl)).StatusCode);
        }

        [Fact]
        public async Task SameUser_AcrossDifferentClientIps_SharesOneGlobalPartition()
        {
            using var factory = Factory(GlobalUserLimit(2));
            var fromIpA = factory.CreateClientForUser(4050, peerIp: "203.0.113.10");
            var fromIpB = factory.CreateClientForUser(4050, peerIp: "198.51.100.20"); // same user id

            Assert.Equal(HttpStatusCode.OK, (await fromIpA.GetAsync(SessionsUrl)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await fromIpB.GetAsync(SessionsUrl)).StatusCode);
            Assert.Equal((HttpStatusCode)429, (await fromIpA.GetAsync(SessionsUrl)).StatusCode);
        }

        [Fact]
        public async Task AnonymousGlobalTraffic_PartitionsByClientIp()
        {
            using var factory = Factory(GlobalAnonLimit(3));
            var a = factory.CreateClientWithPeer("203.0.113.71");
            var b = factory.CreateClientWithPeer("203.0.113.72");

            for (var i = 0; i < 3; i++)
            {
                Assert.Equal(HttpStatusCode.NotFound, (await a.GetAsync(GlobalProbeUrl)).StatusCode);
            }
            Assert.Equal((HttpStatusCode)429, (await a.GetAsync(GlobalProbeUrl)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync(GlobalProbeUrl)).StatusCode);
        }

        [Fact] // authed and anonymous aggregate ceilings are separately configurable
        public async Task AuthenticatedAndAnonymousAggregateCeilings_AreSeparate()
        {
            using var factory = Factory(GlobalUserLimit(50), GlobalAnonLimit(2));
            var anon = factory.CreateClientWithPeer("203.0.113.60");
            var user = factory.CreateClientForUser(4060, peerIp: "203.0.113.60"); // same peer

            Assert.Equal(HttpStatusCode.NotFound, (await anon.GetAsync(GlobalProbeUrl)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await anon.GetAsync(GlobalProbeUrl)).StatusCode);
            Assert.Equal((HttpStatusCode)429, (await anon.GetAsync(GlobalProbeUrl)).StatusCode); // anon ceiling hit

            // the authenticated user's separate 50-permit ceiling is untouched
            for (var i = 0; i < 5; i++)
            {
                Assert.Equal(HttpStatusCode.OK, (await user.GetAsync(SessionsUrl)).StatusCode);
            }
        }

        [Fact]
        public async Task IPv4_And_IPv4MappedIPv6_ShareOneGlobalPartition()
        {
            using var factory = Factory(GlobalAnonLimit(2));
            var v4 = factory.CreateClientWithPeer("203.0.113.80");
            var mapped = factory.CreateClientWithPeer("::ffff:203.0.113.80");

            Assert.Equal(HttpStatusCode.NotFound, (await v4.GetAsync(GlobalProbeUrl)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await mapped.GetAsync(GlobalProbeUrl)).StatusCode);
            Assert.Equal((HttpStatusCode)429, (await v4.GetAsync(GlobalProbeUrl)).StatusCode);
        }

        [Fact]
        public async Task MissingClientAddress_FallsIntoABoundedSharedPartition()
        {
            using var factory = Factory(GlobalAnonLimit(3));
            var noPeer = factory.CreateClient();

            for (var i = 0; i < 3; i++)
            {
                Assert.Equal(HttpStatusCode.NotFound, (await noPeer.GetAsync(GlobalProbeUrl)).StatusCode);
            }
            Assert.Equal((HttpStatusCode)429, (await noPeer.GetAsync(GlobalProbeUrl)).StatusCode);
        }

        [Fact] // forwarding headers can't shift the anonymous global partition
        public async Task SpoofedForwardingHeaders_DoNotChangeTheGlobalPartition()
        {
            using var factory = Factory(GlobalAnonLimit(2));
            var clean = factory.CreateClientWithPeer("203.0.113.40");
            var spoof = factory.CreateClientWithPeer("203.0.113.40");
            spoof.DefaultRequestHeaders.Add("X-Forwarded-For", "8.8.8.8");
            spoof.DefaultRequestHeaders.Add("X-Real-IP", "9.9.9.9");
            spoof.DefaultRequestHeaders.Add("X-Railway-Edge", "railway/edge");

            Assert.Equal(HttpStatusCode.NotFound, (await clean.GetAsync(GlobalProbeUrl)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await clean.GetAsync(GlobalProbeUrl)).StatusCode);
            Assert.Equal((HttpStatusCode)429, (await spoof.GetAsync(GlobalProbeUrl)).StatusCode); // same bucket
        }

        // ---- BLOCKER 2: rate limiting before authorization -----------------------

        [Fact]
        public async Task FirstMissingTokenRequest_ToProtectedEndpoint_Is401()
        {
            using var factory = Factory(GlobalAnonLimit(50));
            var anon = factory.CreateClientWithPeer("203.0.113.90");
            Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync(SessionsUrl)).StatusCode);
        }

        [Fact]
        public async Task FirstInvalidTokenRequest_ToProtectedEndpoint_Is401()
        {
            using var factory = Factory(GlobalAnonLimit(50));
            var bad = InvalidTokenClient(factory, "203.0.113.91");
            Assert.Equal(HttpStatusCode.Unauthorized, (await bad.GetAsync(SessionsUrl)).StatusCode);
        }

        [Fact]
        public async Task RepeatedMissingTokenRequests_AreGloballyConstrained_AndReturn429()
        {
            using var factory = Factory(GlobalAnonLimit(3));
            var anon = factory.CreateClientWithPeer("203.0.113.92");

            for (var i = 0; i < 3; i++)
            {
                Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync(SessionsUrl)).StatusCode);
            }
            Assert.Equal((HttpStatusCode)429, (await anon.GetAsync(SessionsUrl)).StatusCode);
        }

        [Fact]
        public async Task RepeatedInvalidTokenRequests_AreGloballyConstrained_AndReturn429()
        {
            using var factory = Factory(GlobalAnonLimit(3));
            var bad = InvalidTokenClient(factory, "203.0.113.93");

            for (var i = 0; i < 3; i++)
            {
                Assert.Equal(HttpStatusCode.Unauthorized, (await bad.GetAsync(SessionsUrl)).StatusCode);
            }
            Assert.Equal((HttpStatusCode)429, (await bad.GetAsync(SessionsUrl)).StatusCode);
        }

        [Fact]
        public async Task ThrottledUnauthorizedClient_DoesNotLockOutADifferentClient()
        {
            using var factory = Factory(GlobalAnonLimit(2));
            var a = factory.CreateClientWithPeer("203.0.113.94");
            var b = factory.CreateClientWithPeer("203.0.113.95");

            Assert.Equal(HttpStatusCode.Unauthorized, (await a.GetAsync(SessionsUrl)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await a.GetAsync(SessionsUrl)).StatusCode);
            Assert.Equal((HttpStatusCode)429, (await a.GetAsync(SessionsUrl)).StatusCode);

            Assert.Equal(HttpStatusCode.Unauthorized, (await b.GetAsync(SessionsUrl)).StatusCode);
        }

        [Fact]
        public async Task Authorization_Still401s_WhileGlobalQuotaIsNotExhausted()
        {
            using var factory = Factory(GlobalAnonLimit(5));
            var anon = factory.CreateClientWithPeer("203.0.113.96");

            for (var i = 0; i < 5; i++)
            {
                Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync(SessionsUrl)).StatusCode);
            }
        }

        [Fact]
        public async Task GlobalLimiter_SeesAuthenticatedIdentity_SoTokenSprayDoesNotShareABucket()
        {
            using var factory = Factory(GlobalUserLimit(2));
            var user = factory.CreateClientForUser(4100, peerIp: "203.0.113.1");
            var otherUserSamePeer = factory.CreateClientForUser(4101, peerIp: "203.0.113.1");

            Assert.Equal(HttpStatusCode.OK, (await user.GetAsync(SessionsUrl)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await user.GetAsync(SessionsUrl)).StatusCode);
            Assert.Equal((HttpStatusCode)429, (await user.GetAsync(SessionsUrl)).StatusCode);

            Assert.Equal(HttpStatusCode.OK, (await otherUserSamePeer.GetAsync(SessionsUrl)).StatusCode);
        }

        // ---- session-write policy preservation ---------------------------------

        [Fact]
        public async Task SessionWriteLimiter_ForValidUser_KeepsItsOwn429ContractAndRetryAfter()
        {
            using var factory = Factory(new Dictionary<string, string?>
            {
                ["RateLimiting:SessionWrite:TokenLimit"] = "1",
                ["RateLimiting:SessionWrite:TokensPerPeriod"] = "1",
                ["RateLimiting:SessionWrite:ReplenishmentPeriodSeconds"] = "3600",
                ["RateLimiting:SessionWrite:AutoReplenishment"] = "false",
            });
            var client = factory.CreateClientForUser(4200, peerIp: "203.0.113.1");
            var body = JsonContent.Create(new { date = "2026-09-03", name = "S", status = "draft" });

            Assert.Equal(HttpStatusCode.Created, (await client.PostAsync(SessionsUrl, body)).StatusCode);
            var limited = await client.PostAsync(SessionsUrl,
                JsonContent.Create(new { date = "2026-09-03", name = "S", status = "draft" }));

            Assert.Equal((HttpStatusCode)429, limited.StatusCode);
            Assert.Equal("{\"code\":\"rate_limited\"}", await limited.Content.ReadAsStringAsync());
            Assert.True(limited.Headers.TryGetValues("Retry-After", out var ra));
            Assert.True(int.TryParse(string.Join("", ra), out var seconds) && seconds >= 1);
        }

        [Fact]
        public async Task AnonymousSessionWrite_IsStillGlobalLimiterConstrained()
        {
            using var factory = Factory(
                GlobalAnonLimit(3),
                new Dictionary<string, string?> { ["RateLimiting:SessionWrite:TokenLimit"] = "1" });
            var anon = factory.CreateClientWithPeer("203.0.113.98");
            var body = () => JsonContent.Create(new { date = "2026-09-03", name = "S", status = "draft" });

            for (var i = 0; i < 3; i++)
            {
                Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PostAsync(SessionsUrl, body())).StatusCode);
            }
            Assert.Equal((HttpStatusCode)429, (await anon.PostAsync(SessionsUrl, body())).StatusCode);
        }

        [Fact]
        public async Task InvalidBearerToken_GlobalPartition_FallsBackToClientIp_NotAUserBucket()
        {
            using var factory = Factory(GlobalAnonLimit(2));
            var a = InvalidTokenClient(factory, "203.0.113.61");
            var b = InvalidTokenClient(factory, "203.0.113.62");

            Assert.Equal(HttpStatusCode.NotFound, (await a.GetAsync(GlobalProbeUrl)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await a.GetAsync(GlobalProbeUrl)).StatusCode);
            Assert.Equal((HttpStatusCode)429, (await a.GetAsync(GlobalProbeUrl)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync(GlobalProbeUrl)).StatusCode);
        }
    }
}
