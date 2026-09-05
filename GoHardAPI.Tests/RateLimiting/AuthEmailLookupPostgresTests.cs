using System;
using System.Threading;
using System.Threading.Tasks;
using GoHardAPI.Data;
using GoHardAPI.Models;
using GoHardAPI.RateLimiting;
using GoHardAPI.Repositories;
using GoHardAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace GoHardAPI.Tests.RateLimiting
{
    /// <summary>
    /// Proves, against real PostgreSQL 16 with the <b>production schema</b>
    /// (<c>TrainingContext.Database.EnsureCreated()</c> — the way the Railway database
    /// was built), the exact email-lookup equivalence that
    /// <see cref="AuthAttemptIdentity"/> must mirror:
    ///
    /// <list type="bullet">
    ///   <item><c>Users.Email</c> is <c>character varying(255)</c> with the database's
    ///     default (case-sensitive, whitespace-significant) collation and <b>no unique
    ///     index</b>;</item>
    ///   <item><c>IUserRepository.GetByEmailAsync</c> / <c>EmailExistsAsync</c> compile
    ///     to <c>WHERE "Email" = @p</c>, matching only the byte-identical string;</item>
    ///   <item>two accounts differing only in email case can coexist, and each is a
    ///     distinct rate-limit partition.</item>
    /// </list>
    ///
    /// Shares the one <see cref="PostgresFixture"/> container (via
    /// <see cref="PostgresCollection"/>) — a dedicated database inside it holds the
    /// EnsureCreated production schema, built once.
    /// </summary>
    [Collection(PostgresCollection.Name)]
    [Trait("Category", "PostgresIntegration")]
    public sealed class AuthEmailLookupPostgresTests
    {
        private readonly PostgresFixture _pg;
        private static readonly SemaphoreSlim BuildGate = new(1, 1);
        private static string? _dbConnectionString;

        public AuthEmailLookupPostgresTests(PostgresFixture pg) => _pg = pg;

        private async Task<string> ProductionSchemaDbAsync()
        {
            if (_dbConnectionString is { } existing)
            {
                return existing;
            }

            await BuildGate.WaitAsync();
            try
            {
                if (_dbConnectionString is { } made)
                {
                    return made;
                }

                const string dbName = "authlookup_prodschema";
                await using (var admin = new NpgsqlConnection(_pg.ConnectionString))
                {
                    await admin.OpenAsync();
                    await using var cmd = admin.CreateCommand();
                    cmd.CommandText =
                        $"DROP DATABASE IF EXISTS {dbName} WITH (FORCE); CREATE DATABASE {dbName};";
                    await cmd.ExecuteNonQueryAsync();
                }

                var csb = new NpgsqlConnectionStringBuilder(_pg.ConnectionString) { Database = dbName };
                _dbConnectionString = csb.ConnectionString;

                await using var ctx = NewContext(_dbConnectionString);
                await ctx.Database.EnsureCreatedAsync();
                return _dbConnectionString;
            }
            finally
            {
                BuildGate.Release();
            }
        }

        private static TrainingContext NewContext(string cs) =>
            new(new DbContextOptionsBuilder<TrainingContext>().UseNpgsql(cs).Options);

        private static User NewUser(string email) => new()
        {
            Name = "n",
            Username = "u" + Guid.NewGuid().ToString("N")[..12],
            Email = email,
            PasswordHash = "x",
            IsActive = true,
        };

        // ---- the schema itself --------------------------------------------------

        [DockerRequiredFact]
        public async Task Email_Column_IsCaseSensitiveVarchar_WithNoUniqueIndex()
        {
            Assert.True(_pg.Available);
            var cs = await ProductionSchemaDbAsync();

            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync();

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT data_type, character_maximum_length
                    FROM information_schema.columns
                    WHERE table_schema = 'public' AND table_name = 'Users' AND column_name = 'Email'";
                await using var r = await cmd.ExecuteReaderAsync();
                Assert.True(await r.ReadAsync());
                Assert.Equal("character varying", r.GetString(0));
                Assert.Equal(255, r.GetInt32(1));
            }

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT count(*)
                    FROM pg_indexes
                    WHERE schemaname = 'public' AND tablename = 'Users'
                      AND indexdef ILIKE '%unique%' AND indexdef ILIKE '%(""Email"")%'";
                Assert.Equal(0L, Convert.ToInt64(await cmd.ExecuteScalarAsync()));
            }

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT
                      ('Case@Example.com'::varchar(255) = 'case@example.com') AS case_folds,
                      ('case@example.com'::varchar(255) = 'case@example.com ') AS trims";
                await using var r = await cmd.ExecuteReaderAsync();
                Assert.True(await r.ReadAsync());
                Assert.False(r.GetBoolean(0)); // "=" does NOT fold case
                Assert.False(r.GetBoolean(1)); // "=" does NOT trim trailing space
            }
        }

        // ---- GetByEmailAsync / EmailExistsAsync semantics ---------------------

        [DockerRequiredFact]
        public async Task TwoCaseVariantAccounts_Coexist_AndEachLookupIsExact()
        {
            Assert.True(_pg.Available);
            var cs = await ProductionSchemaDbAsync();

            var upperEmail = "Case-" + Guid.NewGuid().ToString("N")[..8] + "@Example.com";
            var lowerEmail = upperEmail.ToLowerInvariant();

            int upperId, lowerId;
            await using (var ctx = NewContext(cs))
            {
                var upper = NewUser(upperEmail);
                var lower = NewUser(lowerEmail);
                ctx.Users.AddRange(upper, lower);
                await ctx.SaveChangesAsync(); // both insert -> no case-insensitive unique constraint
                upperId = upper.Id;
                lowerId = lower.Id;
            }
            Assert.NotEqual(upperId, lowerId);

            await using (var ctx = NewContext(cs))
            {
                var repo = new UserRepository(ctx);

                Assert.Equal(upperId, (await repo.GetByEmailAsync(upperEmail))!.Id);
                Assert.Equal(lowerId, (await repo.GetByEmailAsync(lowerEmail))!.Id);

                Assert.Null(await repo.GetByEmailAsync(upperEmail.ToUpperInvariant())); // case-different
                Assert.Null(await repo.GetByEmailAsync(" " + lowerEmail));              // leading space
                Assert.Null(await repo.GetByEmailAsync(lowerEmail + " "));              // trailing space

                Assert.True(await repo.EmailExistsAsync(lowerEmail));
                Assert.False(await repo.EmailExistsAsync(upperEmail.ToUpperInvariant()));
            }
        }

        // ---- the limiter key derivation matches the proven DB semantics -------

        [DockerRequiredFact]
        public async Task LimiterPartitionKey_Matches_ThatExactLookupEquivalence()
        {
            Assert.True(_pg.Available);
            var cs = await ProductionSchemaDbAsync();

            await using var ctx = NewContext(cs);
            var repo = new UserRepository(ctx);

            const string upper = "Nobody@Example.com";
            const string lower = "nobody@example.com";

            // the compiled lookup is a raw case-sensitive "=" — no lower()/upper()/citext/COLLATE
            var sql = ctx.Users.Where(u => u.Email == lower).ToQueryString();
            Assert.Contains("WHERE u.\"Email\" = ", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("lower(", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("upper(", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("citext", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("collate", sql, StringComparison.OrdinalIgnoreCase);

            // DB treats these as different (nonexistent) rows; the limiter must too.
            Assert.Null(await repo.GetByEmailAsync(upper));
            Assert.Null(await repo.GetByEmailAsync(lower));
            Assert.NotEqual(
                AuthAttemptIdentity.PartitionKey(AuthAttemptIdentity.LoginScope, upper),
                AuthAttemptIdentity.PartitionKey(AuthAttemptIdentity.LoginScope, lower));

            // identical string -> one row, one key
            Assert.Equal(
                AuthAttemptIdentity.PartitionKey(AuthAttemptIdentity.LoginScope, lower),
                AuthAttemptIdentity.PartitionKey(AuthAttemptIdentity.LoginScope, lower));

            // whitespace-different -> different row, different key
            Assert.NotEqual(
                AuthAttemptIdentity.PartitionKey(AuthAttemptIdentity.LoginScope, lower),
                AuthAttemptIdentity.PartitionKey(AuthAttemptIdentity.LoginScope, " " + lower));
        }
    }
}
