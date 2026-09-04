using System;
using System.Linq;
using System.Threading.Tasks;
using GoHardAPI.Data;
using GoHardAPI.Migrations;
using GoHardAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Runs the <b>real Npgsql branch</b> of
    /// <see cref="AddSessionCreateOperationAndClientOperationId"/> through EF's migration
    /// pipeline against a real PostgreSQL container, on top of a representative legacy schema,
    /// and verifies the deployed shape: <c>uuid</c> column, partial owner-scoped index,
    /// foreign keys with <c>ON DELETE SET NULL</c>, the EF history row, and coexistence of
    /// many legacy NULL rows.
    /// </summary>
    [Trait("Category", "PostgresIntegration")]
    public sealed class SessionCreateOperationMigrationPostgresTests : IAsyncLifetime
    {
        private const string MigrationId = PostgresFixture.MigrationId;

        private readonly Testcontainers.PostgreSql.PostgreSqlContainer _container =
            new Testcontainers.PostgreSql.PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();

        private bool _available;
        private string _cs = string.Empty;

        public async Task InitializeAsync()
        {
            try { await _container.StartAsync(); }
            catch (Exception ex)
            {
                // CI (REQUIRE_POSTGRES_TESTS=true): hard-fail rather than silently skip.
                PostgresRequirement.ThrowIfRequired(ex);
                _available = false;
                return;
            }
            _cs = _container.GetConnectionString();
            _available = true;
        }

        public async Task DisposeAsync()
        {
            try { await _container.DisposeAsync(); }
            catch { /* never started */ }
        }

        private TrainingContext NewContext() =>
            new(new DbContextOptionsBuilder<TrainingContext>().UseNpgsql(_cs).Options);

        private async Task ResetToLegacyBaselineAsync()
        {
            await using var c = new NpgsqlConnection(_cs);
            await c.OpenAsync();
            await Exec(c, "DROP SCHEMA public CASCADE; CREATE SCHEMA public;");
            await Exec(c, PostgresFixture.LegacyBaselineSql);

            await using var ctx = NewContext();
            foreach (var id in ctx.Database.GetMigrations().Where(m => m != MigrationId))
            {
                await Exec(c, $"INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ('{id}', '8.0.10');");
            }
        }

        private async Task MigrateAsync()
        {
            await using var ctx = NewContext();
            Assert.Equal(new[] { MigrationId }, ctx.Database.GetPendingMigrations().ToArray());
            await ctx.Database.MigrateAsync();
            Assert.Contains(MigrationId, await ctx.Database.GetAppliedMigrationsAsync());
        }

        // ---- 14: applies to a clean legacy schema, correct PG shape --------------------

        [DockerRequiredFact]
        public async Task Migrate_OnCleanLegacySchema_ProducesCorrectPostgresShape_AndHistoryRow()
        {
            Assert.True(_available);
            await ResetToLegacyBaselineAsync();
            await MigrateAsync();

            await using var c = new NpgsqlConnection(_cs);
            await c.OpenAsync();

            Assert.Equal("uuid", await Scalar(c,
                "SELECT data_type FROM information_schema.columns WHERE table_name='Sessions' AND column_name='ClientOperationId'"));
            Assert.Equal("YES", await Scalar(c,
                "SELECT is_nullable FROM information_schema.columns WHERE table_name='Sessions' AND column_name='ClientOperationId'"));

            Assert.Equal("uuid", await Scalar(c,
                "SELECT data_type FROM information_schema.columns WHERE table_name='SessionCreateOperations' AND column_name='ClientOperationId'"));
            Assert.Equal("timestamp without time zone", await Scalar(c,
                "SELECT data_type FROM information_schema.columns WHERE table_name='SessionCreateOperations' AND column_name='CreatedAt'"));

            // Partial, owner-scoped unique index on Sessions.
            var predicate = await Scalar(c, @"
                SELECT pg_get_expr(i.indpred, i.indrelid)
                FROM pg_index i JOIN pg_class c ON c.oid = i.indexrelid
                WHERE c.relname = 'IX_Sessions_UserId_ClientOperationId'");
            Assert.NotNull(predicate);
            Assert.Contains("\"ClientOperationId\" IS NOT NULL", predicate);
            Assert.True(await ScalarBool(c, @"
                SELECT i.indisunique
                FROM pg_index i JOIN pg_class c ON c.oid = i.indexrelid
                WHERE c.relname = 'IX_Sessions_UserId_ClientOperationId'"));

            // Foreign keys and their delete rules.
            Assert.Equal("SET NULL", await Scalar(c,
                "SELECT delete_rule FROM information_schema.referential_constraints WHERE constraint_name='FK_SessionCreateOperations_Sessions_SessionId'"));
            Assert.Equal("CASCADE", await Scalar(c,
                "SELECT delete_rule FROM information_schema.referential_constraints WHERE constraint_name='FK_SessionCreateOperations_Users_UserId'"));

            // History row written by EF, inside the migration transaction.
            Assert.Equal("1", await Scalar(c,
                $"SELECT count(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '{MigrationId}'"));
        }

        // ---- 14b: applies to a representative legacy schema WITH data, losing nothing ---

        [DockerRequiredFact]
        public async Task Migrate_OnLegacySchemaWithData_PreservesRows_AndLeavesNullKeys()
        {
            Assert.True(_available);
            await ResetToLegacyBaselineAsync();

            await using (var c = new NpgsqlConnection(_cs))
            {
                await c.OpenAsync();
                await Exec(c, "INSERT INTO \"Users\" (\"Id\",\"Name\",\"Email\") VALUES (1,'a','a@x.com'),(2,'b','b@x.com');");
                await Exec(c, "INSERT INTO \"Sessions\" (\"UserId\",\"Date\",\"Status\",\"Version\") VALUES (1, now(), 'draft', 1), (1, now(), 'completed', 3), (2, now(), 'draft', 1);");
            }

            await MigrateAsync();

            await using var conn = new NpgsqlConnection(_cs);
            await conn.OpenAsync();
            Assert.Equal("3", await Scalar(conn, "SELECT count(*) FROM \"Sessions\""));
            Assert.Equal("3", await Scalar(conn, "SELECT count(*) FROM \"Sessions\" WHERE \"ClientOperationId\" IS NULL"));
            Assert.Equal("2", await Scalar(conn, "SELECT count(*) FROM \"Users\""));
        }

        // ---- 15: multiple NULL legacy operation ids coexist ---------------------------

        [DockerRequiredFact]
        public async Task Migrate_ThenManyNullClientOperationIds_Coexist()
        {
            Assert.True(_available);
            await ResetToLegacyBaselineAsync();
            await MigrateAsync();

            await using var c = new NpgsqlConnection(_cs);
            await c.OpenAsync();
            await Exec(c, "INSERT INTO \"Users\" (\"Id\",\"Name\",\"Email\") VALUES (1,'a','a@x.com');");
            // Five NULL-key sessions for the same user — the partial unique index must allow it.
            await Exec(c, "INSERT INTO \"Sessions\" (\"UserId\",\"Date\",\"Status\",\"Version\") SELECT 1, now(), 'draft', 1 FROM generate_series(1,5);");
            Assert.Equal("5", await Scalar(c, "SELECT count(*) FROM \"Sessions\" WHERE \"UserId\"=1 AND \"ClientOperationId\" IS NULL"));
        }

        // ---- 16: the unique indexes are owner-scoped --------------------------------

        [DockerRequiredFact]
        public async Task Migrate_ThenUniqueIndexesAreOwnerScoped()
        {
            Assert.True(_available);
            await ResetToLegacyBaselineAsync();
            await MigrateAsync();

            var key = Guid.NewGuid();
            await using var c = new NpgsqlConnection(_cs);
            await c.OpenAsync();
            await Exec(c, "INSERT INTO \"Users\" (\"Id\",\"Name\",\"Email\") VALUES (1,'a','a@x.com'),(2,'b','b@x.com');");

            // Sessions.(UserId, ClientOperationId): same key under two owners is fine...
            await Exec(c, $"INSERT INTO \"Sessions\" (\"UserId\",\"Date\",\"Status\",\"Version\",\"ClientOperationId\") VALUES (1, now(), 'draft', 1, '{key}');");
            await Exec(c, $"INSERT INTO \"Sessions\" (\"UserId\",\"Date\",\"Status\",\"Version\",\"ClientOperationId\") VALUES (2, now(), 'draft', 1, '{key}');");
            // ...but the same (owner, key) twice is rejected.
            var dupSession = await Assert.ThrowsAsync<PostgresException>(() => Exec(c,
                $"INSERT INTO \"Sessions\" (\"UserId\",\"Date\",\"Status\",\"Version\",\"ClientOperationId\") VALUES (1, now(), 'draft', 1, '{key}');"));
            Assert.Equal("23505", dupSession.SqlState);

            // SessionCreateOperations.(UserId, ClientOperationId): same story.
            await Exec(c, $"INSERT INTO \"SessionCreateOperations\" (\"UserId\",\"ClientOperationId\",\"CreatedAt\") VALUES (1, '{key}', now());");
            await Exec(c, $"INSERT INTO \"SessionCreateOperations\" (\"UserId\",\"ClientOperationId\",\"CreatedAt\") VALUES (2, '{key}', now());");
            var dupOp = await Assert.ThrowsAsync<PostgresException>(() => Exec(c,
                $"INSERT INTO \"SessionCreateOperations\" (\"UserId\",\"ClientOperationId\",\"CreatedAt\") VALUES (1, '{key}', now());"));
            Assert.Equal("23505", dupOp.SqlState);
        }

        // The provider-script / Program.cs-no-duplication consistency checks are
        // provider-agnostic file assertions (no container). They live in
        // SessionCreateOperationScriptConsistencyTests so this class contains ONLY
        // PostgreSQL integration tests and the "Category=PostgresIntegration" filter
        // selects all and only real-provider tests.

        // ---- helpers -------------------------------------------------------------------

        private static async Task Exec(NpgsqlConnection c, string sql)
        {
            await using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task<string?> Scalar(NpgsqlConnection c, string sql)
        {
            await using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            var v = await cmd.ExecuteScalarAsync();
            return v is null or DBNull ? null : Convert.ToString(v);
        }

        private static async Task<bool> ScalarBool(NpgsqlConnection c, string sql)
        {
            await using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToBoolean(await cmd.ExecuteScalarAsync());
        }
    }
}
