using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using GoHardAPI.Tests.Infrastructure;
using Xunit;

namespace GoHardAPI.Tests.RateLimiting
{
    /// <summary>
    /// End-to-end HTTP coverage of the <c>session-write</c> limiter through the real
    /// <c>Program.cs</c> pipeline (isolated <see cref="SessionWriteRateLimitFactory"/>
    /// per test, in-memory DB, deterministic tiny token bucket with
    /// <c>AutoReplenishment = false</c>).
    /// </summary>
    public class SessionWriteRateLimitHttpTests
    {
        private const string CreateUrl = "/api/v1/sessions";

        private static Dictionary<string, string?> Tiny(int tokenLimit) => new()
        {
            ["RateLimiting:SessionWrite:TokenLimit"] = tokenLimit.ToString(),
            ["RateLimiting:SessionWrite:TokensPerPeriod"] = "1",
            ["RateLimiting:SessionWrite:ReplenishmentPeriodSeconds"] = "3600",
            ["RateLimiting:SessionWrite:AutoReplenishment"] = "false",
        };

        private static SessionWriteRateLimitFactory Factory(int tokenLimit = 3, bool? enabled = null)
        {
            var f = new SessionWriteRateLimitFactory();
            foreach (var kv in Tiny(tokenLimit))
            {
                f.ConfigOverrides[kv.Key] = kv.Value;
            }
            if (enabled is { } e)
            {
                f.ConfigOverrides["RateLimiting:SessionWrite:Enabled"] = e ? "true" : "false";
            }
            return f;
        }

        private static HttpContent UnkeyedBody() =>
            JsonContent.Create(new { date = "2026-09-03", name = "S", status = "draft" });

        private static HttpContent KeyedBody(Guid key) =>
            JsonContent.Create(new { date = "2026-09-03", name = "S", status = "draft", clientOperationId = key });

        // ---- 12/13: exhaustion + independence ------------------------------------------

        [Fact] // 12
        public async Task UserA_ExhaustsBucket_ThenReceives429_WithExactBodyAndRetryAfter()
        {
            using var factory = Factory(tokenLimit: 3);
            var clientA = factory.CreateClientForUser(1);

            for (var i = 0; i < 3; i++)
            {
                var ok = await clientA.PostAsync(CreateUrl, UnkeyedBody());
                Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
            }

            var rejected = await clientA.PostAsync(CreateUrl, UnkeyedBody());

            Assert.Equal((HttpStatusCode)429, rejected.StatusCode);
            Assert.Equal("application/json", rejected.Content.Headers.ContentType?.MediaType);
            Assert.Equal("{\"code\":\"rate_limited\"}", await rejected.Content.ReadAsStringAsync());
            Assert.True(rejected.Headers.TryGetValues("Retry-After", out var ra));
            Assert.True(int.TryParse(string.Join("", ra), out var seconds) && seconds >= 1);
        }

        [Fact] // 13
        public async Task UserB_RemainsAdmitted_WhileUserA_IsExhausted()
        {
            using var factory = Factory(tokenLimit: 2);
            var clientA = factory.CreateClientForUser(1);
            var clientB = factory.CreateClientForUser(2);

            Assert.Equal(HttpStatusCode.Created, (await clientA.PostAsync(CreateUrl, UnkeyedBody())).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await clientA.PostAsync(CreateUrl, UnkeyedBody())).StatusCode);
            Assert.Equal((HttpStatusCode)429, (await clientA.PostAsync(CreateUrl, UnkeyedBody())).StatusCode);

