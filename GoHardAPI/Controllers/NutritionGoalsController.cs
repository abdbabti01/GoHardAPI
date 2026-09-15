using Asp.Versioning;
using GoHardAPI.Data;
using GoHardAPI.DTOs;
using GoHardAPI.Models;
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

            var goal = await NutritionTargetService.ResolveForDateAsync(_context, userId, DateTime.UtcNow);

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
        /// Create a new nutrition goal. When marked active (the normal case),
        /// this goes through <see cref="NutritionTargetService.SetActiveGoalAsync"/>
        /// so it becomes a new dated row rather than mutating any existing one -
        /// past dates keep whatever target applied to them. A goal explicitly
        /// created with <c>isActive: false</c> (e.g. a saved, not-yet-applied
        /// preset) is inserted as-is without touching the active row or its
        /// history.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<NutritionGoal>> CreateNutritionGoal([FromBody] NutritionGoal goal)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            if (goal.IsActive)
            {
                var effectiveDate = goal.EffectiveDate == default ? (DateTime?)null : goal.EffectiveDate;
                var created = await NutritionTargetService.SetActiveGoalAsync(_context, userId, goal, effectiveDate);
                return CreatedAtAction(nameof(GetNutritionGoal), new { id = created.Id }, created);
            }

            goal.UserId = userId;
            goal.CreatedAt = DateTime.UtcNow;
            if (goal.ProteinPercentage.HasValue || goal.CarbohydratesPercentage.HasValue || goal.FatPercentage.HasValue)
            {
                goal.CalculateMacrosFromPercentages();
            }

            _context.NutritionGoals.Add(goal);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetNutritionGoal), new { id = goal.Id }, goal);
        }

        /// <summary>
        /// Change the active nutrition target. This never mutates <paramref name="id"/>'s
        /// stored row in place - it inserts a new dated row (effective today, or
        /// the date carried on the request body) via
        /// <see cref="NutritionTargetService.SetActiveGoalAsync"/>, so whatever
        /// applied to past dates through the existing row(s) is preserved
        /// exactly as it was. <paramref name="id"/> is only used to confirm the
        /// caller owns an existing goal to change.
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

            var effectiveDate = goal.EffectiveDate == default ? (DateTime?)null : goal.EffectiveDate;
            await NutritionTargetService.SetActiveGoalAsync(_context, userId, goal, effectiveDate);

            return NoContent();
        }

        /// <summary>
        /// Start using a previously-saved goal's values again, effective today.
        /// This inserts a new dated row copied from <paramref name="id"/>'s
        /// values rather than flipping that historical row's own IsActive flag
        /// in place - the original row (and whatever date range it used to
        /// apply to) is left completely untouched.
        /// </summary>
        [HttpPut("{id}/activate")]
        public async Task<IActionResult> ActivateGoal(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var goal = await _context.NutritionGoals.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);
            if (goal == null || goal.UserId != userId)
            {
                return NotFound();
            }

            await NutritionTargetService.SetActiveGoalAsync(_context, userId, goal);

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

            // The target that applied to THIS date - never today's current
            // target applied retroactively to a past/future date.
            var goal = await NutritionTargetService.ResolveForDateAsync(_context, userId, targetDate);

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
        /// Get today's nutrition progress (planned and consumed values), derived live
        /// from today's MealLog entries. See NutritionProgressCalculator for the
        /// authoritative consumed/planned formula shared by all progress endpoints.
        /// </summary>
        [HttpGet("progress/today")]
        public async Task<ActionResult<NutritionProgressDto>> GetTodayProgress()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

            var activeGoal = await NutritionTargetService.ResolveForDateAsync(_context, userId, today);

            var progress = await NutritionProgressCalculator.CalculateAsync(_context, userId, today, activeGoal?.Id);

            return Ok(progress);
        }

        /// <summary>
        /// Get nutrition progress for a specific date, derived live from that date's
        /// MealLog entries, paired with the target that actually applied on
        /// that date - not today's current target.
        /// </summary>
        [HttpGet("progress/date/{date}")]
        public async Task<ActionResult<NutritionProgressDto>> GetProgressByDate(DateTime date)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var goalForDate = await NutritionTargetService.ResolveForDateAsync(_context, userId, date);

            var progress = await NutritionProgressCalculator.CalculateAsync(_context, userId, date, goalForDate?.Id);

            return Ok(progress);
        }

        /// <summary>
        /// Get nutrition progress with goal combined (single API call for dashboard).
        /// Both the progress AND the goal are resolved for the SAME requested
        /// date - a dashboard for a past date shows that date's actual target,
        /// never today's.
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<ActionResult<NutritionDashboardResponse>> GetDashboard([FromQuery] DateTime? date = null)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var targetDate = date.HasValue
                ? DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc)
                : DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

            var goal = await NutritionTargetService.ResolveForDateAsync(_context, userId, targetDate);

            var progress = await NutritionProgressCalculator.CalculateAsync(_context, userId, targetDate, goal?.Id);

            return Ok(new NutritionDashboardResponse
            {
                Date = progress.Date,
                Goal = goal,
                Progress = progress
            });
        }

        /// <summary>
        /// Remove a nutrition goal. This is a soft delete
        /// (<see cref="NutritionTargetService.SoftDeleteAsync"/>) - the row is
        /// kept so it still answers historical queries for dates before now;
        /// only dates from today onward stop seeing it, reverting cleanly to
        /// whatever target applied immediately before it (or "no target" if
        /// none did).
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNutritionGoal(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var deleted = await NutritionTargetService.SoftDeleteAsync(_context, userId, id);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }

        /// <summary>
        /// The nutrition target in effect for one calendar date - the
        /// authoritative, date-aware read every caller (Today and historical
        /// nutrition views alike) should use instead of guessing from
        /// <see cref="GetActiveGoal"/>'s synthesized sentinel. Returns
        /// <c>hasTarget: false</c>, never a guessed/backfilled value, for any
        /// date before the user's earliest recorded target.
        /// </summary>
        [HttpGet("for-date")]
        public async Task<ActionResult<NutritionTargetForDateResponse>> GetGoalForDate([FromQuery] DateTime date)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var goal = await NutritionTargetService.ResolveForDateAsync(_context, userId, date);

            return Ok(new NutritionTargetForDateResponse
            {
                Date = NutritionTargetService.NormalizeDate(date),
                HasTarget = goal != null,
                Goal = goal,
            });
        }

        /// <summary>
        /// Batch form of <see cref="GetGoalForDate"/> for a history view
        /// rendering many days at once - one round trip instead of one per day.
        /// </summary>
        [HttpGet("for-dates")]
        public async Task<ActionResult<List<NutritionTargetForDateResponse>>> GetGoalsForDateRange(
            [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            if (end < start)
            {
                return BadRequest(new { message = "end must not be before start" });
            }

            var resolved = await NutritionTargetService.ResolveForDateRangeAsync(_context, userId, start, end);

            var response = resolved
                .OrderBy(kv => kv.Key)
                .Select(kv => new NutritionTargetForDateResponse
                {
                    Date = kv.Key,
                    HasTarget = kv.Value != null,
                    Goal = kv.Value,
                })
                .ToList();

            return Ok(response);
        }
    }

    public class NutritionTargetForDateResponse
    {
        [System.Text.Json.Serialization.JsonConverter(typeof(Converters.DateOnlyJsonConverter))]
        public DateTime Date { get; set; }
        public bool HasTarget { get; set; }
        public NutritionGoal? Goal { get; set; }
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

    public class NutritionDashboardResponse
    {
        public DateTime Date { get; set; }
        public NutritionGoal? Goal { get; set; }
        public NutritionProgressDto Progress { get; set; } = null!;
    }
}
