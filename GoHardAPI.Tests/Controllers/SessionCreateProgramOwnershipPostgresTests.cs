using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GoHardAPI.Data;
using GoHardAPI.DTOs;
using GoHardAPI.Models;
using GoHardAPI.Services;
using GoHardAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Program / ProgramWorkout ownership validation on keyed + legacy Session CREATE,
    /// proven against a REAL PostgreSQL database WITH the Sessions -> Programs /
    /// -> ProgramWorkouts foreign keys in place, so "no FK exception escapes as a 500" is
    /// tested against a real constraint (including a deterministic check-then-insert race).
    /// </summary>
    [Trait("Category", "PostgresIntegration")]
    public sealed class SessionCreateProgramOwnershipPostgresTests : IAsyncLifetime
    {
        private const string MigrationId = PostgresFixture.MigrationId;

        private readonly Testcontainers.PostgreSql.PostgreSqlContainer _container =
            new Testcontainers.PostgreSql.PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();

        private bool _available;
        private string _cs = string.Empty;
        private static int _userSeq = 700_000;
        private static int NextUserId() => Interlocked.Increment(ref _userSeq);

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

            await using (var c = new NpgsqlConnection(_cs))
            {
                await c.OpenAsync();
                await Exec(c, PostgresFixture.LegacyBaselineSql);
                await Exec(c, @"
                    CREATE TABLE ""Programs"" (""Id"" serial PRIMARY KEY, ""UserId"" integer NOT NULL);
                    CREATE TABLE ""ProgramWorkouts"" (
                        ""Id"" serial PRIMARY KEY,
                        ""ProgramId"" integer NOT NULL REFERENCES ""Programs""(""Id"") ON DELETE CASCADE);
                    ALTER TABLE ""Sessions"" ADD CONSTRAINT ""FK_Sessions_Programs_ProgramId""
                        FOREIGN KEY (""ProgramId"") REFERENCES ""Programs""(""Id"") ON DELETE CASCADE;
                    ALTER TABLE ""Sessions"" ADD CONSTRAINT ""FK_Sessions_ProgramWorkouts_ProgramWorkoutId""
                        FOREIGN KEY (""ProgramWorkoutId"") REFERENCES ""ProgramWorkouts""(""Id"") ON DELETE CASCADE;");

                await using var ctx = NewContext();
                foreach (var id in ctx.Database.GetMigrations().Where(m => m != MigrationId))
                {
                    await Exec(c,
                        $"INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ('{id}', '8.0.10');");
                }
            }

            await using (var ctx = NewContext())
            {
                await ctx.Database.MigrateAsync();
            }

            _available = true;
        }

        public async Task DisposeAsync()
        {
            try { await _container.DisposeAsync(); }
            catch { /* never started */ }
        }

        private TrainingContext NewContext() =>
            new(new DbContextOptionsBuilder<TrainingContext>().UseNpgsql(_cs).Options);

        private SessionCreateService NewService(TrainingContext ctx) =>
            new(ctx, NullLogger<SessionCreateService>.Instance);

        private async Task<int> SeedUserAsync()
        {
            var id = NextUserId();
            await using var c = new NpgsqlConnection(_cs);
            await c.OpenAsync();
            await Exec(c, $"INSERT INTO \"Users\" (\"Id\",\"Name\",\"Email\") VALUES ({id}, 'u{id}', 'u{id}@x.com')");
            return id;
        }

        private async Task<int> SeedProgramAsync(int ownerUserId)
        {
            await using var c = new NpgsqlConnection(_cs);
            await c.OpenAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = "INSERT INTO \"Programs\" (\"UserId\") VALUES (@u) RETURNING \"Id\"";
            cmd.Parameters.AddWithValue("u", ownerUserId);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        private async Task<long> SessionCountAsync(int userId) =>
            await ScalarLongAsync($"SELECT count(*) FROM \"Sessions\" WHERE \"UserId\" = {userId}");

        private async Task<long> OperationCountAsync(int userId) =>
            await ScalarLongAsync($"SELECT count(*) FROM \"SessionCreateOperations\" WHERE \"UserId\" = {userId}");

        private static SessionCreateRequestDto Req(int? programId = null, int? programWorkoutId = null, Guid? key = null) =>
            new()
            {
                Name = "S",
                Status = SessionStatus.Draft,
                Date = DateTime.UtcNow,
                ProgramId = programId,
                ProgramWorkoutId = programWorkoutId,
                ClientOperationId = key,
            };

        // ---- foreign programId: 404, nothing written (pre-check; FK never reached) --------

        [DockerRequiredFact]
        public async Task KeyedCreate_ForeignProgramId_Returns404_NoSessionNoOperationRow()
        {
            Assert.True(_available);
            var attacker = await SeedUserAsync();
            var victim = await SeedUserAsync();
            var victimProgram = await SeedProgramAsync(victim);

            await using var ctx = NewContext();
            var outcome = await NewService(ctx).CreateAsync(
                attacker, Req(programId: victimProgram, key: Guid.NewGuid()), CancellationToken.None);

            Assert.Equal(SessionCreateResult.ProgramNotFound, outcome.Result);
            Assert.Equal(SessionCreateErrorCodes.ProgramNotFound, outcome.ErrorCode);
            Assert.Equal(0, await SessionCountAsync(attacker));
            Assert.Equal(0, await OperationCountAsync(attacker));
            Assert.Equal(victim, await ScalarLongAsync($"SELECT \"UserId\" FROM \"Programs\" WHERE \"Id\" = {victimProgram}"));
        }

        [DockerRequiredFact]
        public async Task LegacyCreate_ForeignProgramId_Returns404_NoSession()
        {
            Assert.True(_available);
            var attacker = await SeedUserAsync();
            var victim = await SeedUserAsync();
            var victimProgram = await SeedProgramAsync(victim);

            await using var ctx = NewContext();
            var outcome = await NewService(ctx).CreateAsync(
                attacker, Req(programId: victimProgram), CancellationToken.None);

            Assert.Equal(SessionCreateResult.ProgramNotFound, outcome.Result);
            Assert.Equal(0, await SessionCountAsync(attacker));
        }

        // ---- non-existent programId: 404, never a raw FK 500 -----------------------------

        [DockerRequiredFact]
        public async Task KeyedCreate_NonExistentProgramId_Returns404_NotAnFkException()
        {
            Assert.True(_available);
            var user = await SeedUserAsync();

            await using var ctx = NewContext();
            var outcome = await NewService(ctx).CreateAsync(
                user, Req(programId: 999_999_999, key: Guid.NewGuid()), CancellationToken.None);

            Assert.Equal(SessionCreateResult.ProgramNotFound, outcome.Result);
            Assert.Equal(0, await SessionCountAsync(user));
            Assert.Equal(0, await OperationCountAsync(user));
        }

        // ---- deterministic TOCTOU: program deleted between the check and the insert -------
        // A BEFORE INSERT trigger on "Sessions" deletes the referenced program row, so the
        // ownership pre-check passes but the INSERT then violates
        // FK_Sessions_Programs_ProgramId (real 23503). IsProgramForeignKeyViolation must
        // convert that to 404 program_not_found - never a 500 - and roll the attempt back.

        [DockerRequiredFact]
        public async Task KeyedCreate_ProgramDeletedByTriggerAfterTheCheck_Returns404_NotAnFkException_AndRollsBack()
        {
            Assert.True(_available);
            var user = await SeedUserAsync();
            var program = await SeedProgramAsync(user);

            await using (var c = new NpgsqlConnection(_cs))
            {
                await c.OpenAsync();
                await Exec(c, @"
                    CREATE OR REPLACE FUNCTION _drop_program_before_session() RETURNS trigger AS $fn$
                    BEGIN
                        DELETE FROM ""Programs"" WHERE ""Id"" = NEW.""ProgramId"";
                        RETURN NEW;
                    END; $fn$ LANGUAGE plpgsql;
                    CREATE TRIGGER _drop_program_trg BEFORE INSERT ON ""Sessions""
                        FOR EACH ROW EXECUTE FUNCTION _drop_program_before_session();");
            }
            try
            {
                await using var ctx = NewContext();
                var outcome = await NewService(ctx).CreateAsync(
                    user, Req(programId: program, key: Guid.NewGuid()), CancellationToken.None);

                Assert.Equal(SessionCreateResult.ProgramNotFound, outcome.Result);
                Assert.Equal(0, await SessionCountAsync(user));
                Assert.Equal(0, await OperationCountAsync(user)); // whole keyed attempt rolled back
            }
            finally
            {
                await using var c = new NpgsqlConnection(_cs);
                await c.OpenAsync();
                await Exec(c, @"
                    DROP TRIGGER IF EXISTS _drop_program_trg ON ""Sessions"";
                    DROP FUNCTION IF EXISTS _drop_program_before_session();");
            }
        }

        // ---- owned program still works, and a keyed replay never revalidates -------------

        [DockerRequiredFact]
        public async Task KeyedCreate_OwnedProgram_Succeeds_AndReplayDoesNotRevalidate()
        {
            Assert.True(_available);
            var user = await SeedUserAsync();
            var program = await SeedProgramAsync(user);
            var key = Guid.NewGuid();

            SessionCreateOutcome first;
            await using (var ctx = NewContext())
            {
                first = await NewService(ctx).CreateAsync(user, Req(programId: program, key: key), CancellationToken.None);
            }
            Assert.Equal(SessionCreateResult.Created, first.Result);
            Assert.Equal(program, first.Session!.ProgramId);

            // Reassign the program to nobody-relevant, then replay the same key.
            var stranger = await SeedUserAsync();
            await Exec2($"UPDATE \"Programs\" SET \"UserId\" = {stranger} WHERE \"Id\" = {program}");

            await using (var ctx = NewContext())
            {
                var replay = await NewService(ctx).CreateAsync(user, Req(programId: program, key: key), CancellationToken.None);
                Assert.Equal(SessionCreateResult.ReplayedExisting, replay.Result);
                Assert.Equal(first.Session!.Id, replay.Session!.Id);
            }
            Assert.Equal(1, await SessionCountAsync(user));
        }

        // ---- helpers -------------------------------------------------------------------

        private static async Task Exec(NpgsqlConnection c, string sql)
        {
            await using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task Exec2(string sql)
        {
            await using var c = new NpgsqlConnection(_cs);
            await c.OpenAsync();
            await Exec(c, sql);
        }

        private async Task<long> ScalarLongAsync(string sql)
        {
            await using var c = new NpgsqlConnection(_cs);
            await c.OpenAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToInt64(await cmd.ExecuteScalarAsync() ?? 0L);
        }
    }
}
