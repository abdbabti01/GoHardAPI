using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoHardAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueConstraintAllowMultipleWorkoutsPerDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProgramWorkouts_ProgramId_WeekNumber_DayNumber",
                table: "ProgramWorkouts");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramWorkouts_ProgramId_WeekNumber_DayNumber",
                table: "ProgramWorkouts",
                columns: new[] { "ProgramId", "WeekNumber", "DayNumber" });
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
                columns: new[] { "ProgramId", "WeekNumber", "DayNumber" },
                unique: true);
        }
    }
}
