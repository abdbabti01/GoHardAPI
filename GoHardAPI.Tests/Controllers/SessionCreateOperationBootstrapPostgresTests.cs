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
    /// Exercises the startup history-stamp + <c>Migrate()</c> sequence on a real PostgreSQL
    /// container - not <c>Database.Migrate()</c> in isolation - for the ONE concern this
    /// branch owns: whether the keyed-Session-CREATE migration
    /// (<c>20260903195625</c>) gets its DDL executed rather than blind-stamped.
    ///
    /// <c>Program.cs</c>, when <c>__EFMigrationsHistory</c> is empty, stamps the
    /// pre-existing migrations as applied via
    /// <see cref="MigrationBootstrap.MigrationsToPreStamp"/> (which now EXCLUDES
    /// <c>20260903195625</c>) and then runs <c>context.Database.Migrate()</c>.
    ///
    /// SCOPE / KNOWN LIMITATION: the baseline here is <c>Users</c> + <c>Sessions</c> +
    /// <c>__EFMigrationsHistory</c> only, and <see cref="RunStartupBootstrapAsync"/> also
    /// stamps the <c>AddProgram*</c> / <c>202601092*</c> ids (which <c>MigrationsToPreStamp</c>
    /// leaves pending). It therefore proves the realistic PRODUCTION path (populated history,
    /// or an older pre-<c>Programs</c> EnsureCreated schema): <c>Migrate()</c> applies
    /// <c>20260903195625</c> exactly once with the correct shape. It does NOT prove an
    /// absolutely-cold-start database boots end to end - on a truly empty schema
    /// <c>Migrate()</c> would first hit <c>20260109204500_AddProgramTablesOnly</c> (no
    /// <c>IF NOT EXISTS</c>, references <c>Goals</c>/<c>Users</c>) and fail before reaching
    /// this migration. That cold-start fragility is PRE-EXISTING (origin/main's inline filter
    /// already excludes <c>AddProgram*</c>/<c>202601092*</c>) and out of this branch's scope.
    /// </summary>
    [Trait("Category", "PostgresIntegration")]
    public sealed class SessionCreateOperationBootstrapPostgresTests : IAsyncLifetime
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
        }

        /// <summary>
        /// Reproduces the Program.cs bootstrap for THIS migration's concern: clean a
        /// known-broken entry, then (only when history is empty) stamp the pre-existing
        /// migrations via <see cref="MigrationBootstrap.MigrationsToPreStamp"/>, then run
        /// <c>Migrate()</c>.
        ///
        /// It additionally stamps the <c>AddProgram*</c> / <c>202601092*</c> ids that
        /// <c>MigrationsToPreStamp</c> leaves pending. In real production those either are
        /// already applied (populated history) or their tables already exist (older
        /// EnsureCreated schema); stamping them here keeps this test focused on
        /// <c>20260903195625</c> instead of the pre-existing cold-start fragility of the
        /// <c>AddProgram*</c> chain (see the class-level SCOPE note).
        /// </summary>
        private async Task RunStartupBootstrapAsync()
        {
            await using var ctx = NewContext();

            try
            {
                await ctx.Database.ExecuteSqlRawAsync(
                    "DELETE FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" LIKE '20260109202221_%'");
            }
            catch { /* history table may not exist yet */ }

            var pending = ctx.Database.GetPendingMigrations().ToList();
            var applied = ctx.Database.GetAppliedMigrations().ToList();

            if (applied.Count == 0 && pending.Count > 0)
            {
                var stamp = MigrationBootstrap.MigrationsToPreStamp(pending)
                    .Concat(pending.Where(m =>
                        m.Contains("AddProgram", StringComparison.Ordinal) ||
                        m.StartsWith("202601092", StringComparison.Ordinal)))
                    .Distinct();

                foreach (var id in stamp)
                {
                    try
                    {
                        await ctx.Database.ExecuteSqlRawAsync(
                            "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1})",
                            id, "8.0.10");
                    }
                    catch { /* already present */ }
                }
            }

            await ctx.Database.MigrateAsync();
        }

        private async Task StampAllButTheKeyedCreateMigrationAsync()
        {
            await using var ctx = NewContext();
            foreach (var id in ctx.Database.GetMigrations().Where(m => m != MigrationId))
            {
                await ctx.Database.ExecuteSqlRawAsync(
                    "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1})",
                    id, "8.0.10");
            }
        }

        // ---- 1. current production-like history (everything but the new migration applied) --

        [DockerRequiredFact]
        public async Task Bootstrap_FromProductionLikeHistory_AppliesTheMigrationOnce_WithSchemaAndHistory()
        {
            Assert.True(_available);
            await ResetToLegacyBaselineAsync();
            await StampAllButTheKeyedCreateMigrationAsync();

            await using (var ctx = NewContext())
            {
                Assert.Equal(new[] { MigrationId }, ctx.Database.GetPendingMigrations().ToArray());
            }

            await RunStartupBootstrapAsync();

            await AssertDeployedShapeAsync(expectHistoryRows: 1);
        }

        // ---- 2. empty migration history (pre-Programs EnsureCreated / populated-then-cleared) ----

        [DockerRequiredFact]
        public async Task Bootstrap_FromEmptyHistory_LeavesTheMigrationPending_ThenMigrateRunsItsDdl()
        {
            Assert.True(_available);
            await ResetToLegacyBaselineAsync(); // __EFMigrationsHistory exists but is empty

            // The fix: the keyed-Session-CREATE migration is NOT in the blind-stamp set...
            await using (var ctx = NewContext())
            {
                var pending = ctx.Database.GetPendingMigrations().ToList();
                Assert.Contains(MigrationId, pending);
                Assert.DoesNotContain(MigrationId, MigrationBootstrap.MigrationsToPreStamp(pending));
            }

            await RunStartupBootstrapAsync();

            // ...so Migrate() actually executed its DDL. Schema + history must be complete.
            await AssertDeployedShapeAsync(expectHistoryRows: 1);
        }

        // ---- 3. repeated startup ---------------------------------------------------------

        [DockerRequiredFact]
        public async Task Bootstrap_RepeatedStartup_IsIdempotent_NoBootFailure_OneHistoryRow()
        {
            Assert.True(_available);
            await ResetToLegacyBaselineAsync();

            await RunStartupBootstrapAsync();
            await RunStartupBootstrapAsync(); // second boot
            await RunStartupBootstrapAsync(); // third boot

            await AssertDeployedShapeAsync(expectHistoryRows: 1);
        }

        // ---- shared schema assertions -------------------------------------------------------

        private async Task AssertDeployedShapeAsync(int expectHistoryRows)
        {
            await using var c = new NpgsqlConnection(_cs);
            await c.OpenAsync();

            Assert.Equal("uuid", await Scalar(c,
                "SELECT data_type FROM information_schema.columns WHERE table_name='Sessions' AND column_name='ClientOperationId'"));
            Assert.Equal("1", await Scalar(c,
                "SELECT count(*) FROM information_schema.tables WHERE table_name='SessionCreateOperations'"));

            var predicate = await Scalar(c, @"
                SELECT pg_get_expr(i.indpred, i.indrelid)
                FROM pg_index i JOIN pg_class cl ON cl.oid = i.indexrelid
                WHERE cl.relname = 'IX_Sessions_UserId_ClientOperationId'");
            Assert.NotNull(predicate);
            Assert.Contains("\"ClientOperationId\" IS NOT NULL", predicate);

            Assert.Equal("1", await Scalar(c,
                "SELECT count(*) FROM pg_indexes WHERE indexname='IX_SessionCreateOperations_UserId_ClientOperationId'"));
            Assert.Equal("1", await Scalar(c,
                "SELECT count(*) FROM pg_indexes WHERE indexname='IX_SessionCreateOperations_SessionId'"));

            Assert.Equal("SET NULL", await Scalar(c,
                "SELECT delete_rule FROM information_schema.referential_constraints WHERE constraint_name='FK_SessionCreateOperations_Sessions_SessionId'"));
            Assert.Equal("CASCADE", await Scalar(c,
                "SELECT delete_rule FROM information_schema.referential_constraints WHERE constraint_name='FK_SessionCreateOperations_Users_UserId'"));

            Assert.Equal(expectHistoryRows.ToString(), await Scalar(c,
                $"SELECT count(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '{MigrationId}'"));

            // The table is actually usable.
            await Exec(c, "INSERT INTO \"Users\" (\"Id\",\"Name\",\"Email\") VALUES (1,'a','a@x.com') ON CONFLICT DO NOTHING;");
            await Exec(c,
                "INSERT INTO \"SessionCreateOperations\" (\"UserId\",\"ClientOperationId\",\"CreatedAt\") VALUES (1, gen_random_uuid(), now());");
        }

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
    }
}
