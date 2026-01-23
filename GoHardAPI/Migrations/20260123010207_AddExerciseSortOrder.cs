using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoHardAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Exercises",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Exercises");
        }
    }
}
