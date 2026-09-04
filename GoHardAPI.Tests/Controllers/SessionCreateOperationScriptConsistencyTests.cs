using System;
using System.IO;
using GoHardAPI.Migrations;
using GoHardAPI.Tests.Infrastructure;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Provider-agnostic file assertions for the keyed-Session-CREATE schema (no container,
    /// no database). Kept out of <see cref="SessionCreateOperationMigrationPostgresTests"/>
    /// so that class holds only real-PostgreSQL integration tests and the
    /// <c>Category=PostgresIntegration</c> filter selects all and only those.
    ///
    /// Verifies:
    /// <list type="bullet">
    ///   <item>the committed <c>Scripts/SessionCreateOperation.*.sql</c> stay in sync with
    ///     the migration's per-provider DDL constants;</item>
    ///   <item>the idempotent schema DDL lives ONLY in the migration and is never copied
    ///     into <c>Program.cs</c> (which would double-execute at startup and crash the
    ///     deploy).</item>
    /// </list>
    /// </summary>
    public class SessionCreateOperationScriptConsistencyTests
    {
        private const string MigrationId = PostgresFixture.MigrationId;

        [Fact]
        public void CommittedProviderScripts_MatchTheMigrationSql_AndProgramCsHasNoDuplicateDdl()
        {
            var root = FindRepoRoot();
            var pg = File.ReadAllText(Path.Combine(root, "GoHardAPI", "Scripts", "SessionCreateOperation.Postgres.sql"));
            var ss = File.ReadAllText(Path.Combine(root, "GoHardAPI", "Scripts", "SessionCreateOperation.SqlServer.sql"));

            Assert.Contains(Normalize(SessionCreateOperationSql.NpgsqlUp), Normalize(pg));
            Assert.Contains(Normalize(SessionCreateOperationSql.SqlServerUp), Normalize(ss));

            foreach (var script in new[] { pg, ss })
            {
                Assert.Contains("__EFMigrationsHistory", script);
                Assert.Contains(MigrationId, script);
            }

            // The idempotent schema DDL must live ONLY in the migration, never copied into Program.cs.
            var programCs = File.ReadAllText(Path.Combine(root, "GoHardAPI", "Program.cs"));
            Assert.DoesNotContain("SessionCreateOperations", programCs);
            Assert.DoesNotContain("ClientOperationId", programCs);
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
