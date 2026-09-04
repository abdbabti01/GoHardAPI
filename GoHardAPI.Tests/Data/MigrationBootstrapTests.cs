using System.Linq;
using GoHardAPI.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GoHardAPI.Tests.Data
{
    /// <summary>
    /// <see cref="MigrationBootstrap.MigrationsToPreStamp"/> decides which migrations the
    /// empty-history startup path may INSERT into <c>__EFMigrationsHistory</c> WITHOUT
    /// running their <c>Up()</c>. The keyed-Session-CREATE migration must NEVER be in that
    /// set - it carries real DDL a fresh database still needs, so it must stay pending for
    /// <c>context.Database.Migrate()</c>. Each excluded predicate is pinned by a test so a
    /// mutation removing it fails.
    /// </summary>
    public class MigrationBootstrapTests
    {
        private static readonly string[] SamplePending =
        {
            "20251227014922_InitialCreate",
            "20260104143124_AddSessionName",
            "20260109202221_BrokenLegacyEntry",
            "20260112145739_AddSessionVersionColumn",
            "20260109204500_AddProgramTablesOnly",   // Programs migration
            "20260111150116_AddProgramFieldsToSession",
            "20260903195625_AddSessionCreateOperationAndClientOperationId",
        };

        [Fact]
        public void DoesNotPreStamp_TheKeyedSessionCreateMigration()
        {
            var toStamp = MigrationBootstrap.MigrationsToPreStamp(SamplePending);

            Assert.DoesNotContain(
                "20260903195625_AddSessionCreateOperationAndClientOperationId", toStamp);
        }

        [Fact]
        public void Prefix_MatchesExactlyOneRealMigration_SoARenameCannotSilentlyReEnableBlindStamping()
        {
            using var ctx = new TrainingContext(
                new DbContextOptionsBuilder<TrainingContext>()
                    .UseSqlServer("Server=localhost;Database=x;Trusted_Connection=True;").Options);

            var matches = ctx.Database.GetMigrations()
                .Where(m => m.StartsWith(MigrationBootstrap.SessionCreateOperationMigrationPrefix, System.StringComparison.Ordinal))
                .ToList();

            Assert.Single(matches);
            Assert.Contains("AddSessionCreateOperationAndClientOperationId", matches[0]);
            // ...and the fix genuinely keeps it out of the blind-stamp set.
            Assert.DoesNotContain(matches[0], MigrationBootstrap.MigrationsToPreStamp(ctx.Database.GetMigrations()));
        }

        [Fact]
        public void DoesNotPreStamp_ProgramsMigrations()
        {
            var toStamp = MigrationBootstrap.MigrationsToPreStamp(SamplePending);

            Assert.DoesNotContain(toStamp, m => m.Contains("AddProgram"));
        }

        [Fact]
        public void DoesNotPreStamp_The202601092Chain()
        {
            var toStamp = MigrationBootstrap.MigrationsToPreStamp(SamplePending);

            Assert.DoesNotContain(toStamp, m => m.StartsWith("202601092"));
        }

        [Fact]
        public void PreStamps_PlainLegacyMigrationsWhoseSchemaAlreadyExists()
        {
            var toStamp = MigrationBootstrap.MigrationsToPreStamp(SamplePending);

            Assert.Contains("20251227014922_InitialCreate", toStamp);
            Assert.Contains("20260104143124_AddSessionName", toStamp);
            Assert.Contains("20260112145739_AddSessionVersionColumn", toStamp);
        }

        [Fact]
        public void EmptyInput_ReturnsEmpty()
        {
            Assert.Empty(MigrationBootstrap.MigrationsToPreStamp(System.Array.Empty<string>()));
        }
    }
}
