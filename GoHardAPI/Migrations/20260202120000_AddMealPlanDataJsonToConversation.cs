using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoHardAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMealPlanDataJsonToConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MealPlanDataJson",
                table: "ChatConversations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkoutPlanDataJson",
                table: "ChatConversations",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MealPlanDataJson",
                table: "ChatConversations");

            migrationBuilder.DropColumn(
                name: "WorkoutPlanDataJson",
                table: "ChatConversations");
        }
    }
}
