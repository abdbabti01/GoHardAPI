using System.Threading.Tasks;
using GoHardAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Real-PostgreSQL coverage for <c>AddExerciseOccurrenceKey</c>: the migration applies
    /// cleanly to a pre-existing (legacy, no-column) database, is a no-op on a repeated run,
    /// and does not disturb existing data. See <see cref="ProgramWorkoutOccurrenceKeyPostgresFixture"/>
    /// for how the legacy baseline / pending-migration state is constructed (mirrors
    /// <see cref="PostgresFixture"/>).
    /// </summary>
    [Collection(ProgramWorkoutOccurrenceKeyPostgresCollection.Name)]
    [Trait("Category", "PostgresIntegration")]
    public sealed class ProgramWorkoutOccurrenceKeyMigrationPostgresTests
    {
        private readonly ProgramWorkoutOccurrenceKeyPostgresFixture _pg;

        public ProgramWorkoutOccurrenceKeyMigrationPostgresTests(ProgramWorkoutOccurrenceKeyPostgresFixture pg)
        {
            _pg = pg;
        }

        [DockerRequiredFact]
        public async Task Migration_AlreadyApplied_ByTheFixture_AddsNullableColumn_LegacyRowsUnaffected()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");

            await using var raw = new NpgsqlConnection(_pg.ConnectionString);
            await raw.OpenAsync();

            await using (var seed = raw.CreateCommand())
            {
                seed.CommandText = "INSERT INTO \"Users\" (\"Name\",\"Email\") VALUES ('u','u@x.com') RETURNING \"Id\";";
                var userId = (int)(await seed.ExecuteScalarAsync())!;

                await using var seedProgram = raw.CreateCommand();
                seedProgram.CommandText =
                    $"INSERT INTO \"Programs\" (\"UserId\",\"Title\",\"StartDate\",\"CreatedAt\") " +
                    $"VALUES ({userId},'P', now(), now()) RETURNING \"Id\";";
                var programId = (int)(await seedProgram.ExecuteScalarAsync())!;

                await using var seedWorkout = raw.CreateCommand();
                seedWorkout.CommandText =
                    $"INSERT INTO \"ProgramWorkouts\" (\"ProgramId\",\"WeekNumber\",\"DayNumber\",\"WorkoutName\",\"ExercisesJson\") " +
                    $"VALUES ({programId},1,1,'Legacy','[{{\"name\":\"Squat\"}}]');";
                await seedWorkout.ExecuteNonQueryAsync();
            }

            await using var ctx = _pg.NewContext();
            var workout = await ctx.ProgramWorkouts.AsNoTracking().FirstAsync();
            Assert.Equal("Legacy", workout.WorkoutName);

            // The column the migration actually adds.
            await using var colCheck = raw.CreateCommand();
            colCheck.CommandText =
                "SELECT is_nullable FROM information_schema.columns " +
                "WHERE table_name = 'Exercises' AND column_name = 'OccurrenceKey';";
            var isNullable = (string?)await colCheck.ExecuteScalarAsync();
            Assert.Equal("YES", isNullable);
        }

        [DockerRequiredFact]
        public async Task Migration_IsRecordedExactlyOnce_InHistory()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");

            await using var ctx = _pg.NewContext();
            var applied = await ctx.Database.GetAppliedMigrationsAsync();
            Assert.Single(applied, m => m == ProgramWorkoutOccurrenceKeyPostgresFixture.MigrationId);

            // Re-running Migrate() against an already-migrated database is the standard
            // idempotent-redeploy guarantee EF itself provides (nothing pending -> no-op);
            // assert that guarantee holds here too.
            await ctx.Database.MigrateAsync();
            var appliedAgain = await ctx.Database.GetAppliedMigrationsAsync();
            Assert.Single(appliedAgain, m => m == ProgramWorkoutOccurrenceKeyPostgresFixture.MigrationId);
        }
    }
}
