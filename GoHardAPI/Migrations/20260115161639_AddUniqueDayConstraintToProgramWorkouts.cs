using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoHardAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueDayConstraintToProgramWorkouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CLEANUP: Remove any duplicate workouts on the same day before adding unique constraint
            // Keep the first occurrence (lowest Id), delete duplicates
            migrationBuilder.Sql(@"
                WITH DuplicateWorkouts AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (
                               PARTITION BY ProgramId, WeekNumber, DayNumber
                               ORDER BY Id
                           ) AS RowNum
                    FROM ""ProgramWorkouts""
                )
                DELETE FROM ""ProgramWorkouts""
                WHERE ""Id"" IN (
                    SELECT ""Id"" FROM DuplicateWorkouts WHERE RowNum > 1
                );
            ");

            migrationBuilder.DropIndex(
                name: "IX_ProgramWorkouts_ProgramId_WeekNumber_DayNumber",
                table: "ProgramWorkouts");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramWorkouts_ProgramId_WeekNumber_DayNumber",
                table: "ProgramWorkouts",
                columns: new[] { "ProgramId", "WeekNumber", "DayNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProgramWorkouts_ProgramId_WeekNumber_DayNumber",
                table: "ProgramWorkouts");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramWorkouts_ProgramId_WeekNumber_DayNumber",
                table: "ProgramWorkouts",
                columns: new[] { "ProgramId", "WeekNumber", "DayNumber" });
        }
    }
}
