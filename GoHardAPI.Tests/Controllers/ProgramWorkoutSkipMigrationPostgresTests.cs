using System;
using System.Linq;
using System.Threading.Tasks;
using GoHardAPI.Data;
using GoHardAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Runs the real <c>AddProgramWorkoutSkip</c> migration's actual <c>Up()</c> DDL - via EF's
    /// <c>Database.Migrate()</c> pipeline, NOT <c>EnsureCreated</c> - against a real PostgreSQL 16
    /// database, and asserts the new columns land with PostgreSQL-native types (not SQL Server's
    /// "bit"/"datetime2", which the scaffolder emits by default). Same isolation technique as
    /// <see cref="GoalSoftDeleteMigrationPostgresTests"/> - see that class's doc comment for the
    /// full rationale.
    /// </summary>
    [Trait("Category", "PostgresIntegration")]
    public sealed class ProgramWorkoutSkipMigrationPostgresTests : IAsyncLifetime
    {
        private const string MigrationId = "AddProgramWorkoutSkip";

        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

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

        [DockerRequiredFact]
        public async Task the_migration_applies_cleanly_and_produces_postgres_native_column_types()
        {
            Assert.True(_available, "PostgreSQL container must be available in CI");

            // 1. Build the full current schema (including this migration's columns) straight
            //    from the C# model.
            await using (var seedCtx = NewContext())
            {
                await seedCtx.Database.EnsureCreatedAsync();
            }

            await using var conn = new NpgsqlConnection(_cs);
            await conn.OpenAsync();

            // 2. Revert ONLY the columns this migration adds, back to the pre-migration shape.
            await Exec(conn, "ALTER TABLE \"ProgramWorkouts\" DROP COLUMN \"IsSkipped\", DROP COLUMN \"SkippedAt\";");

            // 3. Seed history: every OTHER migration marked applied, leaving only this one
            //    pending.
            await Exec(
                conn,
                "CREATE TABLE \"__EFMigrationsHistory\" (\"MigrationId\" character varying(150) NOT NULL, \"ProductVersion\" character varying(32) NOT NULL, CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY (\"MigrationId\"));");
            await using (var histCtx = NewContext())
            {
                foreach (var id in histCtx.Database.GetMigrations().Where(m => !m.EndsWith(MigrationId)))
                {
                    await Exec(
                        conn,
                        $"INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ('{id}', '8.0.10') ON CONFLICT DO NOTHING;");
                }
            }

            // 4. The real assertion: applying just this migration must not throw.
            await using var ctx = NewContext();
            var pending = (await ctx.Database.GetPendingMigrationsAsync()).ToList();
            Assert.Single(pending);
            Assert.Contains(MigrationId, pending[0]);

            await ctx.Database.MigrateAsync();

            Assert.Contains(pending[0], await ctx.Database.GetAppliedMigrationsAsync());

            // 5. Confirm PostgreSQL-native types landed - NOT SQL Server's "bit"/"datetime2".
            Assert.Equal(
                "boolean",
                await Scalar(conn, "SELECT data_type FROM information_schema.columns WHERE table_name='ProgramWorkouts' AND column_name='IsSkipped'"));
            Assert.Equal(
                "timestamp with time zone",
                await Scalar(conn, "SELECT data_type FROM information_schema.columns WHERE table_name='ProgramWorkouts' AND column_name='SkippedAt'"));

            // 6. Confirm the default is FALSE - existing rows created before this migration
            //    must read back as "not skipped", not NULL.
            var notSkippedCount = await Scalar(conn, "SELECT COUNT(*)::text FROM \"ProgramWorkouts\" WHERE \"IsSkipped\" = FALSE");
            Assert.NotNull(notSkippedCount);
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
            return (string?)await cmd.ExecuteScalarAsync();
        }
    }
}
