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
    /// End-to-end HTTP coverage of the per-credential-identity auth-attempt limiter
    /// (the DI-backed MVC action filter) through the real <c>Program.cs</c> pipeline.
    /// Tiny per-identity limit, 1-hour window — no sleeps. The anonymous global ceiling
    /// is set high so it never masks the per-identity decision (except where a test
    /// deliberately exercises it).
    /// </summary>
    public class AuthAttemptRateLimitHttpTests
    {
        private const string LoginUrl = "/api/v1/auth/login";
        private const string SignupUrl = "/api/v1/auth/signup";

        private static Dictionary<string, string?> AuthLimit(int permit) => new()
        {
            ["RateLimiting:Auth:PermitLimit"] = permit.ToString(),
            ["RateLimiting:Auth:WindowSeconds"] = "3600",
            ["RateLimiting:Auth:QueueLimit"] = "0",
            ["RateLimiting:GlobalAnonymous:PermitLimit"] = "100000",
            ["RateLimiting:GlobalAnonymous:WindowSeconds"] = "3600",
        };

        private static ProxyRateLimitFactory Factory(int authPermit)
        {
            var f = new ProxyRateLimitFactory();
            foreach (var kv in AuthLimit(authPermit))
            {
                f.ConfigOverrides[kv.Key] = kv.Value;
            }
            return f;
        }

        private static HttpContent Login(string email, string password = "whatever") =>
            JsonContent.Create(new { email, password });

        private static HttpContent Signup(string email, string name = "Test User", string username = "someuser", string password = "Abcd1234") =>
            JsonContent.Create(new { name, username, email, password });

        // ---- per-identity quotas ------------------------------------------------

        [Fact]
        public async Task TwoLoginIdentities_HaveIndependentQuotas_AndThrottlingADoesNotBlockB()
        {
            using var factory = Factory(authPermit: 2);
            var client = factory.CreateClientWithPeer("203.0.113.1");

            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(LoginUrl, Login("a@example.com"))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(LoginUrl, Login("a@example.com"))).StatusCode);
            Assert.Equal((HttpStatusCode)429, (await client.PostAsync(LoginUrl, Login("a@example.com"))).StatusCode);

            // Identity B — same socket peer, different identity — is untouched.
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(LoginUrl, Login("b@example.com"))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(LoginUrl, Login("b@example.com"))).StatusCode);
        }

        [Fact] // the byte-identical email string shares one partition
        public async Task ExactRepeatedEmail_SharesOneQuota()
        {
            using var factory = Factory(authPermit: 2);
            var client = factory.CreateClientWithPeer("203.0.113.2");

            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(LoginUrl, Login("user@example.com"))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(LoginUrl, Login("user@example.com"))).StatusCode);
            Assert.Equal((HttpStatusCode)429, (await client.PostAsync(LoginUrl, Login("user@example.com"))).StatusCode);
        }

        [Fact] // production auth does WHERE "Email" = @p (case-sensitive) -> case variants are independent identities
        public async Task CaseDifferentEmails_HaveIndependentQuotas()
        {
            using var factory = Factory(authPermit: 2);
            var client = factory.CreateClientWithPeer("203.0.113.2");

            // exhaust "User@Example.com"
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(LoginUrl, Login("User@Example.com"))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(LoginUrl, Login("User@Example.com"))).StatusCode);
            Assert.Equal((HttpStatusCode)429, (await client.PostAsync(LoginUrl, Login("User@Example.com"))).StatusCode);

            // the lowercase identity — a DIFFERENT account in production — still has its full quota
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(LoginUrl, Login("user@example.com"))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(LoginUrl, Login("user@example.com"))).StatusCode);
        }

        [Fact]
        public async Task LoginAndSignup_ForTheSameIdentity_UseDifferentPartitions()
        {
            using var factory = Factory(authPermit: 1);
            var client = factory.CreateClientWithPeer("203.0.113.3");

            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(LoginUrl, Login("shared@example.com"))).StatusCode);
            Assert.Equal((HttpStatusCode)429, (await client.PostAsync(LoginUrl, Login("shared@example.com"))).StatusCode);

            // signup for the same email still has its own full quota
            var s = await client.PostAsync(SignupUrl, Signup("shared@example.com"));
            Assert.NotEqual((HttpStatusCode)429, s.StatusCode);
        }

        [Fact] // existent and nonexistent accounts get identical rate-limit treatment (no enumeration signal)
        public async Task ExistentAndNonexistentAccounts_AreRateLimitedIdentically()
        {
            using var factory = Factory(authPermit: 3);
            factory.SeedUserWithCredentials(70001, "real@example.com", "RightPassword1");
            var client = factory.CreateClientWithPeer("203.0.113.4");

            async Task<(int admitted, bool sixthIs429)> Drain(string email, string password)
            {
                var admitted = 0;
                for (var i = 0; i < 3; i++)
                {
                    var r = await client.PostAsync(LoginUrl, Login(email, password));
                    if ((int)r.StatusCode != 429) admitted++;
                }
                var sixth = await client.PostAsync(LoginUrl, Login(email, password));
                return (admitted, (int)sixth.StatusCode == 429);
            }

            var existent = await Drain("real@example.com", "WrongPassword1"); // account exists, bad password
            var missing = await Drain("ghost@example.com", "WrongPassword1"); // no such account

            Assert.Equal(existent.admitted, missing.admitted);
            Assert.Equal(3, existent.admitted);
            Assert.True(existent.sixthIs429 && missing.sixthIs429);
        }

        [Fact] // even a correct-password login is rate-limited the same way
        public async Task CorrectPasswordLogin_IsAlsoRateLimited_ByIdentity()
        {
            using var factory = Factory(authPermit: 1);
            factory.SeedUserWithCredentials(70002, "correct@example.com", "GoodPassword1");
            var client = factory.CreateClientWithPeer("203.0.113.5");

            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync(LoginUrl, Login("correct@example.com", "GoodPassword1"))).StatusCode);
            Assert.Equal((HttpStatusCode)429, (await client.PostAsync(LoginUrl, Login("correct@example.com", "GoodPassword1"))).StatusCode);
        }

        // ---- identity is insulated from transport + password ------------------

        [Fact]
        public async Task ForwardingHeadersAndSocketPeerChanges_DoNotChangeAnIdentityBucket()
        {
            using var factory = Factory(authPermit: 2);

            var a = factory.CreateClientWithPeer("203.0.113.10");
            Assert.Equal(HttpStatusCode.Unauthorized, (await a.PostAsync(LoginUrl, Login("victim@example.com"))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await a.PostAsync(LoginUrl, Login("victim@example.com"))).StatusCode);

            // different socket peer + every plausible forwarding header, same identity -> still throttled
            var b = factory.CreateClientWithPeer("198.51.100.20");
            b.DefaultRequestHeaders.Add("X-Forwarded-For", "8.8.8.8");
            b.DefaultRequestHeaders.Add("X-Real-IP", "9.9.9.9");
            b.DefaultRequestHeaders.Add("X-Railway-Edge", "railway/edge");
            Assert.Equal((HttpStatusCode)429, (await b.PostAsync(LoginUrl, Login("victim@example.com"))).StatusCode);
        }

        [Fact]
        public async Task ChangingThePassword_DoesNotMintANewPartition()
        {
            using var factory = Factory(authPermit: 2);
            var client = factory.CreateClientWithPeer("203.0.113.11");

            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(LoginUrl, Login("pw@example.com", "one"))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(LoginUrl, Login("pw@example.com", "two"))).StatusCode);
            Assert.Equal((HttpStatusCode)429, (await client.PostAsync(LoginUrl, Login("pw@example.com", "three"))).StatusCode);
        }

        [Theory] // malformed / whitespace-only email is 400'd BEFORE the filter — never touches any bucket
        [InlineData("not-an-email")]
        [InlineData("   ")]
        [InlineData("")]
        public async Task InvalidEmailSpam_Is400BeforeTheFilter_AndDoesNotConsumeAValidIdentityBucket(string bad)
        {
            using var factory = Factory(authPermit: 2);
            var client = factory.CreateClientWithPeer("203.0.113.12");

            for (var i = 0; i < 6; i++)
            {
                Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(LoginUrl, Login(bad))).StatusCode);
            }

            // the filter never ran for those, so a fresh identity still has its full quota
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(LoginUrl, Login("fresh@example.com"))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(LoginUrl, Login("fresh@example.com"))).StatusCode);
            Assert.Equal((HttpStatusCode)429, (await client.PostAsync(LoginUrl, Login("fresh@example.com"))).StatusCode);
        }

        // ---- 429 shape + no identity disclosure ------------------------------

        [Fact]
        public async Task Auth429_IsBare_AndNeverContainsTheIdentity()
        {
            using var factory = Factory(authPermit: 1);
            var client = factory.CreateClientWithPeer("203.0.113.13");
            const string email = "SecretPerson@hidden-domain.example";

            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(LoginUrl, Login(email))).StatusCode);
            var limited = await client.PostAsync(LoginUrl, Login(email));

            Assert.Equal((HttpStatusCode)429, limited.StatusCode);
            var body = await limited.Content.ReadAsStringAsync();
            Assert.True(string.IsNullOrEmpty(body));
            Assert.DoesNotContain("rate_limited", body);
            foreach (var header in limited.Headers)
            {
                var joined = string.Join(",", header.Value);
                Assert.DoesNotContain("secretperson", joined, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("hidden-domain", joined, StringComparison.OrdinalIgnoreCase);
            }
        }

        // ---- signup is limited per identity too ------------------------------

        [Fact]
        public async Task Signup_IsLimitedPerEmail()
        {
            using var factory = Factory(authPermit: 2);
            var client = factory.CreateClientWithPeer("203.0.113.14");

            // duplicate-email signups -> 400, but each still consumes an attempt
            Assert.NotEqual((HttpStatusCode)429, (await client.PostAsync(SignupUrl, Signup("dup@example.com", username: "u1"))).StatusCode);
            Assert.NotEqual((HttpStatusCode)429, (await client.PostAsync(SignupUrl, Signup("dup@example.com", username: "u2"))).StatusCode);
            Assert.Equal((HttpStatusCode)429, (await client.PostAsync(SignupUrl, Signup("dup@example.com", username: "u3"))).StatusCode);

            // a different email is unaffected
            Assert.NotEqual((HttpStatusCode)429, (await client.PostAsync(SignupUrl, Signup("other@example.com", username: "u4"))).StatusCode);
        }

        // ---- rotating-identity attacker is bounded by the aggregate ceiling ---

        [Fact]
        public async Task RotatingIdentityAttacker_IsBoundedByTheAnonymousAggregateCeiling()
        {
            var f = new ProxyRateLimitFactory();
            f.ConfigOverrides["RateLimiting:Auth:PermitLimit"] = "50";       // per-identity: effectively unlimited here
            f.ConfigOverrides["RateLimiting:Auth:WindowSeconds"] = "3600";
            f.ConfigOverrides["RateLimiting:GlobalAnonymous:PermitLimit"] = "3"; // aggregate ceiling
            f.ConfigOverrides["RateLimiting:GlobalAnonymous:WindowSeconds"] = "3600";
            using var factory = f;
            var client = factory.CreateClientWithPeer("203.0.113.15");

            // 3 distinct identities admitted (401), then the aggregate ceiling rejects the rest
            for (var i = 0; i < 3; i++)
            {
                Assert.Equal(HttpStatusCode.Unauthorized,
                    (await client.PostAsync(LoginUrl, Login($"rot{i}@example.com"))).StatusCode);
            }
            Assert.Equal((HttpStatusCode)429, (await client.PostAsync(LoginUrl, Login("rot99@example.com"))).StatusCode);
        }

        [Fact] // a bearer token cannot move a login attempt onto the authenticated global ceiling
        public async Task BearerAuthenticatedLogin_StillCountsAgainstTheAnonymousAggregateCeiling()
        {
            var f = new ProxyRateLimitFactory();
            f.ConfigOverrides["RateLimiting:Auth:PermitLimit"] = "50";            // per-identity: not the limit here
            f.ConfigOverrides["RateLimiting:Auth:WindowSeconds"] = "3600";
            f.ConfigOverrides["RateLimiting:GlobalAnonymous:PermitLimit"] = "2";  // the anonymous aggregate ceiling
            f.ConfigOverrides["RateLimiting:GlobalAnonymous:WindowSeconds"] = "3600";
            f.ConfigOverrides["RateLimiting:Global:PermitLimit"] = "100000";      // authed ceiling: huge
            f.ConfigOverrides["RateLimiting:Global:WindowSeconds"] = "3600";
            using var factory = f;

            // A client holding a valid JWT for a real user, hammering login with rotating emails.
            var client = factory.CreateClientForUser(70050, peerIp: "203.0.113.30");

            Assert.NotEqual((HttpStatusCode)429, (await client.PostAsync(LoginUrl, Login("ra@example.com"))).StatusCode);
            Assert.NotEqual((HttpStatusCode)429, (await client.PostAsync(LoginUrl, Login("rb@example.com"))).StatusCode);
            // stopped by the anonymous ceiling, NOT admitted under the authed 100k ceiling
            Assert.Equal((HttpStatusCode)429, (await client.PostAsync(LoginUrl, Login("rc@example.com"))).StatusCode);
        }
    }
}
