using System;
using System.Linq;
using GoHardAPI.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Executable coverage for <see cref="GoHardAPI.Migrations.AddExerciseOccurrenceKey"/>,
    /// driving the real EF Core migration pipeline (<c>Database.Migrate()</c>) against SQLite —
    /// same approach and same reason as <see cref="WorkoutTemplatesMigrationTests"/>: the full
    /// historical migration chain does not translate on SQLite (an older migration hard-codes
    /// <c>nvarchar(max)</c>), so each test seeds every migration EXCEPT this one as already
    /// applied, leaving exactly this one pending — mirroring a real deployment receiving only
    /// this migration.
    /// </summary>
    public class ExerciseOccurrenceKeyMigrationTests
    {
        private const string MineId = "20260908162606_AddExerciseOccurrenceKey";
        private const string PrevId = "20260903195625_AddSessionCreateOperationAndClientOperationId";

        private static (SqliteConnection conn, TrainingContext ctx) NewDb()
        {
            var conn = new SqliteConnection("DataSource=:memory:");
            conn.Open();
            var ctx = new TrainingContext(
                new DbContextOptionsBuilder<TrainingContext>().UseSqlite(conn).Options);
            return (conn, ctx);
        }

        private static void PrepareHistory(SqliteConnection conn, TrainingContext ctx)
        {
            Exec(conn, @"CREATE TABLE ""__EFMigrationsHistory"" (""MigrationId"" TEXT NOT NULL CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY, ""ProductVersion"" TEXT NOT NULL);");
            foreach (var id in ctx.Database.GetMigrations().Where(m => m != MineId))
            {
                Exec(conn, $@"INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"",""ProductVersion"") VALUES ('{id}','8.0.10');");
            }
        }

        /// <summary>Representative pre-migration <c>Exercises</c> shape (minus <c>OccurrenceKey</c>).</summary>
        private static void CreatePreUpgradeExercisesTable(SqliteConnection conn)
        {
            Exec(conn, @"CREATE TABLE ""Exercises"" (
                ""Id"" INTEGER PRIMARY KEY AUTOINCREMENT,
                ""SessionId"" INTEGER NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""SortOrder"" INTEGER NOT NULL DEFAULT 0,
                ""Duration"" INTEGER,
                ""RestTime"" INTEGER,
                ""Notes"" TEXT,
                ""ExerciseTemplateId"" INTEGER,
                ""Version"" INTEGER NOT NULL DEFAULT 1);");
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

        [Fact]
        public void Migrate_AddsOccurrenceKeyColumn_NullableAndUnpopulated_ExistingRowsSurvive()
        {
            var (conn, ctx) = NewDb();
            using (ctx)
            {
                PrepareHistory(conn, ctx);
                CreatePreUpgradeExercisesTable(conn);
                Exec(conn, @"INSERT INTO ""Exercises"" (""SessionId"",""Name"") VALUES (1,'Bench Press');");

                Assert.Equal(new[] { MineId }, ctx.Database.GetPendingMigrations().ToArray());
                ctx.Database.Migrate();

                Assert.True(ColumnExists(conn, "Exercises", "OccurrenceKey"));
                Assert.Equal(1, Scalar(conn, "SELECT COUNT(*) FROM \"Exercises\";"));
                Assert.Equal(0, Scalar(conn, "SELECT COUNT(*) FROM \"Exercises\" WHERE \"OccurrenceKey\" IS NOT NULL;"));
                Assert.Contains(MineId, ctx.Database.GetAppliedMigrations());
            }
        }

        [Fact]
        public void Migrate_RunTwice_SecondRunIsANoOp_AndDataIsIntact()
        {
            var (conn, ctx) = NewDb();
            using (ctx)
            {
                PrepareHistory(conn, ctx);
                CreatePreUpgradeExercisesTable(conn);
                ctx.Database.Migrate();

                Exec(conn, @"INSERT INTO ""Exercises"" (""SessionId"",""Name"",""OccurrenceKey"") VALUES (1,'Keep','k-1');");
                Assert.Empty(ctx.Database.GetPendingMigrations());
                ctx.Database.Migrate();

                Assert.Equal(1, Scalar(conn, "SELECT COUNT(*) FROM \"Exercises\" WHERE \"Name\"='Keep' AND \"OccurrenceKey\"='k-1';"));
                Assert.Single(ctx.Database.GetAppliedMigrations().Where(m => m == MineId));
            }
        }

        /// <summary>
        /// SQLite has no <c>DROP COLUMN IF EXISTS</c> and no in-SQL conditional guard, so —
        /// exactly like <c>SessionCreateOperationSql.GenericDown</c> for
        /// <c>Sessions.ClientOperationId</c> — the Generic branch of this migration's Down
        /// deliberately leaves the nullable <c>OccurrenceKey</c> column behind rather than
        /// attempting an unguarded drop that would fail whenever Down runs against a database
        /// where Up never actually applied (routine in this suite's "seed every other migration
        /// as applied" fixtures for unrelated migration test classes). The column is inert once
        /// unmigrated; forward migration is the real path. Data survives regardless.
        /// </summary>
        [Fact]
        public void Down_UnappliesTheMigration_ButLeavesTheInertColumnAndDataInPlace()
        {
            var (conn, ctx) = NewDb();
            using (ctx)
            {
                PrepareHistory(conn, ctx);
                CreatePreUpgradeExercisesTable(conn);
                ctx.Database.Migrate();
                Exec(conn, @"INSERT INTO ""Exercises"" (""SessionId"",""Name"",""OccurrenceKey"") VALUES (1,'Survivor','k-1');");

                ctx.GetService<IMigrator>().Migrate(PrevId);

                Assert.DoesNotContain(MineId, ctx.Database.GetAppliedMigrations());
                Assert.True(ColumnExists(conn, "Exercises", "OccurrenceKey"));
                Assert.Equal(1, Scalar(conn, "SELECT COUNT(*) FROM \"Exercises\" WHERE \"Name\"='Survivor' AND \"OccurrenceKey\"='k-1';"));
            }
        }
    }
}
