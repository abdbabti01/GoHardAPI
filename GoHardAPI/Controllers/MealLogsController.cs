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
    public class MealLogsController : ControllerBase
    {
        private readonly TrainingContext _context;

        public MealLogsController(TrainingContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        /// <summary>
        /// Get meal logs for a date range
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MealLog>>> GetMealLogs(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 30)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var query = _context.MealLogs
                .Where(ml => ml.UserId == userId);

            if (startDate.HasValue)
            {
                var start = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
                query = query.Where(ml => ml.Date >= start);
            }

            if (endDate.HasValue)
            {
                var end = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1), DateTimeKind.Utc);
                query = query.Where(ml => ml.Date < end);
            }

            var mealLogs = await query
                .Include(ml => ml.MealEntries)
                    .ThenInclude(me => me.FoodItems)
                .OrderByDescending(ml => ml.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(mealLogs);
        }

        /// <summary>
        /// Get a specific meal log by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<MealLog>> GetMealLog(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var mealLog = await _context.MealLogs
                .Include(ml => ml.MealEntries)
                    .ThenInclude(me => me.FoodItems)
                .FirstOrDefaultAsync(ml => ml.Id == id && ml.UserId == userId);

            if (mealLog == null)
            {
                return NotFound();
            }

            return Ok(mealLog);
        }

        /// <summary>
        /// Get meal log for a specific date
        /// </summary>
        [HttpGet("date/{date}")]
        public async Task<ActionResult<MealLog>> GetMealLogByDate(DateTime date)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var targetDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);

            var mealLog = await _context.MealLogs
                .Include(ml => ml.MealEntries)
                    .ThenInclude(me => me.FoodItems)
                .FirstOrDefaultAsync(ml => ml.Date == targetDate && ml.UserId == userId);

            if (mealLog == null)
            {
                return NotFound();
            }

            return Ok(mealLog);
        }

        /// <summary>
        /// Get today's meal log (creates if not exists)
        /// </summary>
        [HttpGet("today")]
        public async Task<ActionResult<MealLog>> GetTodaysMealLog()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

            var mealLog = await _context.MealLogs
                .Include(ml => ml.MealEntries)
                    .ThenInclude(me => me.FoodItems)
                .FirstOrDefaultAsync(ml => ml.Date == today && ml.UserId == userId);

            if (mealLog == null)
            {
                // Create today's meal log with default meal entries
                mealLog = new MealLog
                {
                    UserId = userId,
                    Date = today,
                    CreatedAt = DateTime.UtcNow
                };

                // Create default meal entries for the day
                mealLog.MealEntries = new List<MealEntry>
                {
                    new MealEntry { MealType = MealTypes.Breakfast, CreatedAt = DateTime.UtcNow },
                    new MealEntry { MealType = MealTypes.Lunch, CreatedAt = DateTime.UtcNow },
                    new MealEntry { MealType = MealTypes.Dinner, CreatedAt = DateTime.UtcNow },
                    new MealEntry { MealType = MealTypes.Snack, CreatedAt = DateTime.UtcNow }
                };

                _context.MealLogs.Add(mealLog);
                await _context.SaveChangesAsync();
            }

            return Ok(mealLog);
        }

        /// <summary>
        /// Create a new meal log
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<MealLog>> CreateMealLog([FromBody] MealLog mealLog)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var targetDate = DateTime.SpecifyKind(mealLog.Date.Date, DateTimeKind.Utc);

            // Check if a meal log already exists for this date
            var existing = await _context.MealLogs
                .FirstOrDefaultAsync(ml => ml.Date == targetDate && ml.UserId == userId);

            if (existing != null)
            {
                return BadRequest(new { message = "A meal log already exists for this date" });
            }

            mealLog.UserId = userId;
            mealLog.Date = targetDate;
            mealLog.CreatedAt = DateTime.UtcNow;

            _context.MealLogs.Add(mealLog);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMealLog), new { id = mealLog.Id }, mealLog);
        }

        /// <summary>
        /// Update a meal log
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMealLog(int id, [FromBody] MealLog mealLog)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var existing = await _context.MealLogs.FindAsync(id);
            if (existing == null || existing.UserId != userId)
            {
                return NotFound();
            }

            existing.Notes = mealLog.Notes;
            existing.WaterIntake = mealLog.WaterIntake;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Update water intake for a meal log
        /// </summary>
        [HttpPut("{id}/water")]
        public async Task<IActionResult> UpdateWaterIntake(int id, [FromBody] decimal waterIntake)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var mealLog = await _context.MealLogs.FindAsync(id);
            if (mealLog == null || mealLog.UserId != userId)
            {
                return NotFound();
            }

            mealLog.WaterIntake = waterIntake;
            mealLog.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Recalculate totals for a meal log
        /// </summary>
        [HttpPost("{id}/recalculate")]
        public async Task<ActionResult<MealLog>> RecalculateTotals(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var mealLog = await _context.MealLogs
                .Include(ml => ml.MealEntries)
                    .ThenInclude(me => me.FoodItems)
                .FirstOrDefaultAsync(ml => ml.Id == id && ml.UserId == userId);

            if (mealLog == null)
            {
                return NotFound();
            }

            // Recalculate each meal entry first
            foreach (var entry in mealLog.MealEntries)
            {
                entry.RecalculateTotals();
            }

            // Then recalculate the meal log totals (consumed meals only)
            mealLog.RecalculateTotals(consumedOnly: true);

            await _context.SaveChangesAsync();

            return Ok(mealLog);
        }

        /// <summary>
        /// Delete a meal log
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMealLog(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var mealLog = await _context.MealLogs.FindAsync(id);
            if (mealLog == null || mealLog.UserId != userId)
            {
                return NotFound();
            }

            _context.MealLogs.Remove(mealLog);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Clear all food items from a meal log (keeps meal entries, removes food items and resets totals)
        /// </summary>
        [HttpPost("{id}/clear")]
        public async Task<ActionResult<MealLog>> ClearAllFood(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var mealLog = await _context.MealLogs
                .Include(ml => ml.MealEntries)
                    .ThenInclude(me => me.FoodItems)
                .FirstOrDefaultAsync(ml => ml.Id == id && ml.UserId == userId);

            if (mealLog == null)
            {
                return NotFound();
            }

            // Remove all food items from all meal entries
            foreach (var entry in mealLog.MealEntries)
            {
                if (entry.FoodItems != null && entry.FoodItems.Any())
                {
                    _context.FoodItems.RemoveRange(entry.FoodItems);
                }

                // Reset meal entry totals and consumption status
                entry.TotalCalories = 0;
                entry.TotalProtein = 0;
                entry.TotalCarbohydrates = 0;
                entry.TotalFat = 0;
                entry.TotalFiber = 0;
                entry.TotalSodium = 0;
                entry.IsConsumed = false;
                entry.ConsumedAt = null;
                entry.UpdatedAt = DateTime.UtcNow;
            }

            // Reset meal log totals
            mealLog.TotalCalories = 0;
            mealLog.TotalProtein = 0;
            mealLog.TotalCarbohydrates = 0;
            mealLog.TotalFat = 0;
            mealLog.TotalFiber = 0;
            mealLog.TotalSodium = 0;
            mealLog.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Reload to get the updated data
            mealLog = await _context.MealLogs
                .Include(ml => ml.MealEntries)
                    .ThenInclude(me => me.FoodItems)
                .FirstOrDefaultAsync(ml => ml.Id == id);

            return Ok(mealLog);
        }
    }
}
