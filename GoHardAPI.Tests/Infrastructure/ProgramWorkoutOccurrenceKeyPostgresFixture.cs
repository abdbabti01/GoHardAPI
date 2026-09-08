using System;
using System.Linq;
using System.Threading.Tasks;
using GoHardAPI.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace GoHardAPI.Tests.Infrastructure
{
    /// <summary>
    /// Boots one real PostgreSQL 16 container for the exercise-occurrence-key test suite, kept
    /// entirely separate from <see cref="PostgresFixture"/> (which targets exactly the
    /// <c>AddSessionCreateOperationAndClientOperationId</c> migration and seeds every other
    /// migration, including this one, as already-applied — reusing it here would apply this
    /// column's DDL without the fixture's own dedicated coverage). Same recipe:
    ///
    /// <list type="number">
    ///   <item>hand-create a representative <b>legacy</b> schema — <c>Users</c>,
    ///     <c>Programs</c>, <c>ProgramWorkouts</c> and <c>Exercises</c> with the full current
    ///     column set <b>except</b> <c>Exercises.OccurrenceKey</c>;</item>
    ///   <item>seed <c>__EFMigrationsHistory</c> with every migration id <b>except</b>
    ///     <see cref="MigrationId"/>, leaving exactly that one pending;</item>
    ///   <item>run <c>Database.Migrate()</c> — EF executes the real Npgsql branch of
    ///     <c>AddExerciseOccurrenceKey</c> inside its own transaction and writes the history
    ///     row.</item>
    /// </list>
    ///
    /// If Docker is not reachable the fixture constructs but <see cref="Available"/> is
    /// <c>false</c>; <see cref="DockerRequiredFactAttribute"/> skips the tests.
    /// </summary>
    public sealed class ProgramWorkoutOccurrenceKeyPostgresFixture : IAsyncLifetime
    {
        public const string MigrationId = "20260908162606_AddExerciseOccurrenceKey";

        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

        public bool Available { get; private set; }
        public string ConnectionString { get; private set; } = string.Empty;

        public TrainingContext NewContext() =>
            new(new DbContextOptionsBuilder<TrainingContext>().UseNpgsql(ConnectionString).Options);

        public async Task InitializeAsync()
        {
            try
            {
                await _container.StartAsync();
            }
            catch (Exception ex)
            {
                PostgresRequirement.ThrowIfRequired(ex);
                Available = false;
                return;
            }

            ConnectionString = _container.GetConnectionString();

            await using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                await Exec(conn, LegacyBaselineSql);

                await using var ctx = NewContext();
                var pending = ctx.Database.GetMigrations().Where(m => m != MigrationId);
                foreach (var id in pending)
                {
                    await Exec(conn,
                        $"INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ('{id}', '8.0.10');");
                }
            }

            await using (var ctx = NewContext())
            {
                Assert.Equal(new[] { MigrationId }, ctx.Database.GetPendingMigrations().ToArray());
                await ctx.Database.MigrateAsync();
            }

            Available = true;
        }

        public async Task DisposeAsync()
        {
            try { await _container.DisposeAsync(); }
            catch { /* container may never have started */ }
        }

        private static async Task Exec(NpgsqlConnection conn, string sql)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Representative pre-migration schema: Users, Programs, ProgramWorkouts and a legacy
        /// Exercises table (the migration's ALTER TABLE "Exercises" ADD COLUMN requires the
        /// table to already exist) with the full current column set minus OccurrenceKey.
        /// Exercises.SessionId carries no FK here (no Sessions table — unneeded for this
        /// suite, matching the "only what this suite touches" convention already used by
        /// PostgresFixture.LegacyBaselineSql).
        /// </summary>
        internal const string LegacyBaselineSql = @"
CREATE TABLE ""Users"" (
    ""Id"" serial PRIMARY KEY,
    ""Name"" text NOT NULL,
    ""Username"" text NOT NULL DEFAULT '',
    ""Email"" text NOT NULL,
    ""PasswordHash"" text NOT NULL DEFAULT '',
    ""DateCreated"" timestamp without time zone NOT NULL DEFAULT now(),
    ""IsActive"" boolean NOT NULL DEFAULT TRUE
);

CREATE TABLE ""Programs"" (
    ""Id"" serial PRIMARY KEY,
    ""UserId"" integer NOT NULL,
    ""Title"" character varying(200) NOT NULL,
    ""Description"" character varying(1000) NULL,
    ""GoalId"" integer NULL,
    ""TotalWeeks"" integer NOT NULL DEFAULT 12,
    ""CurrentWeek"" integer NOT NULL DEFAULT 1,
    ""CurrentDay"" integer NOT NULL DEFAULT 1,
    ""StartDate"" timestamp without time zone NOT NULL,
    ""EndDate"" timestamp without time zone NULL,
    ""IsActive"" boolean NOT NULL DEFAULT TRUE,
    ""IsCompleted"" boolean NOT NULL DEFAULT FALSE,
    ""Status"" character varying(20) NOT NULL DEFAULT 'active',
    ""SourceConversationId"" integer NULL,
    ""CompletedAt"" timestamp without time zone NULL,
    ""CreatedAt"" timestamp without time zone NOT NULL,
    ""ProgramStructure"" character varying(2000) NULL,
    CONSTRAINT ""FK_Programs_Users_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE
);

CREATE TABLE ""ProgramWorkouts"" (
    ""Id"" serial PRIMARY KEY,
    ""ProgramId"" integer NOT NULL,
    ""WeekNumber"" integer NOT NULL,
    ""DayNumber"" integer NOT NULL,
    ""DayName"" character varying(20) NULL,
    ""WorkoutName"" character varying(200) NOT NULL,
    ""WorkoutType"" character varying(50) NULL,
    ""Description"" character varying(1000) NULL,
    ""EstimatedDuration"" integer NULL,
    ""ExercisesJson"" text NOT NULL,
    ""WarmUp"" character varying(1000) NULL,
    ""CoolDown"" character varying(1000) NULL,
    ""IsCompleted"" boolean NOT NULL DEFAULT FALSE,
    ""CompletedAt"" timestamp without time zone NULL,
    ""CompletionNotes"" character varying(1000) NULL,
    ""OrderIndex"" integer NOT NULL DEFAULT 0,
    ""IsRestDay"" boolean NOT NULL DEFAULT FALSE,
    ""ScheduledDate"" timestamp without time zone NULL,
    CONSTRAINT ""FK_ProgramWorkouts_Programs_ProgramId"" FOREIGN KEY (""ProgramId"") REFERENCES ""Programs"" (""Id"") ON DELETE CASCADE
);
CREATE INDEX ""IX_ProgramWorkouts_ProgramId_WeekNumber_DayNumber"" ON ""ProgramWorkouts"" (""ProgramId"", ""WeekNumber"", ""DayNumber"");

CREATE TABLE ""Exercises"" (
    ""Id"" serial PRIMARY KEY,
    ""SessionId"" integer NOT NULL,
    ""Name"" character varying(100) NOT NULL,
    ""SortOrder"" integer NOT NULL DEFAULT 0,
    ""Duration"" integer NULL,
    ""RestTime"" integer NULL,
    ""Notes"" character varying(500) NULL,
    ""ExerciseTemplateId"" integer NULL,
    ""Version"" integer NOT NULL DEFAULT 1
);

CREATE TABLE ""__EFMigrationsHistory"" (
    ""MigrationId"" character varying(150) NOT NULL CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY,
    ""ProductVersion"" character varying(32) NOT NULL
);
";
    }

    [CollectionDefinition(Name)]
    public sealed class ProgramWorkoutOccurrenceKeyPostgresCollection
        : ICollectionFixture<ProgramWorkoutOccurrenceKeyPostgresFixture>
    {
        public const string Name = "program-workout-occurrence-key-postgres";
    }
}
