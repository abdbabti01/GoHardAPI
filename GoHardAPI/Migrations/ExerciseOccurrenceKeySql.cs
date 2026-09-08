namespace GoHardAPI.Migrations
{
    /// <summary>
    /// Provider-specific SQL for <see cref="AddExerciseOccurrenceKey"/>, factored out the same
    /// way as <see cref="SessionCreateOperationSql"/> so the DDL can be reviewed in one place
    /// and exercised directly by the migration test-suite.
    ///
    /// <para><b>Up</b>: SQL Server and PostgreSQL guard the column add so a repeated or
    /// out-of-order run against an already-patched database is a no-op rather than a "duplicate
    /// column" failure. SQLite's <c>ALTER TABLE ... ADD COLUMN</c> has no <c>IF NOT EXISTS</c>
    /// form and is left unguarded (unchanged from the pre-existing convention in
    /// <c>AddScheduledDateToProgramWorkout</c> / the Generic branch of
    /// <see cref="SessionCreateOperationSql"/>) — in both EF's real migration pipeline and every
    /// test here, <c>Up</c> only ever runs once per database because EF's own
    /// <c>__EFMigrationsHistory</c> bookkeeping prevents a second invocation.</para>
    ///
    /// <para><b>Down</b>: SQL Server and PostgreSQL guard both the table and the column so
    /// rolling back is safe even against a database where <c>Up</c> never actually ran (that
    /// happens routinely in this test suite's "seed every OTHER migration as applied" fixtures
    /// when a migration this one's test class doesn't touch also needs to be unwound). SQLite
    /// has no <c>DROP COLUMN IF EXISTS</c> and no in-SQL conditional guard at all, so — exactly
    /// like <see cref="SessionCreateOperationSql.GenericDown"/> — the Generic branch does
    /// nothing: the leftover nullable <c>OccurrenceKey</c> column is inert. Forward migration is
    /// the real path.</para>
    /// </summary>
    internal static class ExerciseOccurrenceKeySql
    {
        // ------------------------------------------------------------------ SQL Server (local dev)

        internal const string SqlServerUp = @"
IF COL_LENGTH(N'[Exercises]', N'OccurrenceKey') IS NULL
    ALTER TABLE [Exercises] ADD [OccurrenceKey] nvarchar(100) NULL;
";

        internal const string SqlServerDown = @"
IF OBJECT_ID(N'[Exercises]', N'U') IS NOT NULL AND COL_LENGTH(N'[Exercises]', N'OccurrenceKey') IS NOT NULL
    ALTER TABLE [Exercises] DROP COLUMN [OccurrenceKey];
";

        // ---------------------------------------------------------------- PostgreSQL (production)

        internal const string NpgsqlUp = @"
ALTER TABLE ""Exercises"" ADD COLUMN IF NOT EXISTS ""OccurrenceKey"" character varying(100) NULL;
";

        internal const string NpgsqlDown = @"
ALTER TABLE IF EXISTS ""Exercises"" DROP COLUMN IF EXISTS ""OccurrenceKey"";
";

        // ------------------------------------------ Generic (SQLite test-suite / other providers)

        internal const string GenericUp = @"
ALTER TABLE ""Exercises"" ADD COLUMN ""OccurrenceKey"" TEXT NULL;
";

        // Deliberately empty: see the Down section of the class summary above.
        internal const string GenericDown = "";
    }
}
