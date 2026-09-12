using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoHardAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalSoftDelete : Migration
    {
        private const string SqlServer = "Microsoft.EntityFrameworkCore.SqlServer";
        private const string Npgsql = "Npgsql.EntityFrameworkCore.PostgreSQL";

        // Raw, provider-branched SQL rather than the scaffolded `AddColumn<T>(type: ...)`
        // calls, which came back with literal SQL Server types ("datetime2"/"bit") that
        // are used verbatim by whichever provider actually runs the migration — see
        // `AddGoalArchiveAndMealPlanSourceIdentity` for the full rationale and the
        // Postgres test (`GoalArchiveMealPlanMigrationPostgresTests`) this pattern is
        // verified against. Same pattern reused here for the same reason.
        private const string SqlServerUp = @"
ALTER TABLE [Goals] ADD [DeletedAt] datetime2 NULL;
ALTER TABLE [Goals] ADD [IsDeleted] bit NOT NULL DEFAULT 0;

CREATE INDEX [IX_Goals_UserId_IsDeleted] ON [Goals] ([UserId], [IsDeleted]);
";

        private const string NpgsqlUp = @"
ALTER TABLE ""Goals"" ADD ""DeletedAt"" timestamp with time zone NULL;
ALTER TABLE ""Goals"" ADD ""IsDeleted"" boolean NOT NULL DEFAULT FALSE;

CREATE INDEX ""IX_Goals_UserId_IsDeleted"" ON ""Goals"" (""UserId"", ""IsDeleted"");
";

        private const string SqlServerDown = @"
DROP INDEX [IX_Goals_UserId_IsDeleted] ON [Goals];

ALTER TABLE [Goals] DROP COLUMN [DeletedAt];
ALTER TABLE [Goals] DROP COLUMN [IsDeleted];
";

        private const string NpgsqlDown = @"
DROP INDEX ""IX_Goals_UserId_IsDeleted"";

ALTER TABLE ""Goals"" DROP COLUMN ""DeletedAt"";
ALTER TABLE ""Goals"" DROP COLUMN ""IsDeleted"";
";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(migrationBuilder.ActiveProvider switch
            {
                SqlServer => SqlServerUp,
                Npgsql => NpgsqlUp,
                _ => NpgsqlUp,
            });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(migrationBuilder.ActiveProvider switch
            {
                SqlServer => SqlServerDown,
                Npgsql => NpgsqlDown,
                _ => NpgsqlDown,
            });
        }
    }
}
