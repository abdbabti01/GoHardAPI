using Asp.Versioning;
using GoHardAPI.Data;
using GoHardAPI.Models;
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
    public class NutritionGoalsController : ControllerBase
    {
        private readonly TrainingContext _context;

        public NutritionGoalsController(TrainingContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        /// <summary>
        /// Get all nutrition goals for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NutritionGoal>>> GetNutritionGoals()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var goals = await _context.NutritionGoals
                .Where(ng => ng.UserId == userId)
                .OrderByDescending(ng => ng.IsActive)
                .ThenByDescending(ng => ng.CreatedAt)
                .ToListAsync();

            return Ok(goals);
        }

        /// <summary>
        /// Get the currently active nutrition goal
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<NutritionGoal>> GetActiveGoal()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var goal = await _context.NutritionGoals
                .FirstOrDefaultAsync(ng => ng.UserId == userId && ng.IsActive);

            if (goal == null)
            {
                // Return empty goal if none exists (user should set their own goals)
                return Ok(new NutritionGoal
                {
                    UserId = userId,
                    Name = "Not Set",
                    DailyCalories = 0,
                    DailyProtein = 0,
                    DailyCarbohydrates = 0,
                    DailyFat = 0,
                    DailyFiber = 0,
                    DailyWater = 0
                });
            }

            return Ok(goal);
        }

        /// <summary>
        /// Get a specific nutrition goal by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<NutritionGoal>> GetNutritionGoal(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var goal = await _context.NutritionGoals
                .FirstOrDefaultAsync(ng => ng.Id == id && ng.UserId == userId);

            if (goal == null)
            {
                return NotFound();
            }

            return Ok(goal);
        }

        /// <summary>
        /// Create a new nutrition goal
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<NutritionGoal>> CreateNutritionGoal([FromBody] NutritionGoal goal)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            goal.UserId = userId;
            goal.CreatedAt = DateTime.UtcNow;

            // If this is the first goal or is marked as active, deactivate other goals
            if (goal.IsActive)
            {
                await DeactivateOtherGoals(userId);
            }

            // Calculate macros from percentages if provided
            if (goal.ProteinPercentage.HasValue || goal.CarbohydratesPercentage.HasValue || goal.FatPercentage.HasValue)
            {
                goal.CalculateMacrosFromPercentages();
            }

            _context.NutritionGoals.Add(goal);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetNutritionGoal), new { id = goal.Id }, goal);
        }

        /// <summary>
        /// Update a nutrition goal
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNutritionGoal(int id, [FromBody] NutritionGoal goal)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var existing = await _context.NutritionGoals.FindAsync(id);
            if (existing == null || existing.UserId != userId)
            {
                return NotFound();
            }

            existing.Name = goal.Name;
            existing.DailyCalories = goal.DailyCalories;
            existing.DailyProtein = goal.DailyProtein;
            existing.DailyCarbohydrates = goal.DailyCarbohydrates;
            existing.DailyFat = goal.DailyFat;
            existing.DailyFiber = goal.DailyFiber;
            existing.DailySodium = goal.DailySodium;
            existing.DailySugar = goal.DailySugar;
            existing.DailyWater = goal.DailyWater;
            existing.ProteinPercentage = goal.ProteinPercentage;
            existing.CarbohydratesPercentage = goal.CarbohydratesPercentage;
            existing.FatPercentage = goal.FatPercentage;
            existing.UpdatedAt = DateTime.UtcNow;

            // Calculate macros from percentages if provided
            if (goal.ProteinPercentage.HasValue || goal.CarbohydratesPercentage.HasValue || goal.FatPercentage.HasValue)
            {
                existing.CalculateMacrosFromPercentages();
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Set a goal as active
        /// </summary>
        [HttpPut("{id}/activate")]
        public async Task<IActionResult> ActivateGoal(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var goal = await _context.NutritionGoals.FindAsync(id);
            if (goal == null || goal.UserId != userId)
            {
                return NotFound();
            }

            await DeactivateOtherGoals(userId);

            goal.IsActive = true;
            goal.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Get daily progress vs active goal
        /// </summary>
        [HttpGet("progress")]
        public async Task<ActionResult<NutritionProgressResponse>> GetProgress([FromQuery] DateTime? date = null)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var targetDate = date.HasValue
                ? DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc)
                : DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

            // Get active goal
            var goal = await _context.NutritionGoals
                .FirstOrDefaultAsync(ng => ng.UserId == userId && ng.IsActive);

            // Get meal log for the date
            var mealLog = await _context.MealLogs
                .FirstOrDefaultAsync(ml => ml.UserId == userId && ml.Date == targetDate);

            var response = new NutritionProgressResponse
            {
                Date = targetDate,
                Goal = goal ?? new NutritionGoal
                {
                    DailyCalories = 0,
                    DailyProtein = 0,
                    DailyCarbohydrates = 0,
                    DailyFat = 0
                },
                Consumed = new NutritionTotals
                {
                    Calories = mealLog?.TotalCalories ?? 0,
                    Protein = mealLog?.TotalProtein ?? 0,
                    Carbohydrates = mealLog?.TotalCarbohydrates ?? 0,
                    Fat = mealLog?.TotalFat ?? 0,
                    Fiber = mealLog?.TotalFiber ?? 0,
                    Sodium = mealLog?.TotalSodium ?? 0,
                    Water = mealLog?.WaterIntake ?? 0
                }
            };

            // Calculate remaining and percentages
            response.Remaining = new NutritionTotals
            {
                Calories = response.Goal.DailyCalories - response.Consumed.Calories,
                Protein = response.Goal.DailyProtein - response.Consumed.Protein,
                Carbohydrates = response.Goal.DailyCarbohydrates - response.Consumed.Carbohydrates,
                Fat = response.Goal.DailyFat - response.Consumed.Fat
            };

            response.PercentageConsumed = new NutritionPercentages
            {
                Calories = response.Goal.DailyCalories > 0 ? (double)(response.Consumed.Calories / response.Goal.DailyCalories * 100) : 0,
                Protein = response.Goal.DailyProtein > 0 ? (double)(response.Consumed.Protein / response.Goal.DailyProtein * 100) : 0,
                Carbohydrates = response.Goal.DailyCarbohydrates > 0 ? (double)(response.Consumed.Carbohydrates / response.Goal.DailyCarbohydrates * 100) : 0,
                Fat = response.Goal.DailyFat > 0 ? (double)(response.Consumed.Fat / response.Goal.DailyFat * 100) : 0
            };

            return Ok(response);
        }

        /// <summary>
        /// Delete a nutrition goal
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNutritionGoal(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var goal = await _context.NutritionGoals.FindAsync(id);
            if (goal == null || goal.UserId != userId)
            {
                return NotFound();
            }

            _context.NutritionGoals.Remove(goal);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task DeactivateOtherGoals(int userId)
        {
            var activeGoals = await _context.NutritionGoals
                .Where(ng => ng.UserId == userId && ng.IsActive)
                .ToListAsync();

            foreach (var g in activeGoals)
            {
                g.IsActive = false;
                g.UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    public class NutritionProgressResponse
    {
        public DateTime Date { get; set; }
        public NutritionGoal Goal { get; set; } = null!;
        public NutritionTotals Consumed { get; set; } = new();
        public NutritionTotals Remaining { get; set; } = new();
        public NutritionPercentages PercentageConsumed { get; set; } = new();
    }

    public class NutritionTotals
    {
        public decimal Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbohydrates { get; set; }
        public decimal Fat { get; set; }
        public decimal? Fiber { get; set; }
        public decimal? Sodium { get; set; }
        public decimal? Water { get; set; }
    }

    public class NutritionPercentages
    {
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Carbohydrates { get; set; }
        public double Fat { get; set; }
    }
}
