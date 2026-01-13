using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoHardAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionProgramCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_ProgramWorkouts_ProgramWorkoutId",
                table: "Sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_Programs_ProgramId",
                table: "Sessions");

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_ProgramWorkouts_ProgramWorkoutId",
                table: "Sessions",
                column: "ProgramWorkoutId",
                principalTable: "ProgramWorkouts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Programs_ProgramId",
                table: "Sessions",
                column: "ProgramId",
                principalTable: "Programs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_ProgramWorkouts_ProgramWorkoutId",
                table: "Sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_Programs_ProgramId",
                table: "Sessions");

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_ProgramWorkouts_ProgramWorkoutId",
                table: "Sessions",
                column: "ProgramWorkoutId",
                principalTable: "ProgramWorkouts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Programs_ProgramId",
                table: "Sessions",
                column: "ProgramId",
                principalTable: "Programs",
                principalColumn: "Id");
        }
    }
}
