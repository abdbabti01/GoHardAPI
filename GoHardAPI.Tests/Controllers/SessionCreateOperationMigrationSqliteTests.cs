using System;
using System.Linq;
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
    /// SQLite is retained ONLY for migration-shape smoke coverage of
    /// <see cref="AddSessionCreateOperationAndClientOperationId"/> (the Generic branch) — it
    /// drives EF's real <c>Database.Migrate()</c> pipeline and history handling. It is never
    /// cited as concurrency evidence; that is
    /// <see cref="SessionCreateIdempotencyPostgresTests"/>.
    /// </summary>
    public class SessionCreateOperationMigrationSqliteTests
    {
        private const string MineId = "20260903195625_AddSessionCreateOperationAndClientOperationId";

        private static (SqliteConnection conn, TrainingContext ctx) NewDb()
        {
            var conn = new SqliteConnection("DataSource=:memory:");
            conn.Open();
            var ctx = new TrainingContext(
                new DbContextOptionsBuilder<TrainingContext>().UseSqlite(conn).Options);
            return (conn, ctx);
        }

        private static void PrepareLegacy(SqliteConnection conn, TrainingContext ctx)
        {
            Exec(conn, @"CREATE TABLE ""Users"" (""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Users"" PRIMARY KEY AUTOINCREMENT, ""Name"" TEXT NOT NULL, ""Username"" TEXT NOT NULL DEFAULT '', ""Email"" TEXT NOT NULL, ""PasswordHash"" TEXT NOT NULL DEFAULT '');");
            Exec(conn, @"INSERT INTO ""Users"" (""Id"",""Name"",""Email"") VALUES (1,'a','a@x.com'),(2,'b','b@x.com');");
            Exec(conn, @"CREATE TABLE ""Sessions"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Sessions"" PRIMARY KEY AUTOINCREMENT,
                ""UserId"" INTEGER NOT NULL, ""Date"" TEXT NOT NULL, ""Duration"" INTEGER NULL,
                ""Notes"" TEXT NULL, ""Type"" TEXT NULL, ""Name"" TEXT NULL,
                ""Status"" TEXT NOT NULL DEFAULT 'draft', ""StartedAt"" TEXT NULL,
                ""CompletedAt"" TEXT NULL, ""PausedAt"" TEXT NULL, ""ProgramId"" INTEGER NULL,
                ""ProgramWorkoutId"" INTEGER NULL, ""Version"" INTEGER NOT NULL DEFAULT 1);");
            Exec(conn, @"CREATE INDEX ""IX_Sessions_UserId"" ON ""Sessions"" (""UserId"");");
            Exec(conn, @"CREATE TABLE ""__EFMigrationsHistory"" (""MigrationId"" TEXT NOT NULL CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY, ""ProductVersion"" TEXT NOT NULL);");
            foreach (var id in ctx.Database.GetMigrations().Where(m => m != MineId))
            {
                Exec(conn, $@"INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"",""ProductVersion"") VALUES ('{id}','8.0.10');");
            }
        }

        [Fact]
        public void Migrate_AppliesGenericBranch_AddsColumnTableAndOwnerScopedPartialIndex()
        {
            var (conn, ctx) = NewDb();
            using (ctx)
            {
                PrepareLegacy(conn, ctx);
                Assert.Equal(new[] { MineId }, ctx.Database.GetPendingMigrations().ToArray());

                ctx.Database.Migrate();

                Assert.Contains(MineId, ctx.Database.GetAppliedMigrations());
                Assert.True(ColumnExists(conn, "Sessions", "ClientOperationId"));
                Assert.True(TableExists(conn, "SessionCreateOperations"));
                Assert.Equal(1, Scalar(conn, "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name='IX_Sessions_UserId_ClientOperationId';"));
                Assert.Equal(1, Scalar(conn, "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name='IX_SessionCreateOperations_UserId_ClientOperationId';"));

                // Many NULL keys coexist (partial index).
                Exec(conn, @"INSERT INTO ""Sessions"" (""UserId"",""Date"",""Status"",""Version"") VALUES (1,'2026-01-01','draft',1),(1,'2026-01-02','draft',1),(1,'2026-01-03','draft',1);");
                Assert.Equal(3, Scalar(conn, "SELECT COUNT(*) FROM \"Sessions\" WHERE \"UserId\"=1 AND \"ClientOperationId\" IS NULL;"));

                // Owner-scoped: same key, different owners OK; same (owner,key) rejected.
                Exec(conn, @"INSERT INTO ""Sessions"" (""UserId"",""Date"",""Status"",""Version"",""ClientOperationId"") VALUES (1,'2026-02-01','draft',1,'11111111-1111-1111-1111-111111111111');");
                Exec(conn, @"INSERT INTO ""Sessions"" (""UserId"",""Date"",""Status"",""Version"",""ClientOperationId"") VALUES (2,'2026-02-01','draft',1,'11111111-1111-1111-1111-111111111111');");
                var dup = Assert.ThrowsAny<SqliteException>(() =>
                    Exec(conn, @"INSERT INTO ""Sessions"" (""UserId"",""Date"",""Status"",""Version"",""ClientOperationId"") VALUES (1,'2026-03-01','draft',1,'11111111-1111-1111-1111-111111111111');"));
                Assert.Contains("UNIQUE", dup.Message, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void Down_RemovesTheOperationTableAndIndex_LeavingTheInertNullableColumn()
        {
            var (conn, ctx) = NewDb();
            using (ctx)
            {
                PrepareLegacy(conn, ctx);
                ctx.Database.Migrate();

                // PrepareLegacy seeds every OTHER migration — including any added after
                // this one — as already-applied so ctx.Database.Migrate() above only ever
                // runs MineId's real Up(). Those later entries were never actually applied
                // here though, so before asking the migrator to walk back down (which would
                // otherwise try to Down() them first and fail against a schema their Up()
                // never touched), drop them from the history: the only migration genuinely
                // applied in this test is MineId itself.
                Exec(conn, $@"DELETE FROM ""__EFMigrationsHistory"" WHERE ""MigrationId"" > '{MineId}';");

                ctx.GetService<IMigrator>()
                    .Migrate("20260830215026_AddWorkoutTemplateVisibilityAndConvergeSchema");

                Assert.DoesNotContain(MineId, ctx.Database.GetAppliedMigrations());
                Assert.False(TableExists(conn, "SessionCreateOperations"));
                Assert.Equal(0, Scalar(conn, "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name='IX_Sessions_UserId_ClientOperationId';"));
                // Generic Down is deliberately partial: the nullable column is left behind
                // (SQLite cannot conditionally drop it) but is inert without its unique index.
                Assert.True(ColumnExists(conn, "Sessions", "ClientOperationId"));
            }
        }

        [Fact]
        public void GenericSql_DoesNotUseSqlServerBracketQuoting()
        {
            Assert.DoesNotContain("[ClientOperationId]", SessionCreateOperationSql.GenericUp);
            Assert.DoesNotContain("[ClientOperationId]", SessionCreateOperationSql.NpgsqlUp);
            Assert.Contains("\"ClientOperationId\" IS NOT NULL", SessionCreateOperationSql.NpgsqlUp);
            Assert.Contains("[ClientOperationId] IS NOT NULL", SessionCreateOperationSql.SqlServerUp);
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
    }
}
