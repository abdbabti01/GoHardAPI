using System;
using System.Linq;
using System.Threading.Tasks;
using GoHardAPI.Data;
using GoHardAPI.Models;
using GoHardAPI.Services;
using GoHardAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Runs the real <c>AddNutritionGoalEffectiveDate</c> migration's actual <c>Up()</c>
    /// DDL - via EF's <c>Database.Migrate()</c> pipeline, NOT <c>EnsureCreated</c> -
    /// against a real PostgreSQL 16 database seeded with rows shaped like the LEGACY
    /// (pre-migration) schema, and asserts: PostgreSQL-native column types land (not SQL
    /// Server's "bit"/"datetime2"), the backfill correctly distinguishes a row that was
    /// NEVER touched by the old in-place-update endpoints (CreatedAt is genuine evidence)
    /// from one that WAS (only UpdatedAt is genuine evidence - CreatedAt would fabricate
    /// history for values that were actually overwritten later), that a row genuinely
    /// EDITED in place and LATER deactivated by a sibling goal's creation (which
    /// overwrites UpdatedAt a second time, destroying the edit's own timestamp) still
    /// resolves the edited-then-superseded interval as unknown rather than projecting
    /// the current value backward past when it actually started applying, and that the
    /// partial unique index actually rejects a second active row for the same user.
    /// Same isolation technique as <see cref="GoalSoftDeleteMigrationPostgresTests"/>.
    /// </summary>
    [Trait("Category", "PostgresIntegration")]
    public sealed class NutritionGoalEffectiveDateMigrationPostgresTests : IAsyncLifetime
    {
        private const string MigrationId = "AddNutritionGoalEffectiveDate";

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
        public async Task the_migration_backfills_from_created_at_and_enforces_one_active_row_per_user()
        {
            Assert.True(_available, "PostgreSQL container must be available in CI");

            // 1. Build the full current schema straight from the C# model.
            await using (var seedCtx = NewContext())
            {
                await seedCtx.Database.EnsureCreatedAsync();
            }

            await using var conn = new NpgsqlConnection(_cs);
            await conn.OpenAsync();

            // 2. Revert ONLY the columns/index this migration adds, back to the legacy
            //    pre-migration shape, then seed a LEGACY-shaped row (a real user's target
            //    created before this migration ever shipped).
            await Exec(conn, "DROP INDEX IF EXISTS \"IX_NutritionGoals_UserId_Active\";");
            await Exec(conn, "ALTER TABLE \"NutritionGoals\" DROP COLUMN \"EffectiveDate\", DROP COLUMN \"DeletedAt\";");
            await using (var seedCtx = NewContext())
            {
                seedCtx.Users.Add(new User { Id = 1, Name = "u", Username = "u", Email = "u@x.com", PasswordHash = "h" });
                seedCtx.Users.Add(new User { Id = 2, Name = "u2", Username = "u2", Email = "u2@x.com", PasswordHash = "h" });
                seedCtx.Users.Add(new User { Id = 3, Name = "u3", Username = "u3", Email = "u3@x.com", PasswordHash = "h" });
                await seedCtx.SaveChangesAsync();
            }
            // User 1: a row NEVER touched by the old in-place-update endpoints (no
            // UpdatedAt) - CreatedAt is genuine, trustworthy evidence of when these
            // exact values started applying.
            await Exec(
                conn,
                "INSERT INTO \"NutritionGoals\" (\"UserId\", \"DailyCalories\", \"DailyProtein\", \"DailyCarbohydrates\", \"DailyFat\", \"IsActive\", \"CreatedAt\") " +
                "VALUES (1, 1800, 100, 100, 50, TRUE, '2025-06-15T10:30:00Z');");
            // User 2: created long ago, then modified IN PLACE under the OLD (pre-Phase-3)
            // UpdateNutritionGoal/apply-meal-plan behavior - simulated here exactly as
            // those endpoints did it: the macro fields were overwritten and UpdatedAt was
            // stamped, with NO new row ever created. The values stored now (2200 kcal) are
            // only evidenced to have applied since UpdatedAt (2025-06-01) - what applied
            // between CreatedAt (2024-01-01) and UpdatedAt is genuinely lost, and the
            // migration must not fabricate that these (actually more recent) values were
            // in effect all the way back to CreatedAt.
            await Exec(
                conn,
                "INSERT INTO \"NutritionGoals\" (\"UserId\", \"DailyCalories\", \"DailyProtein\", \"DailyCarbohydrates\", \"DailyFat\", \"IsActive\", \"CreatedAt\", \"UpdatedAt\") " +
                "VALUES (2, 2200, 150, 200, 70, TRUE, '2024-01-01T08:00:00Z', '2025-06-01T12:00:00Z');");
            // User 3: the exact counterexample a "trust UpdatedAt unless a sibling
            // was created near it" correlation heuristic gets wrong. Goal A is
            // created Jan 1 (2000 kcal, never stored anywhere post-migration - it is
            // NOT recoverable, only mentioned here for narrative context), genuinely
            // EDITED in place Feb 1 to 1800 kcal via the old UpdateNutritionGoal
            // endpoint (A.UpdatedAt becomes Feb 1 - this row's CURRENT stored value,
            // 1800, really did start applying Feb 1). Then goal B is created Mar 10 at
            // 09:00:00, and the old DeactivateOtherGoals helper - which fires in the
            // SAME request as B's INSERT - OVERWRITES A.UpdatedAt AGAIN to Mar 10 at
            // 09:00:01, one second later, purely because B was created; A's fields are
            // never touched again. By migration time, A.UpdatedAt = Mar 10 09:00:01 is
            // the ONLY surviving timestamp - the Feb 1 edit's evidence is gone, and a
            // sibling (B) genuinely WAS created within a couple of seconds of it. A
            // correlation heuristic keyed on exactly that fingerprint would conclude
            // "not real edit evidence, fall back to CreatedAt" and backfill Jan 1 -
            // fabricating that A's CURRENT (1800 kcal) values applied all through
            // January and February, when 2000 and then a genuinely-different 1800
            // actually did. Plain COALESCE(UpdatedAt, CreatedAt) has no such blind
            // spot: it always uses Mar 10 here, regardless of why UpdatedAt changed.
            await Exec(
                conn,
                "INSERT INTO \"NutritionGoals\" (\"UserId\", \"DailyCalories\", \"DailyProtein\", \"DailyCarbohydrates\", \"DailyFat\", \"IsActive\", \"CreatedAt\", \"UpdatedAt\") " +
                "VALUES (3, 1800, 120, 150, 55, FALSE, '2024-01-01T08:00:00Z', '2024-03-10T09:00:01Z');");
            await Exec(
                conn,
                "INSERT INTO \"NutritionGoals\" (\"UserId\", \"DailyCalories\", \"DailyProtein\", \"DailyCarbohydrates\", \"DailyFat\", \"IsActive\", \"CreatedAt\") " +
                "VALUES (3, 1900, 140, 180, 60, TRUE, '2024-03-10T09:00:00Z');");

            // 3. Seed migration history: every OTHER migration marked applied, leaving only
            //    this one pending.
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

            // 5. PostgreSQL-native types, not SQL Server's "bit"/"datetime2".
            Assert.Equal(
                "timestamp with time zone",
                await Scalar(conn, "SELECT data_type FROM information_schema.columns WHERE table_name='NutritionGoals' AND column_name='EffectiveDate'"));
            Assert.Equal(
                "timestamp with time zone",
                await Scalar(conn, "SELECT data_type FROM information_schema.columns WHERE table_name='NutritionGoals' AND column_name='DeletedAt'"));

            // 6. User 1's untouched legacy row was backfilled from its OWN CreatedAt date -
            //    the only evidence available - never today's date or any other guess.
            var effectiveDate = await Scalar(
                conn,
                "SELECT to_char(\"EffectiveDate\", 'YYYY-MM-DD') FROM \"NutritionGoals\" WHERE \"UserId\" = 1");
            Assert.Equal("2025-06-15", effectiveDate);

            // 6b. User 2's in-place-modified row was backfilled from UpdatedAt, NOT
            //     CreatedAt - the current target is preserved (never mutated by this
            //     migration), but its EffectiveDate honestly reflects only what's evidenced.
            var user2EffectiveDate = await Scalar(
                conn,
                "SELECT to_char(\"EffectiveDate\", 'YYYY-MM-DD') FROM \"NutritionGoals\" WHERE \"UserId\" = 2");
            Assert.Equal("2025-06-01", user2EffectiveDate);
            var user2Calories = await Scalar(
                conn,
                "SELECT \"DailyCalories\"::text FROM \"NutritionGoals\" WHERE \"UserId\" = 2");
            Assert.Equal("2200", user2Calories);

            // 6c. Through the real service, not just raw SQL: a date BETWEEN CreatedAt and
            //     UpdatedAt for user 2 resolves as genuinely unknown - the migration must
            //     not let ResolveForDateAsync claim the current (2200 kcal) value applied
            //     that far back, since it was only evidenced from UpdatedAt onward.
            var midInterval = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
            var resolvedMidInterval = await NutritionTargetService.ResolveForDateAsync(ctx, userId: 2, date: midInterval);
            Assert.Null(resolvedMidInterval);

            // 6d. But on/after UpdatedAt, the current (preserved) target resolves correctly -
            //     "preserve the current target" holds even though EffectiveDate moved forward.
            var onUpdatedAt = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
            var resolvedOnUpdatedAt = await NutritionTargetService.ResolveForDateAsync(ctx, userId: 2, date: onUpdatedAt);
            Assert.NotNull(resolvedOnUpdatedAt);
            Assert.Equal(2200, resolvedOnUpdatedAt!.DailyCalories);

            // 6e. THE COUNTEREXAMPLE: user 3's goal A (edited Feb 1, then its
            //     UpdatedAt overwritten again by a sibling's creation one second
            //     later) must backfill from its OWN surviving UpdatedAt -
            //     2024-03-10 - never from CreatedAt (2024-01-01) and never from the
            //     lost Feb 1 edit date. A correlation heuristic that "sees" B
            //     created within seconds of it and falls back to CreatedAt would
            //     get this wrong (2024-01-01) and fabricate history; plain
            //     COALESCE(UpdatedAt, CreatedAt) gets it right regardless of why
            //     UpdatedAt changed.
            var user3AEffectiveDate = await Scalar(
                conn,
                "SELECT to_char(\"EffectiveDate\", 'YYYY-MM-DD') FROM \"NutritionGoals\" WHERE \"UserId\" = 3 AND \"DailyCalories\" = 1800");
            Assert.Equal("2024-03-10", user3AEffectiveDate);

            // 6f. Through the real service: January and February - when 2000 and
            //     then 1800 genuinely applied, per the narrative - resolve as
            //     UNKNOWN, never fabricated with A's current 1800 value. This is
            //     the exact assertion a sibling-correlation heuristic gets wrong.
            var januaryUser3 = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            Assert.Null(await NutritionTargetService.ResolveForDateAsync(ctx, userId: 3, date: januaryUser3));
            var februaryUser3 = new DateTime(2024, 2, 15, 0, 0, 0, DateTimeKind.Utc);
            Assert.Null(await NutritionTargetService.ResolveForDateAsync(ctx, userId: 3, date: februaryUser3));
            var earlyMarchUser3 = new DateTime(2024, 3, 9, 0, 0, 0, DateTimeKind.Utc);
            Assert.Null(await NutritionTargetService.ResolveForDateAsync(ctx, userId: 3, date: earlyMarchUser3));

            // 6g. Current-target preservation: A and B share the same (day-
            //     granularity) EffectiveDate, so the documented tie-break
            //     (EffectiveDate DESC, Id DESC - higher Id, i.e. the one created
            //     later, wins) correctly resolves Mar 10 onward to B, the actual
            //     current target - "preserve the current target" holds even
            //     through a same-day supersession.
            var lateMarchUser3 = new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Utc);
            var resolvedLateMarch = await NutritionTargetService.ResolveForDateAsync(ctx, userId: 3, date: lateMarchUser3);
            Assert.NotNull(resolvedLateMarch);
            Assert.Equal(1900, resolvedLateMarch!.DailyCalories);

            // 7. The partial unique index is real and enforced: a second active row for the
            //    same user must be rejected by PostgreSQL itself, not merely by application code.
            var violated = false;
            try
            {
                await Exec(
                    conn,
                    "INSERT INTO \"NutritionGoals\" (\"UserId\", \"DailyCalories\", \"DailyProtein\", \"DailyCarbohydrates\", \"DailyFat\", \"IsActive\", \"EffectiveDate\", \"CreatedAt\") " +
                    "VALUES (1, 2200, 150, 200, 70, TRUE, now(), now());");
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                violated = true;
            }
            Assert.True(violated, "a second active row for the same user must violate the partial unique index");

            // A second INACTIVE row for the same user is unaffected by the partial index.
            await Exec(
                conn,
                "INSERT INTO \"NutritionGoals\" (\"UserId\", \"DailyCalories\", \"DailyProtein\", \"DailyCarbohydrates\", \"DailyFat\", \"IsActive\", \"EffectiveDate\", \"CreatedAt\") " +
                "VALUES (1, 2200, 150, 200, 70, FALSE, now(), now());");
        }

        /// <summary>
        /// Repeated deployment must not re-run the backfill or move an
        /// already-established EffectiveDate/DeletedAt. EF Core migrations record
        /// themselves in __EFMigrationsHistory and are never re-applied, and the
        /// backfill's own guard is `WHERE EffectiveDate IS NULL` - this test proves
        /// both hold together against the real migration: after it has run once and
        /// real application traffic (NutritionTargetService) has created dated rows
        /// of its own, calling Database.MigrateAsync() again is a genuine no-op that
        /// changes nothing already established.
        /// </summary>
        [DockerRequiredFact]
        public async Task a_second_migrate_call_after_the_migration_has_run_changes_nothing()
        {
            Assert.True(_available, "PostgreSQL container must be available in CI");

            await using (var seedCtx = NewContext())
            {
                await seedCtx.Database.EnsureCreatedAsync();
            }

            await using var conn = new NpgsqlConnection(_cs);
            await conn.OpenAsync();

            await Exec(conn, "DROP INDEX IF EXISTS \"IX_NutritionGoals_UserId_Active\";");
            await Exec(conn, "ALTER TABLE \"NutritionGoals\" DROP COLUMN \"EffectiveDate\", DROP COLUMN \"DeletedAt\";");
            await using (var seedCtx = NewContext())
            {
                seedCtx.Users.Add(new User { Id = 1, Name = "u", Username = "u", Email = "u@x.com", PasswordHash = "h" });
                await seedCtx.SaveChangesAsync();
            }
            await Exec(
                conn,
                "INSERT INTO \"NutritionGoals\" (\"UserId\", \"DailyCalories\", \"DailyProtein\", \"DailyCarbohydrates\", \"DailyFat\", \"IsActive\", \"CreatedAt\") " +
                "VALUES (1, 1800, 100, 100, 50, TRUE, '2025-06-15T10:30:00Z');");

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

            // First deployment: the migration actually runs and backfills.
            await using (var ctx = NewContext())
            {
                await ctx.Database.MigrateAsync();
            }

            var effectiveDateAfterFirstMigrate = await Scalar(
                conn,
                "SELECT to_char(\"EffectiveDate\", 'YYYY-MM-DD') FROM \"NutritionGoals\" WHERE \"UserId\" = 1");
            Assert.Equal("2025-06-15", effectiveDateAfterFirstMigrate);

            // Real post-migration application traffic: a genuinely new dated row,
            // exactly as NutritionTargetService produces it in production.
            NutritionGoal created;
            await using (var appCtx = NewContext())
            {
                created = await NutritionTargetService.SetActiveGoalAsync(
                    appCtx,
                    userId: 1,
                    fields: new NutritionGoal { DailyCalories = 2100, DailyProtein = 160, DailyCarbohydrates = 210, DailyFat = 70 },
                    effectiveDate: new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc));
            }

            // Second "deployment": Database.MigrateAsync() again - no pending
            // migrations, so this must be a true no-op.
            await using (var secondDeployCtx = NewContext())
            {
                var pendingOnRedeploy = (await secondDeployCtx.Database.GetPendingMigrationsAsync()).ToList();
                Assert.Empty(pendingOnRedeploy);
                await secondDeployCtx.Database.MigrateAsync();
            }

            // The original backfilled row's cutover has not moved.
            var effectiveDateAfterSecondMigrate = await Scalar(
                conn,
                "SELECT to_char(\"EffectiveDate\", 'YYYY-MM-DD') FROM \"NutritionGoals\" WHERE \"UserId\" = 1 AND \"DailyCalories\" = 1800");
            Assert.Equal("2025-06-15", effectiveDateAfterSecondMigrate);

            // The row created by real Phase 3 application traffic after the first
            // migration is untouched by the second migrate call.
            var createdEffectiveDate = await Scalar(
                conn,
                $"SELECT to_char(\"EffectiveDate\", 'YYYY-MM-DD') FROM \"NutritionGoals\" WHERE \"Id\" = {created.Id}");
            Assert.Equal("2025-09-01", createdEffectiveDate);

            await using var finalCtx = NewContext();
            var resolvedCurrent = await NutritionTargetService.ResolveForDateAsync(
                finalCtx, userId: 1, date: new DateTime(2025, 9, 15, 0, 0, 0, DateTimeKind.Utc));
            Assert.NotNull(resolvedCurrent);
            Assert.Equal(2100, resolvedCurrent!.DailyCalories);
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
