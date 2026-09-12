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
    /// Runs the real <c>AddGoalArchiveAndMealPlanSourceIdentity</c> migration's actual
    /// <c>Up()</c> DDL — via EF's <c>Database.Migrate()</c> pipeline, NOT <c>EnsureCreated</c>
    /// — against a real PostgreSQL 16 database, and asserts the new columns land with
    /// PostgreSQL-native types.
    ///
    /// This exists to catch a class of bug an `EnsureCreated`-based test (the rest of this
    /// project's Postgres suite) structurally cannot: a migration's <c>Up()</c> that embeds
    /// an explicit, provider-specific column <c>type:</c> string (e.g. SQL Server's
    /// <c>"datetime2"</c> / <c>"bit"</c>, which is what `dotnet ef migrations add` emits by
    /// default in this repo, since <see cref="Data.DesignTimeDbContextFactory"/> defaults to
    /// SQL Server unless <c>DATABASE_URL</c> is set) is used VERBATIM by EF's SQL generator
    /// for whichever provider actually executes it - it is not translated - so such a
    /// migration fails outright against PostgreSQL even though `EnsureCreated` (which builds
    /// DDL straight from the C# model, bypassing the migration files entirely) looks fine.
    ///
    /// Note: the full historical migration chain (`InitialCreate` onward) does NOT apply
    /// cleanly to a fresh PostgreSQL database from empty - several pre-existing migrations
    /// (predating this change) also embed SQL-Server-only types (see e.g.
    /// `20251227014922_InitialCreate.cs`, `20251229203251_AddExerciseSetAndAuth.cs`) and are
    /// production technical debt outside this task's scope (CLAUDE.md explicitly quarantines
    /// `Program.cs`'s startup schema-patching behavior and forbids extending or "fixing" it
    /// incidentally). This test therefore isolates just the ONE migration under test: it
    /// starts from a real Postgres schema built via `EnsureCreated` (proven elsewhere in this
    /// suite to produce correct, PostgreSQL-native DDL from the current model), manually
    /// reverts only the two tables this migration touches to their pre-migration shape, seeds
    /// `__EFMigrationsHistory` with every OTHER migration marked applied, and then migrates -
    /// mirroring the same "legacy baseline + single pending migration" technique already
    /// established by `SessionCreateOperationMigrationPostgresTests` /
    /// `ExerciseOccurrenceKeyMigrationTests` / `WorkoutTemplatesMigrationTests` for this exact
    /// reason.
    /// </summary>
    [Trait("Category", "PostgresIntegration")]
    public sealed class GoalArchiveMealPlanMigrationPostgresTests : IAsyncLifetime
    {
        private const string MigrationId = "AddGoalArchiveAndMealPlanSourceIdentity";

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
            //    from the C# model - already proven elsewhere in this suite to produce
            //    correct PostgreSQL-native DDL, since it goes through Npgsql's own type
            //    mapper rather than any migration file's (possibly hardcoded) `type:` string.
            await using (var seedCtx = NewContext())
            {
                await seedCtx.Database.EnsureCreatedAsync();
            }

            await using var conn = new NpgsqlConnection(_cs);
            await conn.OpenAsync();

            // 2. Revert ONLY the two tables this migration touches to their pre-migration
            //    shape, so the migration under test has real work to do.
            await Exec(conn, "ALTER TABLE \"Goals\" DROP COLUMN \"IsArchived\", DROP COLUMN \"ArchivedAt\";");
            await Exec(conn, "DROP INDEX IF EXISTS \"IX_Goals_UserId_IsArchived\";");
            await Exec(conn, "ALTER TABLE \"FoodItems\" DROP CONSTRAINT \"FK_FoodItems_ChatConversations_SourcePlanConversationId\";");
            await Exec(conn, "DROP INDEX IF EXISTS \"IX_FoodItems_SourcePlanConversationId\";");
            await Exec(conn, "DROP INDEX IF EXISTS \"IX_FoodItems_MealEntryId_SourcePlanConversationId_SourcePlanDay\";");
            await Exec(conn, "ALTER TABLE \"FoodItems\" DROP COLUMN \"SourcePlanConversationId\", DROP COLUMN \"SourcePlanDay\";");
            // The pre-migration index this migration's Down() would restore.
            await Exec(conn, "CREATE INDEX \"IX_FoodItems_MealEntryId\" ON \"FoodItems\" (\"MealEntryId\");");

            // 3. Seed history: every OTHER migration marked applied (matching this repo's
            //    established pattern for isolating one migration from the rest of the chain),
            //    leaving only this one pending. EnsureCreated doesn't create the EF history
            //    table (it has no concept of migrations), so create it explicitly first.
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

            // 5. Confirm PostgreSQL-native types landed - NOT SQL Server's "datetime2"/"bit".
            Assert.Equal(
                "boolean",
                await Scalar(conn, "SELECT data_type FROM information_schema.columns WHERE table_name='Goals' AND column_name='IsArchived'"));
            Assert.Equal(
                "timestamp with time zone",
                await Scalar(conn, "SELECT data_type FROM information_schema.columns WHERE table_name='Goals' AND column_name='ArchivedAt'"));
            Assert.Equal(
                "integer",
                await Scalar(conn, "SELECT data_type FROM information_schema.columns WHERE table_name='FoodItems' AND column_name='SourcePlanConversationId'"));
            Assert.Equal(
                "integer",
                await Scalar(conn, "SELECT data_type FROM information_schema.columns WHERE table_name='FoodItems' AND column_name='SourcePlanDay'"));
            Assert.Equal(
                "SET NULL",
                await Scalar(conn, "SELECT delete_rule FROM information_schema.referential_constraints WHERE constraint_name='FK_FoodItems_ChatConversations_SourcePlanConversationId'"));
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
