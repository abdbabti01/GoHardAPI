using System;
using System.Collections.Generic;
using System.Linq;

namespace GoHardAPI.Data
{
    /// <summary>
    /// Helpers for the Railway/PostgreSQL startup migration bootstrap in <c>Program.cs</c>.
    ///
    /// The production database was originally created with <c>EnsureCreated()</c>, so it has
    /// base tables but (on the first deploy of the migrations pipeline, or a rebuild) an
    /// empty <c>__EFMigrationsHistory</c>. On that path <c>Program.cs</c> stamps the
    /// migrations whose objects already exist as "applied" so
    /// <c>context.Database.Migrate()</c> only executes genuinely new ones.
    ///
    /// Migrations that carry real, provider-aware DDL a fresh database still needs must be
    /// EXCLUDED from that blind stamp so <c>Migrate()</c> runs their <c>Up()</c>:
    /// the Programs migrations, the <c>202601092*</c> chain, and
    /// <c>20260903195625_AddSessionCreateOperationAndClientOperationId</c> (a single
    /// provider-branched <c>migrationBuilder.Sql(...)</c> that is <c>IF [NOT] EXISTS</c>
    /// guarded and therefore safe on both a fresh and an existing database).
    /// </summary>
    public static class MigrationBootstrap
    {
        /// <summary>
        /// Migration-id timestamp prefix of the keyed-Session-CREATE schema migration.
        /// It must never be stamped without executing its DDL.
        /// </summary>
        public const string SessionCreateOperationMigrationPrefix = "20260903195625";

        /// <summary>
        /// From <paramref name="pendingMigrations"/>, the ones safe to INSERT into
        /// <c>__EFMigrationsHistory</c> without running <c>Up()</c> (their schema already
        /// exists via <c>EnsureCreated</c>). Anything carrying DDL a brand-new database
        /// still needs is left out so it stays pending for <c>Migrate()</c>.
        /// </summary>
        public static IReadOnlyList<string> MigrationsToPreStamp(IEnumerable<string> pendingMigrations)
        {
            ArgumentNullException.ThrowIfNull(pendingMigrations);

            return pendingMigrations
                .Where(m =>
                    !m.Contains("AddProgram", StringComparison.Ordinal) &&
                    !m.StartsWith("202601092", StringComparison.Ordinal) &&
                    !m.StartsWith(SessionCreateOperationMigrationPrefix, StringComparison.Ordinal))
                .ToList();
        }
    }
}
