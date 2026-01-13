using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoHardAPI.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDeleteGoalProgram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Change Program-Goal relationship to CASCADE DELETE
            migrationBuilder.DropForeignKey(
                name: "FK_Programs_Goals_GoalId",
                table: "Programs");

            migrationBuilder.AddForeignKey(
                name: "FK_Programs_Goals_GoalId",
                table: "Programs",
                column: "GoalId",
                principalTable: "Goals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Change Session-Program relationship to CASCADE DELETE
            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_Programs_ProgramId",
                table: "Sessions");

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Programs_ProgramId",
                table: "Sessions",
                column: "ProgramId",
                principalTable: "Programs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Change Session-ProgramWorkout relationship to CASCADE DELETE
            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_ProgramWorkouts_ProgramWorkoutId",
                table: "Sessions");

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_ProgramWorkouts_ProgramWorkoutId",
                table: "Sessions",
                column: "ProgramWorkoutId",
                principalTable: "ProgramWorkouts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert Session-ProgramWorkout to NO ACTION
            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_ProgramWorkouts_ProgramWorkoutId",
                table: "Sessions");

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_ProgramWorkouts_ProgramWorkoutId",
                table: "Sessions",
                column: "ProgramWorkoutId",
                principalTable: "ProgramWorkouts",
                principalColumn: "Id");

            // Revert Session-Program to NO ACTION
            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_Programs_ProgramId",
                table: "Sessions");

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Programs_ProgramId",
                table: "Sessions",
                column: "ProgramId",
                principalTable: "Programs",
                principalColumn: "Id");

            // Revert Program-Goal to SET NULL
            migrationBuilder.DropForeignKey(
                name: "FK_Programs_Goals_GoalId",
                table: "Programs");

            migrationBuilder.AddForeignKey(
                name: "FK_Programs_Goals_GoalId",
                table: "Programs",
                column: "GoalId",
                principalTable: "Goals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