            Assert.Equal(HttpStatusCode.Created, (await clientB.PostAsync(CreateUrl, UnkeyedBody())).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await clientB.PostAsync(CreateUrl, UnkeyedBody())).StatusCode);
        }

        [Fact] // 7 (HTTP form): Enabled=false -> no 429 even far past the token limit
        public async Task Disabled_BypassesTheNamedLimiter_OverHttp()
        {
            using var factory = Factory(tokenLimit: 1, enabled: false);
            var client = factory.CreateClientForUser(1);

            for (var i = 0; i < 6; i++)
            {
                var resp = await client.PostAsync(CreateUrl, UnkeyedBody());
                Assert.NotEqual((HttpStatusCode)429, resp.StatusCode);
                Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
            }

            // The auth limiter is untouched by the session-write toggle.
            var get = await client.GetAsync("/api/v1/sessions");
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        }

        // ---- 14: partition IS the JWT user id ---------------------------------------

        [Fact] // 14
        public async Task Partition_IsTheJwtUserId_NotThePerConnectionOrPerTokenIdentity()
        {
            using var factory = Factory(tokenLimit: 2);

            // Two DIFFERENT clients / different JWTs, SAME user id -> one shared bucket.
            factory.SeedUser(9);
            var first = factory.CreateClient();
            first.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionWriteRateLimitFactory.MintToken(9));
            var second = factory.CreateClient();
            second.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionWriteRateLimitFactory.MintToken(9));

            Assert.Equal(HttpStatusCode.Created, (await first.PostAsync(CreateUrl, UnkeyedBody())).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await second.PostAsync(CreateUrl, UnkeyedBody())).StatusCode);
            // Bucket (capacity 2) is now empty for user 9 regardless of which client asks.
            Assert.Equal((HttpStatusCode)429, (await first.PostAsync(CreateUrl, UnkeyedBody())).StatusCode);
            Assert.Equal((HttpStatusCode)429, (await second.PostAsync(CreateUrl, UnkeyedBody())).StatusCode);

            // A different user is unaffected -> users never collapse into one bucket.
            var other = factory.CreateClientForUser(10);
            Assert.Equal(HttpStatusCode.Created, (await other.PostAsync(CreateUrl, UnkeyedBody())).StatusCode);
        }

        // ---- 15/16: auth before the limiter ---------------------------------------

        [Fact] // 15
        public async Task MissingToken_Returns401_Not429()
        {
            using var factory = Factory(tokenLimit: 1);
            var anon = factory.CreateAnonymousClient();

            for (var i = 0; i < 5; i++)
            {
                var resp = await anon.PostAsync(CreateUrl, UnkeyedBody());
                Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
            }
        }

        [Fact] // 16
        public async Task InvalidToken_Returns401_Not429_Not500()
        {
            using var factory = Factory(tokenLimit: 1);
            var bad = factory.CreateClientWithInvalidToken();

            var resp = await bad.PostAsync(CreateUrl, UnkeyedBody());
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        // ---- 17/18: only generic POST /sessions is limited -----------------------

        [Fact] // 17 + 18
        public async Task OnlyGenericCreate_CarriesThePolicy_OtherSessionWritesNeverGet429()
        {
            using var factory = Factory(tokenLimit: 1);
            var client = factory.CreateClientForUser(1);

            // Drain the session-write bucket.
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsync(CreateUrl, UnkeyedBody())).StatusCode);
            Assert.Equal((HttpStatusCode)429, (await client.PostAsync(CreateUrl, UnkeyedBody())).StatusCode);

            // Every other Session-write endpoint: whatever they return, it is NEVER 429.
            var fromProgram = await client.PostAsync("/api/v1/sessions/from-program-workout",
                JsonContent.Create(new { programWorkoutId = 999999, programId = 999999 }));
            var put = await client.PutAsync("/api/v1/sessions/999999", UnkeyedBody());
            var patchStatus = await client.PatchAsync("/api/v1/sessions/999999/status",
                JsonContent.Create(new { status = "in_progress" }));
            var patchStart = await client.PatchAsync("/api/v1/sessions/999999/start-planned",
                JsonContent.Create(new { }));
            var del = await client.DeleteAsync("/api/v1/sessions/999999");
            var addExercise = await client.PostAsync("/api/v1/sessions/999999/exercises",
                JsonContent.Create(new { exerciseTemplateId = 999999 }));

            foreach (var resp in new[] { fromProgram, put, patchStatus, patchStart, del, addExercise })
            {
                Assert.NotEqual((HttpStatusCode)429, resp.StatusCode);
            }
        }

        // ---- 19/20: nothing happens after rejection ------------------------------

        [Fact] // 19
        public async Task RejectedRequest_NeverInvokesSessionCreateService()
        {
            using var factory = Factory(tokenLimit: 1);
            var client = factory.CreateClientForUser(1);

            Assert.Equal(HttpStatusCode.Created, (await client.PostAsync(CreateUrl, UnkeyedBody())).StatusCode);
            Assert.Equal(1, factory.CreateCalls);

            Assert.Equal((HttpStatusCode)429, (await client.PostAsync(CreateUrl, UnkeyedBody())).StatusCode);
            Assert.Equal(1, factory.CreateCalls); // unchanged -> controller/service never ran
        }

        [Fact] // 20
        public async Task RejectedRequest_WritesNoDatabaseRecords()
        {
            using var factory = Factory(tokenLimit: 1);
            var client = factory.CreateClientForUser(1);

            Assert.Equal(HttpStatusCode.Created, (await client.PostAsync(CreateUrl, KeyedBody(Guid.NewGuid()))).StatusCode);
            var sessionsAfterOk = factory.SessionCount(1);
            var opsAfterOk = factory.OperationCount(1);

            Assert.Equal((HttpStatusCode)429, (await client.PostAsync(CreateUrl, KeyedBody(Guid.NewGuid()))).StatusCode);

            Assert.Equal(sessionsAfterOk, factory.SessionCount(1));
            Assert.Equal(opsAfterOk, factory.OperationCount(1));
        }

        // ---- 21/22/23: replay semantics under the limiter -----------------------

        [Fact] // 21
        public async Task AdmittedKeyedCreate_Returns201()
        {
            using var factory = Factory(tokenLimit: 5);
            var client = factory.CreateClientForUser(1);

            var resp = await client.PostAsync(CreateUrl, KeyedBody(Guid.NewGuid()));
            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        }

        [Fact] // 22
        public async Task AdmittedKeyedReplay_Returns200_WithTheOriginalSession()
        {
            using var factory = Factory(tokenLimit: 5);
            var client = factory.CreateClientForUser(1);
            var key = Guid.NewGuid();

            var first = await client.PostAsync(CreateUrl, KeyedBody(key));
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            var firstId = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

            var replay = await client.PostAsync(CreateUrl, KeyedBody(key));
            Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
            var replayId = (await replay.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

            Assert.Equal(firstId, replayId);
            Assert.Equal(1, factory.SessionCount(1));
            Assert.Equal(1, factory.OperationCount(1));
        }

        [Fact] // 23
        public async Task ReplayRejectedByLimiter_IsAClean429_NoDuplicate_No500()
        {
            using var factory = Factory(tokenLimit: 1);
            var client = factory.CreateClientForUser(1);
            var key = Guid.NewGuid();

            var first = await client.PostAsync(CreateUrl, KeyedBody(key));
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var replay = await client.PostAsync(CreateUrl, KeyedBody(key)); // bucket empty
            Assert.Equal((HttpStatusCode)429, replay.StatusCode);
            Assert.Equal("{\"code\":\"rate_limited\"}", await replay.Content.ReadAsStringAsync());

            Assert.Equal(1, factory.SessionCount(1));   // no duplicate
            Assert.Equal(1, factory.OperationCount(1)); // no duplicate
        }

        // ---- 24 + 10 (HTTP form): the auth-attempt limiter + global limiter -----

        [Fact] // 24 + 10
        public async Task AuthAttemptLimiter_429sWithABareBody_AndGlobalTrafficIsUnaffected()
        {
            using var factory = Factory(tokenLimit: 3);

            // The per-identity auth-attempt limiter defaults to 5/min + queue 2. All 25
            // requests carry the SAME email, so they hit one partition: fire enough
            // concurrently that some are rejected immediately (can't even queue), and
            // stop at the first 429 so queued requests are never awaited.
            var pending = Enumerable.Range(0, 25).Select(_ =>
            {
                var c = factory.CreateAnonymousClient();
                c.Timeout = TimeSpan.FromSeconds(20);
                return c.PostAsync("/api/v1/auth/login",
                    JsonContent.Create(new { email = "x@y.com", password = "nope" }));
            }).ToList();

            HttpResponseMessage? authRejected = null;
            while (pending.Count > 0 && authRejected is null)
            {
                var done = await Task.WhenAny(pending);
                pending.Remove(done);
                try
                {
                    var r = await done;
                    if ((int)r.StatusCode == 429)
                    {
                        authRejected = r;
                    }
                }
                catch
                {
                    // an abandoned/queued request timing out — ignore
                }
            }

            Assert.NotNull(authRejected);
            var body = await authRejected!.Content.ReadAsStringAsync();
            Assert.DoesNotContain("rate_limited", body); // NOT the session-write shape
            Assert.True(string.IsNullOrEmpty(body) || !body.Contains("\"code\""));

            // An authenticated GET still succeeds -> global limiter behavior intact.
            var authed = factory.CreateClientForUser(1);
            var get = await authed.GetAsync("/api/v1/sessions");
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        }
    }
}
