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

        [DockerRequiredFact]
        public async Task Columns_ExistNullable_AndGuardedSqlIsReRunnable()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");
            await using var raw = new NpgsqlConnection(_pg.ConnectionString);
            await raw.OpenAsync();

            foreach (var c in new[] { "TargetSets", "TargetRepsMin", "TargetRepsMax" })
            {
                await using var check = raw.CreateCommand();
                check.CommandText = "SELECT is_nullable FROM information_schema.columns " +
                    $"WHERE table_name = 'Exercises' AND column_name = '{c}';";
                Assert.Equal("YES", (string?)await check.ExecuteScalarAsync());
            }

            await using (var again = raw.CreateCommand())
            {
                again.CommandText = ExerciseTargetsSql.NpgsqlUp; // guarded: no-op, no error
                await again.ExecuteNonQueryAsync();
            }
        }
    }
}
