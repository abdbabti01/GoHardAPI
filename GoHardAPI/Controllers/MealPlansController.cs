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
    public class MealPlansController : ControllerBase
    {
        private readonly TrainingContext _context;

        public MealPlansController(TrainingContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        /// <summary>
        /// Get all meal plans for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MealPlan>>> GetMealPlans()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var plans = await _context.MealPlans
                .Where(mp => mp.UserId == userId)
                .OrderByDescending(mp => mp.IsActive)
                .ThenByDescending(mp => mp.CreatedAt)
                .ToListAsync();

            return Ok(plans);
        }

        /// <summary>
        /// Get a specific meal plan with all details
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<MealPlan>> GetMealPlan(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var plan = await _context.MealPlans
                .Include(mp => mp.Days)
                    .ThenInclude(d => d.Meals)
                        .ThenInclude(m => m.FoodItems)
                            .ThenInclude(fi => fi.FoodTemplate)
                .FirstOrDefaultAsync(mp => mp.Id == id && mp.UserId == userId);

            if (plan == null)
            {
                return NotFound();
            }

            return Ok(plan);
        }

        /// <summary>
        /// Create a new meal plan
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<MealPlan>> CreateMealPlan([FromBody] MealPlan plan)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            plan.UserId = userId;
            plan.CreatedAt = DateTime.UtcNow;

            _context.MealPlans.Add(plan);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMealPlan), new { id = plan.Id }, plan);
        }

        /// <summary>
        /// Update a meal plan
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMealPlan(int id, [FromBody] MealPlan plan)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var existing = await _context.MealPlans.FindAsync(id);
            if (existing == null || existing.UserId != userId)
            {
                return NotFound();
            }

            existing.Name = plan.Name;
            existing.Description = plan.Description;
            existing.DurationDays = plan.DurationDays;
            existing.IsActive = plan.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Add a day to a meal plan
        /// </summary>
        [HttpPost("{id}/days")]
        public async Task<ActionResult<MealPlanDay>> AddDay(int id, [FromBody] MealPlanDay day)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var plan = await _context.MealPlans.FindAsync(id);
            if (plan == null || plan.UserId != userId)
            {
                return NotFound();
            }

            day.MealPlanId = id;
            _context.MealPlanDays.Add(day);
            await _context.SaveChangesAsync();

            return Ok(day);
        }

        /// <summary>
        /// Add a meal to a meal plan day
        /// </summary>
        [HttpPost("days/{dayId}/meals")]
        public async Task<ActionResult<MealPlanMeal>> AddMeal(int dayId, [FromBody] MealPlanMeal meal)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var day = await _context.MealPlanDays
                .Include(d => d.MealPlan)
                .FirstOrDefaultAsync(d => d.Id == dayId);

            if (day == null || day.MealPlan?.UserId != userId)
            {
                return NotFound();
            }

            meal.MealPlanDayId = dayId;
            _context.MealPlanMeals.Add(meal);
            await _context.SaveChangesAsync();

            return Ok(meal);
        }

        /// <summary>
        /// Add a food item to a meal plan meal
        /// </summary>
        [HttpPost("meals/{mealId}/foods")]
        public async Task<ActionResult<MealPlanFoodItem>> AddFoodItem(int mealId, [FromBody] MealPlanFoodItem foodItem)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var meal = await _context.MealPlanMeals
                .Include(m => m.MealPlanDay)
                    .ThenInclude(d => d!.MealPlan)
                .FirstOrDefaultAsync(m => m.Id == mealId);

            if (meal == null || meal.MealPlanDay?.MealPlan?.UserId != userId)
            {
                return NotFound();
            }

            // If food template ID is provided, populate from template
            if (foodItem.FoodTemplateId.HasValue)
            {
                var template = await _context.FoodTemplates.FindAsync(foodItem.FoodTemplateId);
                if (template != null)
                {
                    foodItem.Name = template.Name;
                    foodItem.ServingSize = template.ServingSize;
                    foodItem.ServingUnit = template.ServingUnit;
                    foodItem.Calories = template.Calories * foodItem.Quantity;
                    foodItem.Protein = template.Protein * foodItem.Quantity;
                    foodItem.Carbohydrates = template.Carbohydrates * foodItem.Quantity;
                    foodItem.Fat = template.Fat * foodItem.Quantity;
                }
            }

            foodItem.MealPlanMealId = mealId;
            _context.MealPlanFoodItems.Add(foodItem);
            await _context.SaveChangesAsync();

            // Recalculate meal totals
            meal.RecalculateTotals();
            await _context.SaveChangesAsync();

            return Ok(foodItem);
        }

        /// <summary>
        /// Apply a meal plan to a date range
        /// </summary>
        [HttpPost("{id}/apply")]
        public async Task<ActionResult<IEnumerable<MealLog>>> ApplyMealPlan(int id, [FromBody] ApplyMealPlanRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var plan = await _context.MealPlans
                .Include(mp => mp.Days)
                    .ThenInclude(d => d.Meals)
                        .ThenInclude(m => m.FoodItems)
                .FirstOrDefaultAsync(mp => mp.Id == id && mp.UserId == userId);

            if (plan == null)
            {
                return NotFound();
            }

            var createdLogs = new List<MealLog>();
            var startDate = DateTime.SpecifyKind(request.StartDate.Date, DateTimeKind.Utc);

            for (int i = 0; i < plan.DurationDays && i < plan.Days.Count; i++)
            {
                var currentDate = startDate.AddDays(i);
                var planDay = plan.Days.FirstOrDefault(d => d.DayNumber == i + 1);

                if (planDay == null) continue;

                // Check if meal log already exists for this date
                var existingLog = await _context.MealLogs
                    .FirstOrDefaultAsync(ml => ml.UserId == userId && ml.Date == currentDate);

                if (existingLog != null && !request.OverwriteExisting)
                {
                    continue;
                }

                if (existingLog != null && request.OverwriteExisting)
                {
                    _context.MealLogs.Remove(existingLog);
                }

                // Create new meal log from plan day
                var mealLog = new MealLog
                {
                    UserId = userId,
                    Date = currentDate,
                    Notes = $"From meal plan: {plan.Name} - Day {planDay.DayNumber}",
                    CreatedAt = DateTime.UtcNow
                };

                foreach (var planMeal in planDay.Meals)
                {
                    var mealEntry = new MealEntry
                    {
                        MealType = planMeal.MealType,
                        Name = planMeal.Name,
                        ScheduledTime = planMeal.ScheduledTime.HasValue
                            ? currentDate.Add(planMeal.ScheduledTime.Value)
                            : null,
                        Notes = planMeal.Notes,
                        CreatedAt = DateTime.UtcNow
                    };

                    foreach (var planFood in planMeal.FoodItems)
                    {
                        var foodItem = new FoodItem
                        {
                            FoodTemplateId = planFood.FoodTemplateId,
                            Name = planFood.Name,
                            Quantity = planFood.Quantity,
                            ServingSize = planFood.ServingSize,
                            ServingUnit = planFood.ServingUnit,
                            Calories = planFood.Calories,
                            Protein = planFood.Protein,
                            Carbohydrates = planFood.Carbohydrates,
                            Fat = planFood.Fat,
                            CreatedAt = DateTime.UtcNow
                        };
                        mealEntry.FoodItems.Add(foodItem);
                    }

                    mealEntry.RecalculateTotals();
                    mealLog.MealEntries.Add(mealEntry);
                }

                mealLog.RecalculateTotals();
                _context.MealLogs.Add(mealLog);
                createdLogs.Add(mealLog);
            }

            await _context.SaveChangesAsync();

            return Ok(createdLogs);
        }

        /// <summary>
        /// Delete a meal plan
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMealPlan(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var plan = await _context.MealPlans.FindAsync(id);
            if (plan == null || plan.UserId != userId)
            {
                return NotFound();
            }

            _context.MealPlans.Remove(plan);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Delete a meal plan day
        /// </summary>
        [HttpDelete("days/{dayId}")]
        public async Task<IActionResult> DeleteDay(int dayId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var day = await _context.MealPlanDays
                .Include(d => d.MealPlan)
                .FirstOrDefaultAsync(d => d.Id == dayId);

            if (day == null || day.MealPlan?.UserId != userId)
            {
                return NotFound();
            }

            _context.MealPlanDays.Remove(day);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Delete a meal plan meal
        /// </summary>
        [HttpDelete("meals/{mealId}")]
        public async Task<IActionResult> DeleteMeal(int mealId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var meal = await _context.MealPlanMeals
                .Include(m => m.MealPlanDay)
                    .ThenInclude(d => d!.MealPlan)
                .FirstOrDefaultAsync(m => m.Id == mealId);

            if (meal == null || meal.MealPlanDay?.MealPlan?.UserId != userId)
            {
                return NotFound();
            }

            _context.MealPlanMeals.Remove(meal);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Delete a meal plan food item
        /// </summary>
        [HttpDelete("foods/{foodId}")]
        public async Task<IActionResult> DeleteFoodItem(int foodId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var food = await _context.MealPlanFoodItems
                .Include(f => f.MealPlanMeal)
                    .ThenInclude(m => m!.MealPlanDay)
                        .ThenInclude(d => d!.MealPlan)
                .FirstOrDefaultAsync(f => f.Id == foodId);

            if (food == null || food.MealPlanMeal?.MealPlanDay?.MealPlan?.UserId != userId)
            {
                return NotFound();
            }

            _context.MealPlanFoodItems.Remove(food);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class ApplyMealPlanRequest
    {
        public DateTime StartDate { get; set; }
        public bool OverwriteExisting { get; set; } = false;
    }
}
