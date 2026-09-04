using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GoHardAPI.Data;
using GoHardAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace GoHardAPI.Tests.RateLimiting
{
    /// <summary>
    /// Smallest real-PostgreSQL coverage of the <c>session-write</c> limiter: a rejected
    /// keyed CREATE writes no rows, a replay near exhaustion is either a clean 200 or a
    /// clean 429 with unchanged row counts, one user's exhaustion cannot touch another
    /// user's rows, and concurrent over-limit requests never exceed the admitted count
    /// or leave a partial operation row.
    ///
    /// Runs the real <c>Program.cs</c> pipeline over a Testcontainers PostgreSQL 16
    /// database (shared <see cref="PostgresFixture"/>). No wall-clock sleeps — the token
    /// bucket uses <c>AutoReplenishment = false</c> with tiny limits.
    /// </summary>
    [Collection(PostgresCollection.Name)]
    [Trait("Category", "PostgresIntegration")]
    public sealed class SessionWriteRateLimitPostgresTests
    {
        private readonly PostgresFixture _pg;
        private static int _userSeq = 900_000;

        public SessionWriteRateLimitPostgresTests(PostgresFixture pg) => _pg = pg;

        private static int NextUserId() => Interlocked.Increment(ref _userSeq);

        private sealed class PgFactory : SessionWriteRateLimitFactory
        {
            private readonly string _connectionString;
            public PgFactory(string connectionString) => _connectionString = connectionString;

            protected override void RegisterDatabase(IServiceCollection services) =>
                services.AddDbContext<TrainingContext>(o => o.UseNpgsql(_connectionString));
        }

        private PgFactory NewFactory(int tokenLimit)
        {
            var f = new PgFactory(_pg.ConnectionString);
            f.ConfigOverrides["RateLimiting:SessionWrite:TokenLimit"] = tokenLimit.ToString();
            f.ConfigOverrides["RateLimiting:SessionWrite:TokensPerPeriod"] = "1";
            f.ConfigOverrides["RateLimiting:SessionWrite:ReplenishmentPeriodSeconds"] = "3600";
            f.ConfigOverrides["RateLimiting:SessionWrite:AutoReplenishment"] = "false";
            return f;
        }

        private async Task SeedUserAsync(int id)
        {
            await using var c = new NpgsqlConnection(_pg.ConnectionString);
            await c.OpenAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText =
                "INSERT INTO \"Users\" (\"Id\",\"Name\",\"Email\",\"PasswordHash\") " +
                "VALUES (@id,@n,@e,'x') ON CONFLICT DO NOTHING";
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("n", $"user{id}");
            cmd.Parameters.AddWithValue("e", $"user{id}@example.com");
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<long> ScalarAsync(string sql)
        {
            await using var c = new NpgsqlConnection(_pg.ConnectionString);
            await c.OpenAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToInt64(await cmd.ExecuteScalarAsync() ?? 0L);
        }

        private Task<long> SessionCount(int userId) =>
            ScalarAsync($"SELECT count(*) FROM \"Sessions\" WHERE \"UserId\" = {userId}");

        private Task<long> OperationCount(int userId) =>
            ScalarAsync($"SELECT count(*) FROM \"SessionCreateOperations\" WHERE \"UserId\" = {userId}");

        private static HttpContent Keyed(Guid key) =>
            JsonContent.Create(new { date = "2026-09-03", name = "S", status = "draft", clientOperationId = key });

        // ---- 25 --------------------------------------------------------------------

        [DockerRequiredFact]
        public async Task OverLimitKeyedCreate_ProducesNoSessionAndNoOperationRow()
        {
            Assert.True(_pg.Available);
            using var factory = NewFactory(tokenLimit: 1);
            var userId = NextUserId();
            await SeedUserAsync(userId);
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionWriteRateLimitFactory.MintToken(userId));

            var ok = await client.PostAsync("/api/v1/sessions", Keyed(Guid.NewGuid()));
            Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
            Assert.Equal(1, await SessionCount(userId));
            Assert.Equal(1, await OperationCount(userId));

            var rejected = await client.PostAsync("/api/v1/sessions", Keyed(Guid.NewGuid()));
            Assert.Equal((HttpStatusCode)429, rejected.StatusCode);

            Assert.Equal(1, await SessionCount(userId));   // unchanged
            Assert.Equal(1, await OperationCount(userId)); // unchanged
        }

        // ---- 26 --------------------------------------------------------------------

        [DockerRequiredFact]
        public async Task ReplayNearExhaustion_IsEitherCleanReplayOrClean429_RowCountsUnchanged()
        {
            Assert.True(_pg.Available);
            var key = Guid.NewGuid();

            // (a) admitted replay (capacity for both calls) -> 200 with the original session.
            using (var roomy = NewFactory(tokenLimit: 5))
            {
                var userId = NextUserId();
                await SeedUserAsync(userId);
                var client = roomy.CreateClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", SessionWriteRateLimitFactory.MintToken(userId));

                var first = await client.PostAsync("/api/v1/sessions", Keyed(key));
                Assert.Equal(HttpStatusCode.Created, first.StatusCode);
                var firstId = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

                var replay = await client.PostAsync("/api/v1/sessions", Keyed(key));
                Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
                var replayId = (await replay.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
                Assert.Equal(firstId, replayId);

                Assert.Equal(1, await SessionCount(userId));
                Assert.Equal(1, await OperationCount(userId));
            }

            // (b) replay rejected by the drained limiter -> a clean 429, row counts unchanged.
            using (var drained = NewFactory(tokenLimit: 1))
            {
                var userId = NextUserId();
                await SeedUserAsync(userId);
                var client = drained.CreateClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", SessionWriteRateLimitFactory.MintToken(userId));
                var key2 = Guid.NewGuid();

                Assert.Equal(HttpStatusCode.Created,
                    (await client.PostAsync("/api/v1/sessions", Keyed(key2))).StatusCode);

                var replay = await client.PostAsync("/api/v1/sessions", Keyed(key2));
                Assert.Equal((HttpStatusCode)429, replay.StatusCode);
                Assert.Equal("{\"code\":\"rate_limited\"}", await replay.Content.ReadAsStringAsync());

                Assert.Equal(1, await SessionCount(userId));
                Assert.Equal(1, await OperationCount(userId));
            }
        }

        // ---- 27 --------------------------------------------------------------------

        [DockerRequiredFact]
        public async Task UserA_ExhaustingCapacity_DoesNotAffectUserB_DatabaseOperation()
        {
            Assert.True(_pg.Available);
            using var factory = NewFactory(tokenLimit: 1);
            var a = NextUserId();
            var b = NextUserId();
            await SeedUserAsync(a);
            await SeedUserAsync(b);

            var clientA = factory.CreateClient();
            clientA.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionWriteRateLimitFactory.MintToken(a));
            var clientB = factory.CreateClient();
            clientB.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionWriteRateLimitFactory.MintToken(b));

            Assert.Equal(HttpStatusCode.Created, (await clientA.PostAsync("/api/v1/sessions", Keyed(Guid.NewGuid()))).StatusCode);
            Assert.Equal((HttpStatusCode)429, (await clientA.PostAsync("/api/v1/sessions", Keyed(Guid.NewGuid()))).StatusCode);

            // B still writes for real.
            Assert.Equal(HttpStatusCode.Created, (await clientB.PostAsync("/api/v1/sessions", Keyed(Guid.NewGuid()))).StatusCode);

            Assert.Equal(1, await SessionCount(a));
            Assert.Equal(1, await OperationCount(a));
            Assert.Equal(1, await SessionCount(b));
            Assert.Equal(1, await OperationCount(b));
        }

        // ---- 28 --------------------------------------------------------------------

        [DockerRequiredFact]
        public async Task ConcurrentOverLimitRequests_NeverExceedAdmittedCount_NoPartialOperationRow()
        {
            Assert.True(_pg.Available);
            const int capacity = 3;
            const int fired = 20;
            using var factory = NewFactory(tokenLimit: capacity);
            var userId = NextUserId();
            await SeedUserAsync(userId);
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionWriteRateLimitFactory.MintToken(userId));

            var responses = await Task.WhenAll(Enumerable.Range(0, fired).Select(_ =>
                client.PostAsync("/api/v1/sessions", Keyed(Guid.NewGuid()))));

            var created = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
            var limited = responses.Count(r => (int)r.StatusCode == 429);

            // AutoReplenishment=false + a 3600s period => no token ever replenishes
            // during the test, so exactly `capacity` of the concurrent burst is admitted.
            Assert.Equal(capacity, created);
            Assert.Equal(fired, created + limited); // every response is 201 or 429, nothing else
            Assert.Equal(created, await SessionCount(userId));
            Assert.Equal(created, await OperationCount(userId)); // one op row per admitted create, no partials

            // No orphaned/incomplete operation rows.
            Assert.Equal(0, await ScalarAsync(
                $"SELECT count(*) FROM \"SessionCreateOperations\" WHERE \"UserId\" = {userId} AND \"CompletedAt\" IS NULL"));
        }
    }
}
