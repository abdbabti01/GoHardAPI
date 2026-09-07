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

        // ---- 17/18: the create-operation write path is limited; the other Session
        // writes (PUT / PATCH / DELETE-by-id / add-exercise) are not. POST
        // /sessions/from-program-workout DOES carry the policy (it also writes the
        // Sessions / SessionCreateOperations create-operation path) and the cancel
        // counterpart DELETE /sessions/by-operation/{key} does too (the latter covered
        // by SessionCreateCancellationHttpTests). --------------

        [Fact] // 17 + 18
        public async Task OtherSessionWrites_DoNotCarryThePolicy_AndNeverGet429()
        {
            using var factory = Factory(tokenLimit: 1);
            var client = factory.CreateClientForUser(1);

            // Drain the session-write bucket.
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsync(CreateUrl, UnkeyedBody())).StatusCode);
            Assert.Equal((HttpStatusCode)429, (await client.PostAsync(CreateUrl, UnkeyedBody())).StatusCode);

            // Every OTHER Session-write endpoint: whatever they return, it is NEVER 429.
            var put = await client.PutAsync("/api/v1/sessions/999999", UnkeyedBody());
            var patchStatus = await client.PatchAsync("/api/v1/sessions/999999/status",
                JsonContent.Create(new { status = "in_progress" }));
            var patchStart = await client.PatchAsync("/api/v1/sessions/999999/start-planned",
                JsonContent.Create(new { }));
            var del = await client.DeleteAsync("/api/v1/sessions/999999");
            var addExercise = await client.PostAsync("/api/v1/sessions/999999/exercises",
                JsonContent.Create(new { exerciseTemplateId = 999999 }));

            foreach (var resp in new[] { put, patchStatus, patchStart, del, addExercise })
            {
                Assert.NotEqual((HttpStatusCode)429, resp.StatusCode);
            }
        }

        [Fact] // 18b: from-program-workout shares the same per-user bucket as POST /sessions.
        public async Task FromProgramWorkout_CarriesTheSessionWritePolicy_429AfterBucketDrained()
        {
            using var factory = Factory(tokenLimit: 1);
            var client = factory.CreateClientForUser(1);

            // Drain the shared per-user session-write bucket with the generic create.
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsync(CreateUrl, UnkeyedBody())).StatusCode);

            var rejected = await client.PostAsync("/api/v1/sessions/from-program-workout",
                JsonContent.Create(new { programWorkoutId = 999999, programId = 999999 }));

            Assert.Equal((HttpStatusCode)429, rejected.StatusCode);
            Assert.Equal("{\"code\":\"rate_limited\"}", await rejected.Content.ReadAsStringAsync());
            Assert.True(rejected.Headers.TryGetValues("Retry-After", out _));
        }

        // ---- 18c-18h: behavioral proof that the from-program-workout create-operation path
        // (keyed AND unkeyed) is on the SAME per-user session-write budget as
        // POST /sessions and DELETE /sessions/by-operation/{key} ----------------------

        private const string FromProgramWorkoutUrl = "/api/v1/sessions/from-program-workout";

        private static HttpContent FpwUnkeyed(int programId, int workoutId) =>
            JsonContent.Create(new { programWorkoutId = workoutId, programId });

        private static HttpContent FpwKeyed(int programId, int workoutId, Guid key) =>
            JsonContent.Create(new { programWorkoutId = workoutId, programId, clientOperationId = key });

        [Fact] // 18c: both the keyed and the unkeyed from-program-workout request are limited.
        public async Task FromProgramWorkout_KeyedAndUnkeyed_AreBothSubjectToThePolicy()
        {
            using var factory = Factory(tokenLimit: 1);
            var (p, w) = factory.SeedProgramWorkout(1);
            var client = factory.CreateClientForUser(1);

            // Unkeyed drains the single token.
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsync(FromProgramWorkoutUrl, FpwUnkeyed(p, w))).StatusCode);

            // Keyed request now rejected on the same bucket.
            var keyedRejected = await client.PostAsync(FromProgramWorkoutUrl, FpwKeyed(p, w, Guid.NewGuid()));
            Assert.Equal((HttpStatusCode)429, keyedRejected.StatusCode);
            Assert.Equal("{\"code\":\"rate_limited\"}", await keyedRejected.Content.ReadAsStringAsync());
            Assert.True(keyedRejected.Headers.TryGetValues("Retry-After", out var ra1)
                       && int.TryParse(string.Join("", ra1), out var s1) && s1 >= 1);

            // And the reverse: with a fresh bucket, keyed drains and unkeyed is then rejected.
            using var factory2 = Factory(tokenLimit: 1);
            var (p2, w2) = factory2.SeedProgramWorkout(1);
            var client2 = factory2.CreateClientForUser(1);
            Assert.Equal(HttpStatusCode.Created,
                (await client2.PostAsync(FromProgramWorkoutUrl, FpwKeyed(p2, w2, Guid.NewGuid()))).StatusCode);
            var unkeyedRejected = await client2.PostAsync(FromProgramWorkoutUrl, FpwUnkeyed(p2, w2));
            Assert.Equal((HttpStatusCode)429, unkeyedRejected.StatusCode);
        }

        [Fact] // 18d: generic CREATE, from-program-workout CREATE and cancel share ONE user bucket.
        public async Task AllThreeCreateOperationEndpoints_ConsumeTheSameUserBudget()
        {
            using var factory = Factory(tokenLimit: 1);
            var (p, w) = factory.SeedProgramWorkout(1);
            var client = factory.CreateClientForUser(1);

            // Spend the single token on a from-program-workout create.
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsync(FromProgramWorkoutUrl, FpwUnkeyed(p, w))).StatusCode);

            // Both siblings are now over budget for THIS user.
            var genericRejected = await client.PostAsync(CreateUrl, UnkeyedBody());
            Assert.Equal((HttpStatusCode)429, genericRejected.StatusCode);
            Assert.Equal("{\"code\":\"rate_limited\"}", await genericRejected.Content.ReadAsStringAsync());

            var cancelRejected = await client.DeleteAsync($"/api/v1/sessions/by-operation/{Guid.NewGuid()}");
            Assert.Equal((HttpStatusCode)429, cancelRejected.StatusCode);
            Assert.Equal("{\"code\":\"rate_limited\"}", await cancelRejected.Content.ReadAsStringAsync());
        }

        [Fact] // 18e: exhausting A's from-program-workout budget does not touch B's.
        public async Task FromProgramWorkout_ExhaustingUserA_LeavesUserBAdmitted()
        {
            using var factory = Factory(tokenLimit: 2);
            var (pa, wa) = factory.SeedProgramWorkout(1);
            var (pb, wb) = factory.SeedProgramWorkout(2);
            var a = factory.CreateClientForUser(1);
            var b = factory.CreateClientForUser(2);

            Assert.Equal(HttpStatusCode.Created, (await a.PostAsync(FromProgramWorkoutUrl, FpwUnkeyed(pa, wa))).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await a.PostAsync(FromProgramWorkoutUrl, FpwUnkeyed(pa, wa))).StatusCode);
            Assert.Equal((HttpStatusCode)429, (await a.PostAsync(FromProgramWorkoutUrl, FpwUnkeyed(pa, wa))).StatusCode);

            // B's bucket is independent.
            Assert.Equal(HttpStatusCode.Created, (await b.PostAsync(FromProgramWorkoutUrl, FpwUnkeyed(pb, wb))).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await b.PostAsync(FromProgramWorkoutUrl, FpwKeyed(pb, wb, Guid.NewGuid()))).StatusCode);
        }

        [Fact] // 18f: a rejected from-program-workout request writes NOTHING and never reaches the service.
        public async Task RejectedFromProgramWorkout_WritesNoSessionChildrenOrOperationRow_AndSkipsTheService()
        {
            using var factory = Factory(tokenLimit: 1);
            var (p, w) = factory.SeedProgramWorkout(1);
            var client = factory.CreateClientForUser(1);

            // One admitted create so there is prior state to compare against.
            Assert.Equal(HttpStatusCode.Created,
                (await client.PostAsync(FromProgramWorkoutUrl, FpwKeyed(p, w, Guid.NewGuid()))).StatusCode);
            var sessions = factory.SessionCount(1);
            var exercises = factory.ExerciseCount(1);
            var ops = factory.OperationCount(1);
            var calls = factory.CreateCalls;

            // This one WOULD succeed (valid owned program+workout) but the bucket is empty.
            var rejected = await client.PostAsync(FromProgramWorkoutUrl, FpwKeyed(p, w, Guid.NewGuid()));
            Assert.Equal((HttpStatusCode)429, rejected.StatusCode);

            Assert.Equal(sessions, factory.SessionCount(1));   // no new Session
            Assert.Equal(exercises, factory.ExerciseCount(1)); // no new Exercises
            Assert.Equal(ops, factory.OperationCount(1));      // no new operation row
            Assert.Equal(calls, factory.CreateCalls);          // service never invoked
        }

        [Fact] // 18g: an admitted from-program-workout create keeps its established shape/status.
        public async Task AdmittedFromProgramWorkout_Returns201_WithExercises_ThenKeyedReplayReturns200()
        {
            using var factory = Factory(tokenLimit: 5);
            var (p, w) = factory.SeedProgramWorkout(1, "[{\"name\":\"Squat\"},{\"name\":\"Bench\"},{\"name\":\"Row\"}]");
            var client = factory.CreateClientForUser(1);
            var key = Guid.NewGuid();

            var first = await client.PostAsync(FromProgramWorkoutUrl, FpwKeyed(p, w, key));
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            var firstBody = await first.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(3, firstBody.GetProperty("exercises").GetArrayLength());
            var firstId = firstBody.GetProperty("id").GetInt32();

            var replay = await client.PostAsync(FromProgramWorkoutUrl, FpwKeyed(p, w, key));
            Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
            var replayBody = await replay.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(firstId, replayBody.GetProperty("id").GetInt32());
            Assert.Equal(3, replayBody.GetProperty("exercises").GetArrayLength()); // no duplicate children

            Assert.Equal(1, factory.SessionCount(1));
            Assert.Equal(1, factory.OperationCount(1));
        }

        [Fact] // 18h: the empty-GUID key is a 400 (not a 429) even when the bucket still has tokens,
                // and the 400 is returned BEFORE any write.
        public async Task FromProgramWorkout_EmptyGuidKey_Returns400_invalid_operation_key_NotRateLimited()
        {
            using var factory = Factory(tokenLimit: 5);
            var (p, w) = factory.SeedProgramWorkout(1);
            var client = factory.CreateClientForUser(1);

            var resp = await client.PostAsync(FromProgramWorkoutUrl, FpwKeyed(p, w, Guid.Empty));

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            Assert.Contains("invalid_operation_key", await resp.Content.ReadAsStringAsync());
            Assert.Equal(0, factory.SessionCount(1));
            Assert.Equal(0, factory.ExerciseCount(1));
            Assert.Equal(0, factory.OperationCount(1));
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
