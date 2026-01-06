using GoHardAPI.Data;
using GoHardAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GoHardAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SessionsController : ControllerBase
    {
        private readonly TrainingContext _context;

        public SessionsController(TrainingContext context)
        {
            _context = context;
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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Session>>> GetSessions()
        {
            var userId = GetCurrentUserId();
            return await _context.Sessions
                .Where(s => s.UserId == userId)
                .Include(ts => ts.Exercises)
                    .ThenInclude(e => e.ExerciseSets)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Session>> GetSession(int id)
        {
            var userId = GetCurrentUserId();
            var Session = await _context.Sessions
                .Include(ts => ts.Exercises)
                    .ThenInclude(e => e.ExerciseSets)
                .FirstOrDefaultAsync(ts => ts.Id == id && ts.UserId == userId);

            if (Session == null)
            {
                return NotFound();
            }

            return Session;
        }

        [HttpPost]
        public async Task<ActionResult<Session>> CreateSession(Session Session)
        {
            // Ensure the session belongs to the current authenticated user
            var userId = GetCurrentUserId();
            Session.UserId = userId;

            _context.Sessions.Add(Session);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSession), new { id = Session.Id }, Session);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSession(int id, Session Session)
        {
            if (id != Session.Id)
            {
                return BadRequest();
            }

            var userId = GetCurrentUserId();

            // Verify the session belongs to the current user
            var existingSession = await _context.Sessions.FindAsync(id);
            if (existingSession == null || existingSession.UserId != userId)
            {
                return NotFound();
            }

            // Update the existing tracked entity instead of tracking a new one
            existingSession.Name = Session.Name;
            existingSession.Type = Session.Type;
            existingSession.Status = Session.Status;
            existingSession.Date = Session.Date;
            existingSession.Duration = Session.Duration;
            existingSession.Notes = Session.Notes;
            existingSession.StartedAt = Session.StartedAt;
            existingSession.PausedAt = Session.PausedAt;
            existingSession.CompletedAt = Session.CompletedAt;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Sessions.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSession(int id)
        {
            var userId = GetCurrentUserId();
            var Session = await _context.Sessions.FindAsync(id);

            if (Session == null || Session.UserId != userId)
            {
                return NotFound();
            }

            _context.Sessions.Remove(Session);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/Sessions/5/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateSessionStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            var userId = GetCurrentUserId();
            var session = await _context.Sessions.FindAsync(id);

            if (session == null || session.UserId != userId)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(request.Status))
            {
                return BadRequest(new { message = "Status cannot be empty." });
            }

            // Validate status
            var validStatuses = new[] { "draft", "in_progress", "completed" };
            if (!validStatuses.Contains(request.Status.ToLower()))
            {
                return BadRequest(new { message = "Invalid status. Must be: draft, in_progress, or completed" });
            }

            session.Status = request.Status.ToLower();

            // Update timestamps based on status
            if (request.Status.ToLower() == "in_progress" && session.StartedAt == null)
            {
                session.StartedAt = DateTime.UtcNow;
            }
            else if (request.Status.ToLower() == "completed" && session.CompletedAt == null)
            {
                session.CompletedAt = DateTime.UtcNow;

                // AUTO-UPDATE WORKOUT GOALS
                await UpdateWorkoutGoals(userId, session.CompletedAt.Value);
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/Sessions/5/pause
        [HttpPatch("{id}/pause")]
        public async Task<IActionResult> PauseSession(int id)
        {
            var userId = GetCurrentUserId();
            var session = await _context.Sessions.FindAsync(id);

            if (session == null || session.UserId != userId)
            {
                return NotFound();
            }

            if (session.Status != "in_progress")
            {
                return BadRequest(new { message = "Can only pause an in-progress session" });
            }

            session.PausedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/Sessions/5/resume
        [HttpPatch("{id}/resume")]
        public async Task<IActionResult> ResumeSession(int id)
        {
            var userId = GetCurrentUserId();
            var session = await _context.Sessions.FindAsync(id);

            if (session == null || session.UserId != userId)
            {
                return NotFound();
            }

            if (session.Status != "in_progress")
            {
                return BadRequest(new { message = "Can only resume an in-progress session" });
            }

            if (session.PausedAt == null)
            {
                return BadRequest(new { message = "Session is not paused" });
            }

            // Calculate how long it was paused
            var pausedDuration = DateTime.UtcNow - session.PausedAt.Value;

            // Adjust StartedAt to account for paused time
            if (session.StartedAt != null)
            {
                session.StartedAt = session.StartedAt.Value.Add(pausedDuration);
            }

            session.PausedAt = null;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Sessions/5/exercises
        [HttpPost("{id}/exercises")]
        public async Task<ActionResult<Exercise>> AddExerciseToSession(int id, [FromBody] AddExerciseRequest request)
        {
            var userId = GetCurrentUserId();
            var session = await _context.Sessions.FindAsync(id);

            if (session == null || session.UserId != userId)
            {
                return NotFound(new { message = "Session not found" });
            }

            var template = await _context.ExerciseTemplates.FindAsync(request.ExerciseTemplateId);
            if (template == null)
            {
                return NotFound(new { message = "Exercise template not found" });
            }

            // Create a new exercise instance from the template
            var exercise = new Exercise
            {
                SessionId = id,
                Name = template.Name,
                ExerciseTemplateId = request.ExerciseTemplateId
            };

            _context.Exercises.Add(exercise);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSession), new { id = session.Id }, exercise);
        }

        private async Task UpdateWorkoutGoals(int userId, DateTime workoutDate)
        {
            // Get active workout frequency goals
            var workoutGoals = await _context.Goals
                .Where(g => g.UserId == userId &&
                            g.IsActive &&
                            !g.IsCompleted &&
                            (g.GoalType.ToLower().Contains("workout") ||
                             g.GoalType.ToLower().Contains("frequency") ||
                             g.GoalType.ToLower().Contains("training")))
                .ToListAsync();

            foreach (var goal in workoutGoals)
            {
                // Determine if this workout counts toward the goal's time frame
                bool countsTowardGoal = ShouldCountWorkout(goal, workoutDate);

                if (countsTowardGoal)
                {
                    // Increment the goal's current value
                    goal.CurrentValue += 1;

                    // Add progress entry for tracking
                    var progress = new GoalProgress
                    {
                        GoalId = goal.Id,
                        RecordedAt = DateTime.UtcNow,
                        Value = goal.CurrentValue,
                        Notes = "Auto-tracked from workout completion"
                    };

                    _context.GoalProgressHistory.Add(progress);

                    // Check if goal is now complete
                    if (goal.CurrentValue >= goal.TargetValue)
                    {
                        goal.IsCompleted = true;
                        goal.CompletedAt = DateTime.UtcNow;
                        goal.IsActive = false;
                    }
                }
            }
        }

        private bool ShouldCountWorkout(Goal goal, DateTime workoutDate)
        {
            var now = DateTime.UtcNow;

            switch (goal.TimeFrame?.ToLower())
            {
                case "daily":
                    return workoutDate.Date == now.Date;

                case "weekly":
                    return GetWeekNumber(workoutDate) == GetWeekNumber(now) &&
                           workoutDate.Year == now.Year;

                case "monthly":
                    return workoutDate.Month == now.Month &&
                           workoutDate.Year == now.Year;

                case "yearly":
                    return workoutDate.Year == now.Year;

                case "total":
                case null:
                    return true;  // All-time goals count any workout

                default:
                    return false;
            }
        }

        private int GetWeekNumber(DateTime date)
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            return culture.Calendar.GetWeekOfYear(
                date,
                System.Globalization.CalendarWeekRule.FirstDay,
                DayOfWeek.Monday
            );
        }
    }
}
