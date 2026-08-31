using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoHardAPI.Migrations
{
    /// <summary>
    /// Schema-convergence migration for the Workout Template feature.
    ///
    /// History context: the <c>WorkoutTemplates</c> and <c>WorkoutTemplateRatings</c> tables were
    /// never created by an EF migration — they only exist in the model snapshot and in
    /// <c>Scripts/Phase3_DatabaseSetup.sql</c> / <c>Scripts/FixWorkoutTemplateSchema.sql</c> (run by
    /// hand against the local SQL Server database). On a fresh database, or on the PostgreSQL
    /// production database, they may not exist at all.
    ///
    /// This migration therefore does two things, idempotently, so it is safe on every environment:
    ///   1. Creates both tables and their indexes IF they are absent (fresh / production convergence).
    ///   2. Adds the new <c>IsPublic</c> visibility column (default <c>false</c>) and its supporting
    ///      index IF they are absent.
    ///
    /// It never drops or recreates an existing table, so no data is lost where the tables were
    /// already provisioned by the SQL scripts. Provider-specific SQL is used because the guard
    /// syntax differs per engine; see <see cref="WorkoutTemplateConvergenceSql"/>.
    /// </summary>
    public partial class AddWorkoutTemplateVisibilityAndConvergeSchema : Migration
    {
        private const string SqlServer = "Microsoft.EntityFrameworkCore.SqlServer";
        private const string Npgsql = "Npgsql.EntityFrameworkCore.PostgreSQL";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Each branch is a single non-suppressed Sql() call so EF wraps the whole migration —
            // schema changes plus the __EFMigrationsHistory row — in one transaction: a failed
            // deploy leaves neither a history row nor partial schema. Never add a
            // `suppressTransaction: true` Sql() call here or a PostgreSQL `CREATE INDEX
            // CONCURRENTLY`; either would break that all-or-nothing guarantee.
            migrationBuilder.Sql(migrationBuilder.ActiveProvider switch
            {
                SqlServer => WorkoutTemplateConvergenceSql.SqlServerUp,
                Npgsql => WorkoutTemplateConvergenceSql.NpgsqlUp,
                _ => WorkoutTemplateConvergenceSql.GenericUp
            });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately partial and non-destructive: this only reverses the additions the
            // migration is responsible for (the IsPublic column and its index). The template
            // tables are left in place because on every real database they predate this
            // migration — dropping them here would destroy data this migration never created.
            migrationBuilder.Sql(migrationBuilder.ActiveProvider switch
            {
                SqlServer => WorkoutTemplateConvergenceSql.SqlServerDown,
                Npgsql => WorkoutTemplateConvergenceSql.NpgsqlDown,
                _ => WorkoutTemplateConvergenceSql.GenericDown
            });
        }
    }
}
