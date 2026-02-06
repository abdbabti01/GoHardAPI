using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GoHardAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddNutritionProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NutritionProgresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NutritionGoalId = table.Column<int>(type: "integer", nullable: true),
                    PlannedCalories = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    PlannedProtein = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    PlannedCarbohydrates = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    PlannedFat = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    PlannedFiber = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    PlannedWater = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    ConsumedCalories = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    ConsumedProtein = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    ConsumedCarbohydrates = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    ConsumedFat = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    ConsumedFiber = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    ConsumedWater = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NutritionProgresses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NutritionProgresses_NutritionGoals_NutritionGoalId",
                        column: x => x.NutritionGoalId,
                        principalTable: "NutritionGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NutritionProgresses_UserId_Date",
                table: "NutritionProgresses",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NutritionProgresses_NutritionGoalId",
                table: "NutritionProgresses",
                column: "NutritionGoalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NutritionProgresses");
        }
    }
}
