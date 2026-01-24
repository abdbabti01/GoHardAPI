using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoHardAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddNutritionTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FoodTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ServingSize = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ServingUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Calories = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Protein = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Carbohydrates = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Fat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Fiber = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Sugar = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SaturatedFat = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TransFat = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Sodium = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Potassium = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Cholesterol = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    VitaminA = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    VitaminC = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    VitaminD = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Calcium = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Iron = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCustom = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoodTemplates_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MealLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    WaterIntake = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCalories = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalProtein = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCarbohydrates = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalFat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalFiber = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalSodium = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MealPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DurationDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AverageDailyCalories = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AverageDailyProtein = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AverageDailyCarbohydrates = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AverageDailyFat = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealPlans_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NutritionGoals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DailyCalories = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DailyProtein = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DailyCarbohydrates = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DailyFat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DailyFiber = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DailySodium = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DailySugar = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DailyWater = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ProteinPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CarbohydratesPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FatPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NutritionGoals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MealEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MealLogId = table.Column<int>(type: "int", nullable: false),
                    MealType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ScheduledTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsConsumed = table.Column<bool>(type: "bit", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TotalCalories = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalProtein = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCarbohydrates = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalFat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalFiber = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalSodium = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealEntries_MealLogs_MealLogId",
                        column: x => x.MealLogId,
                        principalTable: "MealLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MealPlanDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MealPlanId = table.Column<int>(type: "int", nullable: false),
                    DayNumber = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TotalCalories = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalProtein = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCarbohydrates = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalFat = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealPlanDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealPlanDays_MealPlans_MealPlanId",
                        column: x => x.MealPlanId,
                        principalTable: "MealPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FoodItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MealEntryId = table.Column<int>(type: "int", nullable: false),
                    FoodTemplateId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ServingSize = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ServingUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Calories = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Protein = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Carbohydrates = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Fat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Fiber = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Sugar = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Sodium = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoodItems_FoodTemplates_FoodTemplateId",
                        column: x => x.FoodTemplateId,
                        principalTable: "FoodTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FoodItems_MealEntries_MealEntryId",
                        column: x => x.MealEntryId,
                        principalTable: "MealEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MealPlanMeals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MealPlanDayId = table.Column<int>(type: "int", nullable: false),
                    MealType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ScheduledTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TotalCalories = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalProtein = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCarbohydrates = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalFat = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealPlanMeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealPlanMeals_MealPlanDays_MealPlanDayId",
                        column: x => x.MealPlanDayId,
                        principalTable: "MealPlanDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MealPlanFoodItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MealPlanMealId = table.Column<int>(type: "int", nullable: false),
                    FoodTemplateId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ServingSize = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ServingUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Calories = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Protein = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Carbohydrates = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Fat = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealPlanFoodItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealPlanFoodItems_FoodTemplates_FoodTemplateId",
                        column: x => x.FoodTemplateId,
                        principalTable: "FoodTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MealPlanFoodItems_MealPlanMeals_MealPlanMealId",
                        column: x => x.MealPlanMealId,
                        principalTable: "MealPlanMeals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FoodItems_FoodTemplateId",
                table: "FoodItems",
                column: "FoodTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodItems_MealEntryId",
                table: "FoodItems",
                column: "MealEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodTemplates_Barcode",
                table: "FoodTemplates",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_FoodTemplates_Category_IsCustom",
                table: "FoodTemplates",
                columns: new[] { "Category", "IsCustom" });

            migrationBuilder.CreateIndex(
                name: "IX_FoodTemplates_CreatedByUserId",
                table: "FoodTemplates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodTemplates_Name",
                table: "FoodTemplates",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_MealEntries_MealLogId_MealType",
                table: "MealEntries",
                columns: new[] { "MealLogId", "MealType" });

            migrationBuilder.CreateIndex(
                name: "IX_MealLogs_UserId_Date",
                table: "MealLogs",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MealPlanDays_MealPlanId_DayNumber",
                table: "MealPlanDays",
                columns: new[] { "MealPlanId", "DayNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_MealPlanFoodItems_FoodTemplateId",
                table: "MealPlanFoodItems",
                column: "FoodTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_MealPlanFoodItems_MealPlanMealId",
                table: "MealPlanFoodItems",
                column: "MealPlanMealId");

            migrationBuilder.CreateIndex(
                name: "IX_MealPlanMeals_MealPlanDayId_MealType",
                table: "MealPlanMeals",
                columns: new[] { "MealPlanDayId", "MealType" });

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_UserId_IsActive",
                table: "MealPlans",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_NutritionGoals_UserId_IsActive",
                table: "NutritionGoals",
                columns: new[] { "UserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FoodItems");

            migrationBuilder.DropTable(
                name: "MealPlanFoodItems");

            migrationBuilder.DropTable(
                name: "NutritionGoals");

            migrationBuilder.DropTable(
                name: "MealEntries");

            migrationBuilder.DropTable(
                name: "FoodTemplates");

            migrationBuilder.DropTable(
                name: "MealPlanMeals");

            migrationBuilder.DropTable(
                name: "MealLogs");

            migrationBuilder.DropTable(
                name: "MealPlanDays");

            migrationBuilder.DropTable(
                name: "MealPlans");
        }
    }
}
