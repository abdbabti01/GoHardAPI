using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoHardAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddNutritionGoalCalculationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Bmr",
                table: "NutritionGoals",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CalorieAdjustment",
                table: "NutritionGoals",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Explanation",
                table: "NutritionGoals",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Tdee",
                table: "NutritionGoals",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bmr",
                table: "NutritionGoals");

            migrationBuilder.DropColumn(
                name: "CalorieAdjustment",
                table: "NutritionGoals");

            migrationBuilder.DropColumn(
                name: "Explanation",
                table: "NutritionGoals");

            migrationBuilder.DropColumn(
                name: "Tdee",
                table: "NutritionGoals");
        }
    }
}
