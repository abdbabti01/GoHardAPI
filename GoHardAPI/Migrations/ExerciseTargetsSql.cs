namespace GoHardAPI.Migrations
{
    /// <summary>
    /// Provider-specific DDL for <see cref="AddExerciseTargets"/>, same pattern and rationale as
    /// <see cref="ExerciseOccurrenceKeySql"/>: guarded Up/Down on SQL Server and PostgreSQL,
    /// unguarded Up and no-op Down on SQLite. Three nullable int columns, no data migration.
    /// </summary>
    internal static class ExerciseTargetsSql
    {
        private static readonly string[] Columns = { "TargetSets", "TargetRepsMin", "TargetRepsMax" };

        internal static readonly string SqlServerUp = string.Concat(Columns.Select(c => $@"
IF COL_LENGTH(N'[Exercises]', N'{c}') IS NULL
    ALTER TABLE [Exercises] ADD [{c}] int NULL;
"));

        internal static readonly string SqlServerDown = string.Concat(Columns.Select(c => $@"
IF OBJECT_ID(N'[Exercises]', N'U') IS NOT NULL AND COL_LENGTH(N'[Exercises]', N'{c}') IS NOT NULL
    ALTER TABLE [Exercises] DROP COLUMN [{c}];
"));

        internal static readonly string NpgsqlUp = string.Concat(Columns.Select(c =>
            $"ALTER TABLE \"Exercises\" ADD COLUMN IF NOT EXISTS \"{c}\" integer NULL;\n"));

        internal static readonly string NpgsqlDown = string.Concat(Columns.Select(c =>
            $"ALTER TABLE IF EXISTS \"Exercises\" DROP COLUMN IF EXISTS \"{c}\";\n"));

        internal static readonly string GenericUp = string.Concat(Columns.Select(c =>
            $"ALTER TABLE \"Exercises\" ADD COLUMN \"{c}\" INTEGER NULL;\n"));

        internal const string GenericDown = "";
    }
}
