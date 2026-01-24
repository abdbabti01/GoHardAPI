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
}
