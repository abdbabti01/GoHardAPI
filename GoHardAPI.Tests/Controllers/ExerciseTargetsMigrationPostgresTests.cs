using GoHardAPI.Migrations;
using GoHardAPI.Tests.Infrastructure;
using Npgsql;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    [Collection(ProgramWorkoutOccurrenceKeyPostgresCollection.Name)]
    [Trait("Category", "PostgresIntegration")]
    public sealed class ExerciseTargetsMigrationPostgresTests
    {
        private readonly ProgramWorkoutOccurrenceKeyPostgresFixture _pg;
        public ExerciseTargetsMigrationPostgresTests(ProgramWorkoutOccurrenceKeyPostgresFixture pg) => _pg = pg;

        // The shared fixture's Database.Migrate() only ever really executes AddExerciseOccurrenceKey's
        // Up() (every other migration, including this one, is pre-seeded into __EFMigrationsHistory as
        // already-applied without its DDL running) — so AddExerciseTargets's guarded SQL is exercised
        // directly against the real container here instead of relying on the fixture to have run it.
        [DockerRequiredFact]
        public async Task ProviderSql_UpDown_AreGuardedAndReRunnable_OnRealPostgres()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");
            await using var raw = new NpgsqlConnection(_pg.ConnectionString);
            await raw.OpenAsync();

            var columns = new[] { "TargetSets", "TargetRepsMin", "TargetRepsMax" };

            // a. Up is guarded: running it twice must not throw, and all three columns must exist nullable.
            await Exec(raw, ExerciseTargetsSql.NpgsqlUp);
            await Exec(raw, ExerciseTargetsSql.NpgsqlUp);

            foreach (var c in columns)
            {
                await using var check = raw.CreateCommand();
                check.CommandText = "SELECT is_nullable FROM information_schema.columns " +
                    $"WHERE table_name = 'Exercises' AND column_name = '{c}';";
                Assert.Equal("YES", (string?)await check.ExecuteScalarAsync());
            }

            // b. Down is guarded: running it twice must not throw, and none of the columns remain.
            await Exec(raw, ExerciseTargetsSql.NpgsqlDown);
            await Exec(raw, ExerciseTargetsSql.NpgsqlDown);

            await using (var count = raw.CreateCommand())
            {
                count.CommandText = "SELECT COUNT(*) FROM information_schema.columns " +
                    "WHERE table_name = 'Exercises' AND column_name IN ('TargetSets','TargetRepsMin','TargetRepsMax');";
                Assert.Equal(0L, (long)(await count.ExecuteScalarAsync() ?? 0L));
            }

            // c. Leave the shared fixture DB with the columns present, matching the EF model it now includes.
            await Exec(raw, ExerciseTargetsSql.NpgsqlUp);
        }

        private static async Task Exec(NpgsqlConnection conn, string sql)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
