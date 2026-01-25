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
    public class MealEntriesController : ControllerBase
    {
        private readonly TrainingContext _context;

        public MealEntriesController(TrainingContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        /// <summary>
        /// Get meal entries for a meal log
        /// </summary>
        [HttpGet("meallog/{mealLogId}")]
        public async Task<ActionResult<IEnumerable<MealEntry>>> GetMealEntries(int mealLogId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            // Verify the meal log belongs to the user
            var mealLog = await _context.MealLogs.FindAsync(mealLogId);
            if (mealLog == null || mealLog.UserId != userId)
            {
                return NotFound();
            }

            var entries = await _context.MealEntries
                .Where(me => me.MealLogId == mealLogId)
                .Include(me => me.FoodItems)
                .OrderBy(me => me.MealType)
                .ToListAsync();

            return Ok(entries);
        }

        /// <summary>
        /// Get a specific meal entry by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<MealEntry>> GetMealEntry(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var entry = await _context.MealEntries
                .Include(me => me.FoodItems)
                .Include(me => me.MealLog)
                .FirstOrDefaultAsync(me => me.Id == id);

            if (entry == null || entry.MealLog?.UserId != userId)
            {
                return NotFound();
            }

            return Ok(entry);
        }

        /// <summary>
        /// Create a new meal entry
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<MealEntry>> CreateMealEntry([FromBody] MealEntry entry)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            // Verify the meal log belongs to the user
            var mealLog = await _context.MealLogs.FindAsync(entry.MealLogId);
            if (mealLog == null || mealLog.UserId != userId)
            {
                return NotFound(new { message = "Meal log not found" });
            }

            entry.CreatedAt = DateTime.UtcNow;

            _context.MealEntries.Add(entry);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMealEntry), new { id = entry.Id }, entry);
        }

        /// <summary>
        /// Update a meal entry
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMealEntry(int id, [FromBody] MealEntry entry)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var existing = await _context.MealEntries
                .Include(me => me.MealLog)
                .FirstOrDefaultAsync(me => me.Id == id);

            if (existing == null || existing.MealLog?.UserId != userId)
            {
                return NotFound();
            }

            existing.MealType = entry.MealType;
            existing.Name = entry.Name;
            existing.ScheduledTime = entry.ScheduledTime;
            existing.Notes = entry.Notes;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Mark a meal entry as consumed
        /// </summary>
        [HttpPut("{id}/consume")]
        public async Task<IActionResult> MarkAsConsumed(int id, [FromBody] MarkConsumedRequest? request = null)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var entry = await _context.MealEntries
                .Include(me => me.MealLog)
                .FirstOrDefaultAsync(me => me.Id == id);

            if (entry == null || entry.MealLog?.UserId != userId)
            {
                return NotFound();
            }

            entry.IsConsumed = request?.IsConsumed ?? true;
            entry.ConsumedAt = entry.IsConsumed ? (request?.ConsumedAt ?? DateTime.UtcNow) : null;
            entry.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Get planned vs consumed summary for today
        /// </summary>
        [HttpGet("today/status")]
        public async Task<ActionResult<MealStatusResponse>> GetTodayMealStatus()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            return await GetMealStatusForDate(userId, today);
        }

        /// <summary>
        /// Get planned vs consumed summary for a specific date
        /// </summary>
        [HttpGet("date/{date}/status")]
        public async Task<ActionResult<MealStatusResponse>> GetDateMealStatus(DateTime date)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var targetDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
            return await GetMealStatusForDate(userId, targetDate);
        }

        private async Task<ActionResult<MealStatusResponse>> GetMealStatusForDate(int userId, DateTime date)
        {
            var mealLog = await _context.MealLogs
                .Include(ml => ml.MealEntries)
                    .ThenInclude(me => me.FoodItems)
                        .ThenInclude(fi => fi.FoodTemplate)
                .FirstOrDefaultAsync(ml => ml.UserId == userId && ml.Date == date);

            if (mealLog == null)
            {
                return Ok(new MealStatusResponse
                {
                    Date = date,
                    MealLogId = null,
                    Meals = new List<MealStatusItem>(),
                    PlannedTotals = new MacroTotals(),
                    ConsumedTotals = new MacroTotals(),
                    MealsConsumed = 0,
                    TotalMeals = 0
                });
            }

            var meals = mealLog.MealEntries
                .OrderBy(me => GetMealTypeOrder(me.MealType))
                .Select(me => new MealStatusItem
                {
                    MealEntryId = me.Id,
                    MealType = me.MealType,
                    Name = me.Name,
                    ScheduledTime = me.ScheduledTime,
                    IsConsumed = me.IsConsumed,
                    ConsumedAt = me.ConsumedAt,
                    Calories = me.TotalCalories,
                    Protein = me.TotalProtein,
                    Carbohydrates = me.TotalCarbohydrates,
                    Fat = me.TotalFat,
                    Foods = me.FoodItems.Select(fi => new FoodStatusItem
                    {
                        FoodItemId = fi.Id,
                        Name = fi.Name,
                        Quantity = fi.Quantity,
                        ServingSize = fi.ServingSize,
                        ServingUnit = fi.ServingUnit,
                        Calories = fi.Calories,
                        Protein = fi.Protein,
                        Carbohydrates = fi.Carbohydrates,
                        Fat = fi.Fat,
                        FoodTemplateId = fi.FoodTemplateId
                    }).ToList()
                }).ToList();

            var consumedMeals = mealLog.MealEntries.Where(me => me.IsConsumed).ToList();

            var response = new MealStatusResponse
            {
                Date = date,
                MealLogId = mealLog.Id,
                Meals = meals,
                PlannedTotals = new MacroTotals
                {
                    Calories = mealLog.TotalCalories,
                    Protein = mealLog.TotalProtein,
                    Carbohydrates = mealLog.TotalCarbohydrates,
                    Fat = mealLog.TotalFat
                },
                ConsumedTotals = new MacroTotals
                {
                    Calories = consumedMeals.Sum(me => me.TotalCalories),
                    Protein = consumedMeals.Sum(me => me.TotalProtein),
                    Carbohydrates = consumedMeals.Sum(me => me.TotalCarbohydrates),
                    Fat = consumedMeals.Sum(me => me.TotalFat)
                },
                MealsConsumed = consumedMeals.Count,
                TotalMeals = mealLog.MealEntries.Count
            };

            return Ok(response);
        }

        private static int GetMealTypeOrder(string mealType)
        {
            return mealType.ToLower() switch
            {
                "breakfast" => 1,
                "lunch" => 2,
                "dinner" => 3,
                "snack" => 4,
                _ => 5
            };
        }

        /// <summary>
        /// Delete a meal entry
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMealEntry(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var entry = await _context.MealEntries
                .Include(me => me.MealLog)
                .FirstOrDefaultAsync(me => me.Id == id);

            if (entry == null || entry.MealLog?.UserId != userId)
            {
                return NotFound();
            }

            _context.MealEntries.Remove(entry);
            await _context.SaveChangesAsync();

            // Recalculate meal log totals
            var mealLog = await _context.MealLogs
                .Include(ml => ml.MealEntries)
                    .ThenInclude(me => me.FoodItems)
                .FirstOrDefaultAsync(ml => ml.Id == entry.MealLogId);

            if (mealLog != null)
            {
                mealLog.RecalculateTotals();
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }
    }

    public class MarkConsumedRequest
    {
        public bool IsConsumed { get; set; } = true;
        public DateTime? ConsumedAt { get; set; }
    }

    public class MealStatusResponse
    {
        public DateTime Date { get; set; }
        public int? MealLogId { get; set; }
        public List<MealStatusItem> Meals { get; set; } = new();
        public MacroTotals PlannedTotals { get; set; } = new();
        public MacroTotals ConsumedTotals { get; set; } = new();
        public int MealsConsumed { get; set; }
        public int TotalMeals { get; set; }
    }

    public class MealStatusItem
    {
        public int MealEntryId { get; set; }
        public string MealType { get; set; } = "";
        public string? Name { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public bool IsConsumed { get; set; }
        public DateTime? ConsumedAt { get; set; }
        public decimal Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbohydrates { get; set; }
        public decimal Fat { get; set; }
        public List<FoodStatusItem> Foods { get; set; } = new();
    }

    public class FoodStatusItem
    {
        public int FoodItemId { get; set; }
        public string Name { get; set; } = "";
        public decimal Quantity { get; set; }
        public decimal ServingSize { get; set; }
        public string ServingUnit { get; set; } = "g";
        public decimal Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbohydrates { get; set; }
        public decimal Fat { get; set; }
        public int? FoodTemplateId { get; set; }
    }

    public class MacroTotals
    {
        public decimal Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbohydrates { get; set; }
        public decimal Fat { get; set; }
    }
}
