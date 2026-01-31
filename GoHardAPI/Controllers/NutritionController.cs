using Asp.Versioning;
using GoHardAPI.Data;
using GoHardAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GoHardAPI.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    public class NutritionController : ControllerBase
    {
        private readonly TrainingContext _context;
        private readonly NutritionCalculatorService _calculator;
        private readonly ILogger<NutritionController> _logger;

        public NutritionController(
            TrainingContext context,
            NutritionCalculatorService calculator,
            ILogger<NutritionController> logger)
        {
            _context = context;
            _calculator = calculator;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("User not authenticated");
            }
            return userId;
        }

        /// <summary>
        /// Calculate personalized nutrition targets based on user metrics and goal
        /// </summary>
        [HttpPost("calculate")]
        public async Task<ActionResult<CalculateNutritionResponse>> CalculateNutrition(CalculateNutritionRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Validate required metrics
                if (!user.Weight.HasValue || user.Weight <= 0)
                {
                    return BadRequest(new { message = "Please set your weight in your profile first" });
                }

                if (!user.Height.HasValue || user.Height <= 0)
                {
                    return BadRequest(new { message = "Please set your height in your profile first" });
                }

                var weightKg = (decimal)user.Weight.Value;
                var heightCm = (decimal)user.Height.Value;
                var age = NutritionCalculatorService.CalculateAge(user.DateOfBirth);
                var gender = user.Gender ?? "Male";
                var activityLevel = user.ActivityLevel ?? "ModeratelyActive";

                // Calculate target weight change per week based on request
                decimal? targetWeightChangePerWeek = null;
                if (request.TargetWeightChange.HasValue && request.TimeframeWeeks.HasValue && request.TimeframeWeeks > 0)
                {
                    targetWeightChangePerWeek = Math.Abs(request.TargetWeightChange.Value) / request.TimeframeWeeks.Value;
                }

                var calculation = _calculator.CalculateNutrition(
                    weightKg,
                    heightCm,
                    age,
                    gender,
                    activityLevel,
                    request.GoalType ?? "Maintenance",
                    targetWeightChangePerWeek
                );

                _logger.LogInformation(
                    "Calculated nutrition for user {UserId}: {Calories} cal, {Protein}g protein (Goal: {Goal})",
                    userId, calculation.DailyCalories, calculation.DailyProtein, request.GoalType);

                return Ok(new CalculateNutritionResponse
                {
                    DailyCalories = calculation.DailyCalories,
                    DailyProtein = calculation.DailyProtein,
                    DailyCarbohydrates = calculation.DailyCarbohydrates,
                    DailyFat = calculation.DailyFat,
                    DailyFiber = 25, // Standard recommendation
                    DailyWater = 2000, // Standard recommendation (ml)
                    Bmr = calculation.Bmr,
                    Tdee = calculation.Tdee,
                    CalorieAdjustment = calculation.CalorieAdjustment,
                    ExpectedWeeklyWeightChange = calculation.ExpectedWeeklyWeightChange,
                    Explanation = calculation.Explanation,
                    UserMetrics = new UserMetricsSummary
                    {
                        WeightKg = weightKg,
                        WeightLbs = Math.Round(weightKg * 2.205m, 1),
                        HeightCm = heightCm,
                        Age = age,
                        Gender = gender,
                        ActivityLevel = activityLevel
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating nutrition");
                return StatusCode(500, new { message = "Failed to calculate nutrition targets" });
            }
        }

        /// <summary>
        /// Calculate and save nutrition targets as active nutrition goal
        /// </summary>
        [HttpPost("calculate-and-save")]
        public async Task<ActionResult<CalculateNutritionResponse>> CalculateAndSaveNutrition(CalculateNutritionRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Validate required metrics
                if (!user.Weight.HasValue || user.Weight <= 0)
                {
                    return BadRequest(new { message = "Please set your weight in your profile first" });
                }

                if (!user.Height.HasValue || user.Height <= 0)
                {
                    return BadRequest(new { message = "Please set your height in your profile first" });
                }

                var weightKg = (decimal)user.Weight.Value;
                var heightCm = (decimal)user.Height.Value;
                var age = NutritionCalculatorService.CalculateAge(user.DateOfBirth);
                var gender = user.Gender ?? "Male";
                var activityLevel = user.ActivityLevel ?? "ModeratelyActive";

                // Calculate target weight change per week based on request
                decimal? targetWeightChangePerWeek = null;
                if (request.TargetWeightChange.HasValue && request.TimeframeWeeks.HasValue && request.TimeframeWeeks > 0)
                {
                    targetWeightChangePerWeek = Math.Abs(request.TargetWeightChange.Value) / request.TimeframeWeeks.Value;
                }

                var calculation = _calculator.CalculateNutrition(
                    weightKg,
                    heightCm,
                    age,
                    gender,
                    activityLevel,
                    request.GoalType ?? "Maintenance",
                    targetWeightChangePerWeek
                );

                // Deactivate existing nutrition goals
                var existingGoals = await _context.NutritionGoals
                    .Where(ng => ng.UserId == userId && ng.IsActive)
                    .ToListAsync();

                foreach (var existing in existingGoals)
                {
                    existing.IsActive = false;
                    existing.UpdatedAt = DateTime.UtcNow;
                }

                // Create new nutrition goal
                var goalName = request.GoalType?.ToLower() switch
                {
                    var g when g?.Contains("loss") == true => "Weight Loss Plan",
                    var g when g?.Contains("gain") == true || g?.Contains("muscle") == true => "Muscle Gain Plan",
                    _ => "Maintenance Plan"
                };

                var nutritionGoal = new Models.NutritionGoal
                {
                    UserId = userId,
                    Name = goalName,
                    DailyCalories = calculation.DailyCalories,
                    DailyProtein = calculation.DailyProtein,
                    DailyCarbohydrates = calculation.DailyCarbohydrates,
                    DailyFat = calculation.DailyFat,
                    DailyFiber = 25,
                    DailyWater = 2000,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.NutritionGoals.Add(nutritionGoal);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Calculated and saved nutrition goal {GoalId} for user {UserId}: {Calories} cal",
                    nutritionGoal.Id, userId, calculation.DailyCalories);

                return Ok(new CalculateNutritionResponse
                {
                    NutritionGoalId = nutritionGoal.Id,
                    DailyCalories = calculation.DailyCalories,
                    DailyProtein = calculation.DailyProtein,
                    DailyCarbohydrates = calculation.DailyCarbohydrates,
                    DailyFat = calculation.DailyFat,
                    DailyFiber = 25,
                    DailyWater = 2000,
                    Bmr = calculation.Bmr,
                    Tdee = calculation.Tdee,
                    CalorieAdjustment = calculation.CalorieAdjustment,
                    ExpectedWeeklyWeightChange = calculation.ExpectedWeeklyWeightChange,
                    Explanation = calculation.Explanation,
                    UserMetrics = new UserMetricsSummary
                    {
                        WeightKg = weightKg,
                        WeightLbs = Math.Round(weightKg * 2.205m, 1),
                        HeightCm = heightCm,
                        Age = age,
                        Gender = gender,
                        ActivityLevel = activityLevel
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating and saving nutrition");
                return StatusCode(500, new { message = "Failed to calculate and save nutrition targets" });
            }
        }

        /// <summary>
        /// Get available activity levels
        /// </summary>
        [HttpGet("activity-levels")]
        [AllowAnonymous]
        public ActionResult<IEnumerable<ActivityLevelOption>> GetActivityLevels()
        {
            var levels = new List<ActivityLevelOption>
            {
                new() { Value = "Sedentary", Label = "Sedentary", Description = "Little or no exercise, desk job" },
                new() { Value = "LightlyActive", Label = "Lightly Active", Description = "Light exercise 1-3 days/week" },
                new() { Value = "ModeratelyActive", Label = "Moderately Active", Description = "Moderate exercise 3-5 days/week" },
                new() { Value = "VeryActive", Label = "Very Active", Description = "Hard exercise 6-7 days/week" },
                new() { Value = "ExtremelyActive", Label = "Extremely Active", Description = "Very hard exercise, physical job, or 2x training" }
            };

            return Ok(levels);
        }
    }

    // Request/Response DTOs
    public class CalculateNutritionRequest
    {
        /// <summary>
        /// Goal type: WeightLoss, MuscleGain, Maintenance
        /// </summary>
        public string? GoalType { get; set; }

        /// <summary>
        /// Target weight change in lbs (positive for gain, can be negative for loss)
        /// </summary>
        public decimal? TargetWeightChange { get; set; }

        /// <summary>
        /// Timeframe in weeks to achieve target weight change
        /// </summary>
        public int? TimeframeWeeks { get; set; }
    }

    public class CalculateNutritionResponse
    {
        public int? NutritionGoalId { get; set; }
        public decimal DailyCalories { get; set; }
        public decimal DailyProtein { get; set; }
        public decimal DailyCarbohydrates { get; set; }
        public decimal DailyFat { get; set; }
        public decimal DailyFiber { get; set; }
        public decimal DailyWater { get; set; }
        public decimal Bmr { get; set; }
        public decimal Tdee { get; set; }
        public decimal CalorieAdjustment { get; set; }
        public decimal ExpectedWeeklyWeightChange { get; set; }
        public string Explanation { get; set; } = string.Empty;
        public UserMetricsSummary? UserMetrics { get; set; }
    }

    public class UserMetricsSummary
    {
        public decimal WeightKg { get; set; }
        public decimal WeightLbs { get; set; }
        public decimal HeightCm { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string ActivityLevel { get; set; } = string.Empty;
    }

    public class ActivityLevelOption
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
