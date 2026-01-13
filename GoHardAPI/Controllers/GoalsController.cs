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
            // Keep the user's provided CurrentValue - don't force it to 0
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
        /// Get deletion impact for a goal (how many programs and sessions will be deleted)
        /// </summary>
        [HttpGet("{id}/deletion-impact")]
        public async Task<ActionResult<object>> GetDeletionImpact(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var goal = await _context.Goals.FindAsync(id);
            if (goal == null || goal.UserId != userId)
            {
                return NotFound();
            }

            // Count programs linked to this goal
            var programsCount = await _context.Programs
                .Where(p => p.GoalId == id && p.UserId == userId)
                .CountAsync();

            // Count sessions linked to those programs
            var sessionsCount = await _context.Sessions
                .Where(s => s.UserId == userId && s.ProgramId != null &&
                       _context.Programs.Any(p => p.Id == s.ProgramId && p.GoalId == id))
                .CountAsync();

            return Ok(new
            {
                programsCount,
                sessionsCount
            });
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

            // Note: Progress values represent incremental changes (deltas), not absolute values.
            // For weight loss: each entry is pounds lost (e.g., 2 lbs, 5 lbs)
            // For increase goals: each entry is progress made (e.g., 1 workout, 3 workouts)
            // The Goal.CurrentValue remains as the starting value, and progress is calculated
            // by summing all GoalProgress entries via the TotalProgress property.
            //
            // Goals are not automatically marked as complete when adding progress.
            // Users should manually mark goals as complete using the /complete endpoint.

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
