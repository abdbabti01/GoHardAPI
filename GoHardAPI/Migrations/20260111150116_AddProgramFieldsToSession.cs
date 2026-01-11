using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoHardAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddProgramFieldsToSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProgramId",
                table: "Sessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProgramWorkoutId",
                table: "Sessions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ProgramId",
                table: "Sessions",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ProgramWorkoutId",
                table: "Sessions",
                column: "ProgramWorkoutId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_ProgramWorkouts_ProgramWorkoutId",
                table: "Sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_Programs_ProgramId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_ProgramId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_ProgramWorkoutId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "ProgramId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "ProgramWorkoutId",
                table: "Sessions");
        }
    }
}
