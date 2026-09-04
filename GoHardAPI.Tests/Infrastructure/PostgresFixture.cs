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
    /// Boots one real PostgreSQL 16 container for the keyed-Session-CREATE test suites and
    /// brings its schema to the post-migration state the way a real deployment does:
    ///
    /// <list type="number">
    ///   <item>hand-create a representative <b>legacy</b> schema — <c>Users</c> and a
    ///     <c>Sessions</c> table with the full current column set <b>except</b>
    ///     <c>ClientOperationId</c>;</item>
    ///   <item>seed <c>__EFMigrationsHistory</c> with every migration id <b>except</b>
    ///     <see cref="MigrationId"/>, leaving exactly that one pending;</item>
    ///   <item>run <c>Database.Migrate()</c> — EF executes the real Npgsql branch of
    ///     <c>AddSessionCreateOperationAndClientOperationId</c> inside its own transaction and
    ///     writes the history row.</item>
    /// </list>
    ///
    /// If Docker is not reachable the fixture constructs but <see cref="Available"/> is
    /// <c>false</c>; the <see cref="DockerRequiredFactAttribute"/> skips the tests.
    /// </summary>
    public sealed class PostgresFixture : IAsyncLifetime
    {
        public const string MigrationId = "20260903195625_AddSessionCreateOperationAndClientOperationId";

        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

        public bool Available { get; private set; }
        public string ConnectionString { get; private set; } = string.Empty;

        public TrainingContext NewContext() =>
            new(new DbContextOptionsBuilder<TrainingContext>().UseNpgsql(ConnectionString).Options);

        public NpgsqlConnection NewRawConnection() => new(ConnectionString);

        public async Task InitializeAsync()
        {
            try
            {
                await _container.StartAsync();
            }
            catch (Exception ex)
            {
                // CI (REQUIRE_POSTGRES_TESTS=true): a container that will not start is a
                // hard failure, not a silent skip. Local dev: degrade to unavailable.
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
        /// Representative pre-migration schema. Column set matches what the running app
        /// expects on <c>Sessions</c> today (legacy timestamp behavior =&gt;
        /// <c>timestamp without time zone</c>), minus the <c>ClientOperationId</c> the
        /// migration adds. No <c>Programs</c>/<c>Exercises</c> tables — the suites only
        /// touch <c>Users</c>, <c>Sessions</c> and <c>SessionCreateOperations</c>.
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

CREATE TABLE ""Sessions"" (
    ""Id"" serial PRIMARY KEY,
    ""UserId"" integer NOT NULL,
    ""Date"" timestamp without time zone NOT NULL,
    ""Duration"" integer NULL,
    ""Notes"" character varying(1000) NULL,
    ""Type"" character varying(50) NULL,
    ""Name"" character varying(100) NULL,
    ""Status"" character varying(20) NOT NULL DEFAULT 'draft',
    ""StartedAt"" timestamp without time zone NULL,
    ""CompletedAt"" timestamp without time zone NULL,
    ""PausedAt"" timestamp without time zone NULL,
    ""ProgramId"" integer NULL,
    ""ProgramWorkoutId"" integer NULL,
    ""Version"" integer NOT NULL DEFAULT 1,
    CONSTRAINT ""FK_Sessions_Users_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE
);
CREATE INDEX ""IX_Sessions_UserId"" ON ""Sessions"" (""UserId"");

CREATE TABLE ""__EFMigrationsHistory"" (
    ""MigrationId"" character varying(150) NOT NULL CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY,
    ""ProductVersion"" character varying(32) NOT NULL
);
";
    }

    [CollectionDefinition(Name)]
    public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
    {
        public const string Name = "postgres";
    }
}
