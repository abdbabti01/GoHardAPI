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

            _context.Entry(Session).State = EntityState.Modified;

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
    }
}
