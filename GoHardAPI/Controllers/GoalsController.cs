using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GoHardAPI.Data;
using GoHardAPI.Models;
using System.Security.Claims;

namespace GoHardAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GoalsController : ControllerBase
    {
        private readonly TrainingContext _context;

        public GoalsController(TrainingContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        /// <summary>
        /// Get all goals for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Goal>>> GetGoals([FromQuery] bool? isActive = null)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var query = _context.Goals
                .Where(g => g.UserId == userId);

            if (isActive.HasValue)
            {
                query = query.Where(g => g.IsActive == isActive.Value);
            }

            var goals = await query
                .Include(g => g.ProgressHistory)
                .OrderByDescending(g => g.IsActive)
                .ThenByDescending(g => g.CreatedAt)
                .ToListAsync();

            return Ok(goals);
        }

        /// <summary>
        /// Get a specific goal by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Goal>> GetGoal(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var goal = await _context.Goals
                .Include(g => g.ProgressHistory.OrderBy(p => p.RecordedAt))
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

            if (goal == null)
            {
                return NotFound();
            }

            return Ok(goal);
        }

        /// <summary>
        /// Create a new goal
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Goal>> CreateGoal(Goal goal)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            goal.UserId = userId;
            goal.CreatedAt = DateTime.UtcNow;
            goal.StartDate = goal.StartDate == default ? DateTime.UtcNow : goal.StartDate;
            goal.CurrentValue = 0;
            goal.IsCompleted = false;

            _context.Goals.Add(goal);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGoal), new { id = goal.Id }, goal);
        }

        /// <summary>
        /// Update an existing goal
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGoal(int id, Goal goal)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            if (id != goal.Id)
            {
                return BadRequest();
            }

            var existingGoal = await _context.Goals.FindAsync(id);
            if (existingGoal == null || existingGoal.UserId != userId)
            {
                return NotFound();
            }

            // Update fields
            existingGoal.GoalType = goal.GoalType;
            existingGoal.TargetValue = goal.TargetValue;
            existingGoal.CurrentValue = goal.CurrentValue;
            existingGoal.Unit = goal.Unit;
            existingGoal.TimeFrame = goal.TimeFrame;
            existingGoal.TargetDate = goal.TargetDate;
            existingGoal.IsActive = goal.IsActive;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GoalExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        /// <summary>
        /// Delete a goal
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGoal(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var goal = await _context.Goals.FindAsync(id);
            if (goal == null || goal.UserId != userId)
            {
                return NotFound();
            }

            _context.Goals.Remove(goal);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Mark a goal as completed
        /// </summary>
        [HttpPut("{id}/complete")]
        public async Task<IActionResult> CompleteGoal(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var goal = await _context.Goals.FindAsync(id);
            if (goal == null || goal.UserId != userId)
            {
                return NotFound();
            }

            goal.IsCompleted = true;
            goal.CompletedAt = DateTime.UtcNow;
            goal.IsActive = false;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Add a progress entry for a goal
        /// </summary>
        [HttpPost("{id}/progress")]
        public async Task<ActionResult<GoalProgress>> AddProgress(int id, [FromBody] GoalProgress progress)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var goal = await _context.Goals.FindAsync(id);
            if (goal == null || goal.UserId != userId)
            {
                return NotFound();
            }

            progress.GoalId = id;
            progress.RecordedAt = DateTime.UtcNow;

            _context.GoalProgressHistory.Add(progress);

            // Update current value
            goal.CurrentValue = progress.Value;

            // Check if goal is completed
            if (goal.CurrentValue >= goal.TargetValue && !goal.IsCompleted)
            {
                goal.IsCompleted = true;
                goal.CompletedAt = DateTime.UtcNow;
                goal.IsActive = false;
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGoal), new { id = goal.Id }, progress);
        }

        /// <summary>
        /// Get progress history for a goal
        /// </summary>
        [HttpGet("{id}/history")]
        public async Task<ActionResult<IEnumerable<GoalProgress>>> GetProgressHistory(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var goal = await _context.Goals.FindAsync(id);
            if (goal == null || goal.UserId != userId)
            {
                return NotFound();
            }

            var history = await _context.GoalProgressHistory
                .Where(gp => gp.GoalId == id)
                .OrderBy(gp => gp.RecordedAt)
                .ToListAsync();

            return Ok(history);
        }

        private bool GoalExists(int id)
        {
            return _context.Goals.Any(e => e.Id == id);
        }
    }
}
