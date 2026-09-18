using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoHardAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddProgramWorkoutSkip : Migration
    {
        private const string SqlServer = "Microsoft.EntityFrameworkCore.SqlServer";
        private const string Npgsql = "Npgsql.EntityFrameworkCore.PostgreSQL";

        // Raw, provider-branched SQL rather than the scaffolded `AddColumn<T>(type: ...)`
        // calls, which came back with literal SQL Server types ("datetime2"/"bit") that
        // are used verbatim by whichever provider actually runs the migration - see
        // `AddGoalArchiveAndMealPlanSourceIdentity` for the full rationale. Same pattern
        // reused here for the same reason.
        private const string SqlServerUp = @"
ALTER TABLE [ProgramWorkouts] ADD [IsSkipped] bit NOT NULL DEFAULT 0;
ALTER TABLE [ProgramWorkouts] ADD [SkippedAt] datetime2 NULL;
";

        private const string NpgsqlUp = @"
ALTER TABLE ""ProgramWorkouts"" ADD ""IsSkipped"" boolean NOT NULL DEFAULT FALSE;
ALTER TABLE ""ProgramWorkouts"" ADD ""SkippedAt"" timestamp with time zone NULL;
";

        private const string SqlServerDown = @"
ALTER TABLE [ProgramWorkouts] DROP COLUMN [IsSkipped];
ALTER TABLE [ProgramWorkouts] DROP COLUMN [SkippedAt];
";

        private const string NpgsqlDown = @"
ALTER TABLE ""ProgramWorkouts"" DROP COLUMN ""IsSkipped"";
ALTER TABLE ""ProgramWorkouts"" DROP COLUMN ""SkippedAt"";
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
