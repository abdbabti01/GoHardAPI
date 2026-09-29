using System;
using System.Linq;
using System.Text.RegularExpressions;
using GoHardAPI.Data;
using GoHardAPI.Migrations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Executable coverage for <see cref="GoHardAPI.Migrations.AddExerciseTargets"/>, driving the
    /// real EF Core migration pipeline (<c>Database.Migrate()</c>) against SQLite — same approach
    /// as <see cref="ExerciseOccurrenceKeyMigrationTests"/>: seed every migration EXCEPT this one
    /// as already applied, leaving exactly this one pending.
    /// </summary>
    public class ExerciseTargetsMigrationTests
    {
        private const string MineId = "20260929041302_AddExerciseTargets";
        private const string PrevId = "20260914013617_AddNutritionGoalEffectiveDate";

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

        /// <summary>Representative pre-migration <c>Exercises</c> shape (occurrenceKey version plus OccurrenceKey column).</summary>
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
                ""OccurrenceKey"" TEXT,
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
        public void Migrate_AddsThreeNullableTargetColumns_ExistingRowsSurviveWithNulls()
        {
            var (conn, ctx) = NewDb();
            using (conn) using (ctx)
            {
                PrepareHistory(conn, ctx);
                CreatePreUpgradeExercisesTable(conn);
                Exec(conn, @"INSERT INTO ""Exercises"" (""SessionId"",""Name"",""OccurrenceKey"") VALUES (1,'Keep','k-1');");

                Assert.Equal(new[] { MineId }, ctx.Database.GetPendingMigrations().ToArray());
                ctx.Database.Migrate();

                foreach (var c in new[] { "TargetSets", "TargetRepsMin", "TargetRepsMax" })
                {
                    Assert.True(ColumnExists(conn, "Exercises", c));
                    Assert.Equal(0, Scalar(conn, $"SELECT COUNT(*) FROM \"Exercises\" WHERE \"{c}\" IS NOT NULL;"));
                }
                Assert.Equal(1, Scalar(conn, "SELECT COUNT(*) FROM \"Exercises\" WHERE \"Name\"='Keep' AND \"OccurrenceKey\"='k-1';"));
            }
        }

        [Fact]
        public void Migrate_RunTwice_SecondRunIsANoOp()
        {
            var (conn, ctx) = NewDb();
            using (conn) using (ctx)
            {
                PrepareHistory(conn, ctx);
                CreatePreUpgradeExercisesTable(conn);
                ctx.Database.Migrate();
                ctx.Database.Migrate();
                Assert.Empty(ctx.Database.GetPendingMigrations());
                Assert.Single(ctx.Database.GetAppliedMigrations().Where(m => m == MineId));
            }
        }

        [Fact]
        public void ProviderSql_IsGuarded_OnSqlServerAndPostgres()
        {
            Assert.Equal(3, Regex.Matches(ExerciseTargetsSql.SqlServerUp, "IF COL_LENGTH").Count);
            Assert.Equal(3, Regex.Matches(ExerciseTargetsSql.NpgsqlUp, "ADD COLUMN IF NOT EXISTS").Count);
            Assert.Equal(3, Regex.Matches(ExerciseTargetsSql.NpgsqlDown, "DROP COLUMN IF EXISTS").Count);
            Assert.Equal(3, Regex.Matches(ExerciseTargetsSql.SqlServerDown, "IF OBJECT_ID").Count);
            Assert.Equal(3, Regex.Matches(ExerciseTargetsSql.SqlServerDown, @"DROP COLUMN \[").Count);
        }
    }
}
