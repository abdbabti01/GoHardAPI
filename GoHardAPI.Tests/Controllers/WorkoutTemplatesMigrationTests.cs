using System;
using System.IO;
using System.Linq;
using GoHardAPI.Data;
using GoHardAPI.Migrations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Executable coverage for <see cref="AddWorkoutTemplateVisibilityAndConvergeSchema"/>.
    ///
    /// These tests drive the <b>real EF Core migration pipeline</b> (`Database.Migrate()` /
    /// `IMigrator`) against a SQLite database, so they exercise EF's own transaction wrapping and
    /// `__EFMigrationsHistory` management — not just the raw SQL string. The migration's provider
    /// switch resolves to the Generic (SQLite-compatible) branch here; the SQL Server and
    /// PostgreSQL branches cannot run in CI (no engine available) and are verified statically from
    /// the committed scripts (see <see cref="CommittedProviderScripts_StayInSyncWithTheConvergenceSql"/>
    /// and the PR notes).
    ///
    /// The full historical migration chain does NOT translate on SQLite (an older migration
    /// hard-codes `nvarchar(max)`), so each test seeds `Users` + marks every migration EXCEPT this
    /// one as already applied, leaving exactly this migration pending. That mirrors the real
    /// deployment situation: a database that already has the prior schema and is receiving only
    /// the new convergence migration.
    /// </summary>
    public class WorkoutTemplatesMigrationTests
    {
        private const string MineId = "20260830215026_AddWorkoutTemplateVisibilityAndConvergeSchema";
        private const string PrevId = "20260207132327_AddNutritionProgress";

        private static (SqliteConnection conn, TrainingContext ctx) NewDb()
        {
            var conn = new SqliteConnection("DataSource=:memory:");
            conn.Open();
            var ctx = new TrainingContext(
                new DbContextOptionsBuilder<TrainingContext>().UseSqlite(conn).Options);
            return (conn, ctx);
        }

        /// <summary>Seed Users and mark every migration except this one as applied.</summary>
        private static void PrepareHistory(SqliteConnection conn, TrainingContext ctx)
        {
            Exec(conn, @"CREATE TABLE ""Users"" (""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Users"" PRIMARY KEY AUTOINCREMENT, ""Name"" TEXT NOT NULL, ""Username"" TEXT NOT NULL, ""Email"" TEXT NOT NULL, ""PasswordHash"" TEXT NOT NULL);");
            Exec(conn, @"INSERT INTO ""Users"" (""Id"",""Name"",""Username"",""Email"",""PasswordHash"") VALUES (1,'Alice','alice','a@x.com','h'),(2,'Bob','bob','b@x.com','h');");
            Exec(conn, @"CREATE TABLE ""__EFMigrationsHistory"" (""MigrationId"" TEXT NOT NULL CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY, ""ProductVersion"" TEXT NOT NULL);");
            foreach (var id in ctx.Database.GetMigrations().Where(m => m != MineId))
            {
                Exec(conn, $@"INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"",""ProductVersion"") VALUES ('{id}','8.0.10');");
            }
        }

        private static void CreatePreUpgradeTemplateTables(SqliteConnection conn, bool includeRatingsTable = true)
        {
            // The shape Scripts/Phase3_DatabaseSetup.sql produced: no IsPublic column, and (for
            // ratings) the unique index under the script's own name.
            Exec(conn, @"CREATE TABLE ""WorkoutTemplates"" (
                ""Id"" INTEGER PRIMARY KEY AUTOINCREMENT, ""Name"" TEXT NOT NULL, ""Description"" TEXT,
                ""ExercisesJson"" TEXT NOT NULL, ""RecurrencePattern"" TEXT NOT NULL, ""DaysOfWeek"" TEXT,
                ""IntervalDays"" INTEGER, ""EstimatedDuration"" INTEGER, ""Category"" TEXT,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1, ""UsageCount"" INTEGER NOT NULL DEFAULT 0,
                ""CreatedAt"" TEXT NOT NULL, ""LastUsedAt"" TEXT, ""CreatedByUserId"" INTEGER,
                ""IsCustom"" INTEGER NOT NULL DEFAULT 0, ""Rating"" REAL, ""RatingCount"" INTEGER NOT NULL DEFAULT 0);");
            if (includeRatingsTable)
            {
                Exec(conn, @"CREATE TABLE ""WorkoutTemplateRatings"" (
                    ""Id"" INTEGER PRIMARY KEY AUTOINCREMENT, ""WorkoutTemplateId"" INTEGER NOT NULL,
                    ""UserId"" INTEGER NOT NULL, ""Rating"" REAL NOT NULL, ""RatedAt"" TEXT NOT NULL);");
                Exec(conn, @"CREATE UNIQUE INDEX ""IX_WorkoutTemplateRatings_TemplateId_UserId"" ON ""WorkoutTemplateRatings"" (""WorkoutTemplateId"", ""UserId"");");
            }
        }

        private static void Exec(SqliteConnection c, string sql)
        {
            using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        private static long Scalar(SqliteConnection c, string sql)
        {
            using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToInt64(cmd.ExecuteScalar() ?? 0L);
        }

        private static bool ColumnExists(SqliteConnection c, string table, string column) =>
            Scalar(c, $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = '{column}';") > 0;

        private static bool TableExists(SqliteConnection c, string table) =>
            Scalar(c, $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{table}';") > 0;

        // ---- Scenario 1: fresh database --------------------------------------------------------

        [Fact]
        public void FreshDatabase_Migrate_CreatesConvergedSchema_AndEfWritesTheHistoryRow()
        {
            var (conn, ctx) = NewDb();
            using (ctx)
            {
                PrepareHistory(conn, ctx);
                Assert.Equal(new[] { MineId }, ctx.Database.GetPendingMigrations().ToArray());

                ctx.Database.Migrate();

                Assert.True(TableExists(conn, "WorkoutTemplates"));
                Assert.True(TableExists(conn, "WorkoutTemplateRatings"));
                Assert.True(ColumnExists(conn, "WorkoutTemplates", "IsPublic"));
                Assert.Contains(MineId, ctx.Database.GetAppliedMigrations());

                // Unique (WorkoutTemplateId, UserId): two raters OK, same rater twice rejected.
                Exec(conn, @"INSERT INTO ""WorkoutTemplates"" (""Name"",""ExercisesJson"",""RecurrencePattern"",""IsActive"",""UsageCount"",""CreatedAt"",""CreatedByUserId"",""IsCustom"",""RatingCount"") VALUES ('T','[]','daily',1,0,'2026-01-01',1,1,0);");
                Exec(conn, @"INSERT INTO ""WorkoutTemplateRatings"" (""WorkoutTemplateId"",""UserId"",""Rating"",""RatedAt"") VALUES (1,1,4,'2026-01-01');");
                Exec(conn, @"INSERT INTO ""WorkoutTemplateRatings"" (""WorkoutTemplateId"",""UserId"",""Rating"",""RatedAt"") VALUES (1,2,5,'2026-01-01');");
                var dup = Assert.ThrowsAny<SqliteException>(() =>
                    Exec(conn, @"INSERT INTO ""WorkoutTemplateRatings"" (""WorkoutTemplateId"",""UserId"",""Rating"",""RatedAt"") VALUES (1,1,2,'2026-01-01');"));
                Assert.Contains("UNIQUE", dup.Message, StringComparison.OrdinalIgnoreCase);

                // Users table untouched.
                Assert.Equal(2, Scalar(conn, "SELECT COUNT(*) FROM \"Users\";"));

                // A newly inserted custom template is private by default.
                Assert.Equal(0, Scalar(conn, "SELECT \"IsPublic\" FROM \"WorkoutTemplates\" WHERE \"Name\"='T';"));
            }
        }

        // ---- Scenario 2: existing tables with data -------------------------------------------

        [Fact]
        public void ExistingTablesWithData_Migrate_PreservesEverything_AndBackfillsIsPublicFalse()
        {
            var (conn, ctx) = NewDb();
            using (ctx)
            {
                PrepareHistory(conn, ctx);
                CreatePreUpgradeTemplateTables(conn);

                Exec(conn, @"INSERT INTO ""WorkoutTemplates"" (""Id"",""Name"",""ExercisesJson"",""RecurrencePattern"",""IsActive"",""UsageCount"",""CreatedAt"",""CreatedByUserId"",""IsCustom"",""RatingCount"") VALUES (10,'Existing Custom','[]','daily',1,3,'2026-01-01',1,1,1);");
                Exec(conn, @"INSERT INTO ""WorkoutTemplates"" (""Id"",""Name"",""ExercisesJson"",""RecurrencePattern"",""IsActive"",""UsageCount"",""CreatedAt"",""CreatedByUserId"",""IsCustom"",""RatingCount"") VALUES (11,'Existing System','[]','daily',1,0,'2026-01-01',NULL,0,0);");
                Exec(conn, @"INSERT INTO ""WorkoutTemplateRatings"" (""WorkoutTemplateId"",""UserId"",""Rating"",""RatedAt"") VALUES (10,2,4,'2026-01-01');");

                ctx.Database.Migrate();

                // No data loss.
                Assert.Equal(2, Scalar(conn, "SELECT COUNT(*) FROM \"WorkoutTemplates\";"));
                Assert.Equal(1, Scalar(conn, "SELECT COUNT(*) FROM \"WorkoutTemplateRatings\";"));
                // Pre-existing custom row is private after the upgrade.
                Assert.Equal(0, Scalar(conn, "SELECT \"IsPublic\" FROM \"WorkoutTemplates\" WHERE \"Id\"=10;"));
                // Pre-existing system row untouched.
                Assert.Equal(1, Scalar(conn, "SELECT COUNT(*) FROM \"WorkoutTemplates\" WHERE \"Id\"=11 AND \"CreatedByUserId\" IS NULL;"));
                // Ratings survive and the (template,user) pair still cannot be duplicated
                // (via the pre-existing Phase3-named unique index — the migration did not drop it).
                Assert.ThrowsAny<SqliteException>(() =>
                    Exec(conn, @"INSERT INTO ""WorkoutTemplateRatings"" (""WorkoutTemplateId"",""UserId"",""Rating"",""RatedAt"") VALUES (10,2,1,'2026-02-02');"));
                // EF recorded the migration.
                Assert.Contains(MineId, ctx.Database.GetAppliedMigrations());
            }
        }

        // ---- Scenario 3: partially-existing schema ------------------------------------------

        [Fact]
        public void PartialSchema_Migrate_ConvergesTheMissingPieces()
        {
            var (conn, ctx) = NewDb();
            using (ctx)
            {
                PrepareHistory(conn, ctx);
                // Only WorkoutTemplates exists (old shape); the ratings table and every EF index
                // are missing.
                CreatePreUpgradeTemplateTables(conn, includeRatingsTable: false);
                Exec(conn, @"INSERT INTO ""WorkoutTemplates"" (""Id"",""Name"",""ExercisesJson"",""RecurrencePattern"",""IsActive"",""UsageCount"",""CreatedAt"",""CreatedByUserId"",""IsCustom"",""RatingCount"") VALUES (20,'Half','[]','daily',1,0,'2026-01-01',1,1,0);");

                ctx.Database.Migrate();

                Assert.True(TableExists(conn, "WorkoutTemplateRatings"));
                Assert.True(ColumnExists(conn, "WorkoutTemplates", "IsPublic"));
                Assert.Equal(0, Scalar(conn, "SELECT \"IsPublic\" FROM \"WorkoutTemplates\" WHERE \"Id\"=20;"));
                Assert.Equal(1, Scalar(conn, "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name='IX_WorkoutTemplates_IsPublic_Category';"));
                Assert.Equal(1, Scalar(conn, "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name='IX_WorkoutTemplateRatings_WorkoutTemplateId_UserId';"));
                Assert.Contains(MineId, ctx.Database.GetAppliedMigrations());
            }
        }

        // ---- Scenario 4a: re-applied / restarted deployment --------------------------------

        [Fact]
        public void Migrate_RunTwice_SecondRunIsANoOp_AndDataIsIntact()
        {
            var (conn, ctx) = NewDb();
            using (ctx)
            {
                PrepareHistory(conn, ctx);
                ctx.Database.Migrate();
                Exec(conn, @"INSERT INTO ""WorkoutTemplates"" (""Name"",""ExercisesJson"",""RecurrencePattern"",""IsActive"",""UsageCount"",""CreatedAt"",""CreatedByUserId"",""IsCustom"",""RatingCount"",""IsPublic"") VALUES ('Keep','[]','daily',1,0,'2026-01-01',1,1,0,1);");

                // A second deploy: nothing pending, Migrate() does nothing, data survives.
                Assert.Empty(ctx.Database.GetPendingMigrations());
                ctx.Database.Migrate();

                Assert.Equal(1, Scalar(conn, "SELECT COUNT(*) FROM \"WorkoutTemplates\" WHERE \"Name\"='Keep';"));
                Assert.Equal(1, Scalar(conn, "SELECT \"IsPublic\" FROM \"WorkoutTemplates\" WHERE \"Name\"='Keep';"));
                Assert.Single(ctx.Database.GetAppliedMigrations().Where(m => m == MineId));
            }
        }

        // ---- Scenario 4b: interrupted deployment (transaction / recovery) -----------------

        [Fact]
        public void Migrate_WhenUpFailsMidway_TheTransactionRollsBackTheHistoryRow_ThenAnIdempotentRetrySucceeds()
        {
            var (conn, ctx) = NewDb();
            using (ctx)
            {
                PrepareHistory(conn, ctx);
                // Force the Generic branch's `ALTER TABLE ... ADD COLUMN "IsPublic"` to fail:
                // the table already has that column. Everything before it in Up() is idempotent
                // and will run; the ADD COLUMN then throws "duplicate column".
                Exec(conn, @"CREATE TABLE ""WorkoutTemplates"" (
                    ""Id"" INTEGER PRIMARY KEY AUTOINCREMENT, ""Name"" TEXT NOT NULL, ""Description"" TEXT,
                    ""ExercisesJson"" TEXT NOT NULL, ""RecurrencePattern"" TEXT NOT NULL, ""DaysOfWeek"" TEXT,
                    ""IntervalDays"" INTEGER, ""EstimatedDuration"" INTEGER, ""Category"" TEXT,
                    ""IsActive"" INTEGER NOT NULL DEFAULT 1, ""UsageCount"" INTEGER NOT NULL DEFAULT 0,
                    ""CreatedAt"" TEXT NOT NULL, ""LastUsedAt"" TEXT, ""CreatedByUserId"" INTEGER,
                    ""IsCustom"" INTEGER NOT NULL DEFAULT 0, ""Rating"" REAL, ""RatingCount"" INTEGER NOT NULL DEFAULT 0,
                    ""IsPublic"" INTEGER NOT NULL DEFAULT 0);");
                Exec(conn, @"INSERT INTO ""WorkoutTemplates"" (""Id"",""Name"",""ExercisesJson"",""RecurrencePattern"",""IsActive"",""UsageCount"",""CreatedAt"",""CreatedByUserId"",""IsCustom"",""RatingCount"",""IsPublic"") VALUES (30,'PreExisting','[]','daily',1,0,'2026-01-01',1,1,0,0);");

                Assert.ThrowsAny<Exception>(() => ctx.Database.Migrate());

                // EF wraps the migration in a transaction: the failed Up() left NO history row and
                // did not partially commit (the ratings table it created before the failing
                // statement was rolled back).
                Assert.DoesNotContain(MineId, ctx.Database.GetAppliedMigrations());
                Assert.False(TableExists(conn, "WorkoutTemplateRatings"));
                Assert.Equal(1, Scalar(conn, "SELECT COUNT(*) FROM \"WorkoutTemplates\" WHERE \"Id\"=30;"));

                // Operator remediates the drift, redeploy retries the (idempotent) migration.
                Exec(conn, @"ALTER TABLE ""WorkoutTemplates"" DROP COLUMN ""IsPublic"";");
                ctx.Database.Migrate();

                Assert.Contains(MineId, ctx.Database.GetAppliedMigrations());
                Assert.True(ColumnExists(conn, "WorkoutTemplates", "IsPublic"));
                Assert.True(TableExists(conn, "WorkoutTemplateRatings"));
                Assert.Equal(1, Scalar(conn, "SELECT COUNT(*) FROM \"WorkoutTemplates\" WHERE \"Id\"=30;"));
            }
        }

        // ---- Down: partial and non-destructive --------------------------------------------

        [Fact]
        public void Down_RemovesOnlyIsPublicAndItsIndex_LeavingTablesData_RatingsAndTheUniqueIndex()
        {
            var (conn, ctx) = NewDb();
            using (ctx)
            {
                PrepareHistory(conn, ctx);
                ctx.Database.Migrate();
                Exec(conn, @"INSERT INTO ""WorkoutTemplates"" (""Id"",""Name"",""ExercisesJson"",""RecurrencePattern"",""IsActive"",""UsageCount"",""CreatedAt"",""CreatedByUserId"",""IsCustom"",""RatingCount"",""IsPublic"") VALUES (40,'Survivor','[]','daily',1,0,'2026-01-01',1,1,0,1);");
                Exec(conn, @"INSERT INTO ""WorkoutTemplateRatings"" (""WorkoutTemplateId"",""UserId"",""Rating"",""RatedAt"") VALUES (40,2,5,'2026-01-01');");

                // PrepareHistory seeds every OTHER migration — including any added after
                // this one — as already-applied so ctx.Database.Migrate() above only ever
                // runs MineId's real Up(). Those later entries were never actually applied
                // here though, so before asking the migrator to walk back down to PrevId
                // (which would otherwise try to Down() them first and fail against a
                // schema their Up() never touched), drop them from the history: the only
                // migration genuinely applied in this test is MineId itself.
                Exec(conn, $@"DELETE FROM ""__EFMigrationsHistory"" WHERE ""MigrationId"" > '{MineId}';");

                ctx.GetService<IMigrator>().Migrate(PrevId);

                Assert.DoesNotContain(MineId, ctx.Database.GetAppliedMigrations());
                Assert.False(ColumnExists(conn, "WorkoutTemplates", "IsPublic"));
                Assert.Equal(0, Scalar(conn, "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name='IX_WorkoutTemplates_IsPublic_Category';"));
                // Tables, rows, ratings and the unique index all survive the rollback.
                Assert.Equal(1, Scalar(conn, "SELECT COUNT(*) FROM \"WorkoutTemplates\" WHERE \"Id\"=40;"));
                Assert.Equal(1, Scalar(conn, "SELECT COUNT(*) FROM \"WorkoutTemplateRatings\" WHERE \"WorkoutTemplateId\"=40;"));
                Assert.Equal(1, Scalar(conn, "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name='IX_WorkoutTemplateRatings_WorkoutTemplateId_UserId';"));
            }
        }

        // ---- Static verification of the un-runnable provider branches ---------------------

        [Fact]
        public void CommittedProviderScripts_StayInSyncWithTheConvergenceSql()
        {
            var root = FindRepoRoot();
            var sqlServerScript = File.ReadAllText(Path.Combine(root, "GoHardAPI", "Scripts", "WorkoutTemplateConvergence.SqlServer.sql"));
            var postgresScript = File.ReadAllText(Path.Combine(root, "GoHardAPI", "Scripts", "WorkoutTemplateConvergence.Postgres.sql"));

            // Each script is `dotnet ef migrations script`'d from the same migration, so it must
            // contain that provider branch's SQL verbatim (whitespace-normalised).
            Assert.Contains(Normalize(WorkoutTemplateConvergenceSql.SqlServerUp), Normalize(sqlServerScript));
            Assert.Contains(Normalize(WorkoutTemplateConvergenceSql.NpgsqlUp), Normalize(postgresScript));

            // And each script must still let EF own the history row.
            Assert.Contains("__EFMigrationsHistory", sqlServerScript);
            Assert.Contains(MineId, sqlServerScript);
            Assert.Contains("__EFMigrationsHistory", postgresScript);
            Assert.Contains(MineId, postgresScript);
        }

        private static string Normalize(string s) =>
            string.Join(' ', s.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));

        private static string FindRepoRoot()
        {
            var dir = AppContext.BaseDirectory;
            while (dir != null && !File.Exists(Path.Combine(dir, "GoHardAPI.sln")))
            {
                dir = Directory.GetParent(dir)?.FullName;
            }
            return dir ?? throw new DirectoryNotFoundException("repo root (GoHardAPI.sln) not found");
        }
    }
}
