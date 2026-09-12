using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GoHardAPI.Data;
using GoHardAPI.Models;
using System.Security.Claims;

namespace GoHardAPI.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
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
                .Where(g => g.UserId == userId && !g.IsDeleted);

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
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId && !g.IsDeleted);

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
            if (existingGoal == null || existingGoal.UserId != userId || existingGoal.IsDeleted)
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
        /// Get deletion impact for a goal: what will be detached and preserved.
        /// Deleting a goal no longer destroys linked Program/Session history — it only
        /// unlinks (detaches) that history from the goal being removed.
        /// </summary>
        [HttpGet("{id}/deletion-impact")]
        public async Task<ActionResult<object>> GetDeletionImpact(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var goal = await _context.Goals.FindAsync(id);
            if (goal == null || goal.UserId != userId || goal.IsDeleted)
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
                sessionsCount,
                message = programsCount > 0
                    ? $"This will unlink {programsCount} program(s) from this goal. They (and their {sessionsCount} session(s)) will be preserved and remain in your workout history — only the link to this goal is removed."
                    : null
            });
        }

        /// <summary>
        /// Delete a goal. This never issues a physical DELETE against the Goals table —
        /// it detaches linked Programs (GoalId set to null, for display cleanliness) and
        /// then soft-deletes the goal itself (<see cref="Goal.IsDeleted"/>/<see
        /// cref="Goal.DeletedAt"/>). Every other endpoint on this controller treats a
        /// soft-deleted goal as not found, so from the user's perspective the goal is
        /// gone exactly as a hard delete would have made it appear.
        ///
        /// This is what closes the concurrent-creation-vs-delete race by construction,
        /// not by locking: because the Goals row is never physically removed, the
        /// database's ON DELETE CASCADE from Programs→Goals can never fire for this
        /// goal, no matter when a concurrent request creates and links a new Program to
        /// it relative to this request's detach step. The only residual effect of a
        /// Program being linked in that narrow window is that it may keep pointing at
        /// this (now soft-deleted, still fetchable-by-id-internally) goal row instead of
        /// being detached — a cosmetic staleness, never data loss — because the detach
        /// query and the soft-delete write are not one atomic statement. See
        /// GoalProgramHistoryPreservationPostgresTests for a live-concurrency test that
        /// this can never destroy the concurrently-created Program.
        ///
        /// This makes the DELETE endpoint itself safe for old clients: the URL, verb,
        /// and 204 response are unchanged, but the destructive cascade an old client
        /// could accidentally trigger no longer exists.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGoal(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var goal = await _context.Goals.FindAsync(id);
            if (goal == null || goal.UserId != userId || goal.IsDeleted)
            {
                return NotFound();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var linkedPrograms = await _context.Programs
                    .Where(p => p.GoalId == id && p.UserId == userId)
                    .ToListAsync();

                if (linkedPrograms.Count > 0)
                {
                    foreach (var program in linkedPrograms)
                    {
                        program.GoalId = null;
                    }
                    await _context.SaveChangesAsync();
                }

                goal.IsDeleted = true;
                goal.DeletedAt = DateTime.UtcNow;
                goal.IsActive = false;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

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
            if (goal == null || goal.UserId != userId || goal.IsDeleted)
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
        /// Archive a goal: remove it from active use WITHOUT completing it. Archiving is
        /// not completion — it never sets IsCompleted/CompletedAt and never awards
        /// progress. It also never cascades to linked Programs or nutrition targets;
        /// any further action on those must be explicit. Archived goals remain
        /// discoverable via GET /goals.
        /// </summary>
        [HttpPut("{id}/archive")]
        public async Task<IActionResult> ArchiveGoal(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var goal = await _context.Goals.FindAsync(id);
            if (goal == null || goal.UserId != userId || goal.IsDeleted)
            {
                return NotFound();
            }

            goal.IsArchived = true;
            goal.ArchivedAt = DateTime.UtcNow;
            goal.IsActive = false;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Restore an archived goal to active use.
        /// </summary>
        [HttpPut("{id}/unarchive")]
        public async Task<IActionResult> UnarchiveGoal(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var goal = await _context.Goals.FindAsync(id);
            if (goal == null || goal.UserId != userId || goal.IsDeleted)
            {
                return NotFound();
            }

            goal.IsArchived = false;
            goal.ArchivedAt = null;
            if (!goal.IsCompleted)
            {
                goal.IsActive = true;
            }

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
            if (goal == null || goal.UserId != userId || goal.IsDeleted)
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
            if (goal == null || goal.UserId != userId || goal.IsDeleted)
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
