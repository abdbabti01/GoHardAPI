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
    public class RunSessionsController : ControllerBase
    {
        private readonly TrainingContext _context;

        public RunSessionsController(TrainingContext context)
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

        /// <summary>
        /// Get all run sessions for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RunSession>>> GetRunSessions()
        {
            var userId = GetCurrentUserId();
            var sessions = await _context.RunSessions
                .Where(rs => rs.UserId == userId)
                .OrderByDescending(rs => rs.Date)
                .ToListAsync();

            return sessions;
        }

        /// <summary>
        /// Get recent run sessions (completed only)
        /// </summary>
        [HttpGet("recent")]
        public async Task<ActionResult<IEnumerable<RunSession>>> GetRecentRunSessions([FromQuery] int limit = 5)
        {
            var userId = GetCurrentUserId();
            var sessions = await _context.RunSessions
                .Where(rs => rs.UserId == userId && rs.Status == "completed")
                .OrderByDescending(rs => rs.Date)
                .Take(limit)
                .ToListAsync();

            return sessions;
        }

        /// <summary>
        /// Get run sessions for this week
        /// </summary>
        [HttpGet("weekly")]
        public async Task<ActionResult<object>> GetWeeklyStats()
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;
            var weekStart = now.Date.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday);
            if (now.DayOfWeek == DayOfWeek.Sunday)
                weekStart = weekStart.AddDays(-7);

            var sessions = await _context.RunSessions
                .Where(rs => rs.UserId == userId
                    && rs.Status == "completed"
                    && rs.Date >= weekStart)
                .ToListAsync();

            var totalDistance = sessions.Sum(s => s.Distance ?? 0);
            var totalDuration = sessions.Sum(s => s.Duration ?? 0);

            return new
            {
                runCount = sessions.Count,
                totalDistance = totalDistance,
                totalDuration = totalDuration
            };
        }

        /// <summary>
        /// Get a single run session
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<RunSession>> GetRunSession(int id)
        {
            var userId = GetCurrentUserId();
            var session = await _context.RunSessions
                .FirstOrDefaultAsync(rs => rs.Id == id && rs.UserId == userId);

            if (session == null)
            {
                return NotFound();
            }

            return session;
        }

        /// <summary>
        /// Create a new run session
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<RunSession>> CreateRunSession(RunSession runSession)
        {
            var userId = GetCurrentUserId();
            runSession.UserId = userId;

            _context.RunSessions.Add(runSession);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRunSession), new { id = runSession.Id }, runSession);
        }

        /// <summary>
        /// Update a run session
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRunSession(int id, RunSession runSession)
        {
            if (id != runSession.Id)
            {
                return BadRequest();
            }

            var userId = GetCurrentUserId();
            var existingSession = await _context.RunSessions.FindAsync(id);

            if (existingSession == null || existingSession.UserId != userId)
            {
                return NotFound();
            }

            // Update fields
            existingSession.Name = runSession.Name;
            existingSession.Date = runSession.Date;
            existingSession.Distance = runSession.Distance;
            existingSession.Duration = runSession.Duration;
            existingSession.AveragePace = runSession.AveragePace;
            existingSession.Calories = runSession.Calories;
            existingSession.Status = runSession.Status;
            existingSession.StartedAt = runSession.StartedAt;
            existingSession.CompletedAt = runSession.CompletedAt;
            existingSession.PausedAt = runSession.PausedAt;
            existingSession.RouteJson = runSession.RouteJson;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.RunSessions.Any(rs => rs.Id == id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        /// <summary>
        /// Update run session status
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateRunSessionStatus(int id, [FromBody] UpdateRunStatusRequest request)
        {
            var userId = GetCurrentUserId();
            var session = await _context.RunSessions.FindAsync(id);

            if (session == null || session.UserId != userId)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                session.Status = request.Status.ToLower();
            }

            if (request.StartedAt.HasValue)
            {
                session.StartedAt = request.StartedAt.Value;
            }

            if (request.CompletedAt.HasValue)
            {
                session.CompletedAt = request.CompletedAt.Value;
            }

            if (request.PausedAt.HasValue)
            {
                session.PausedAt = request.PausedAt.Value;
            }
            else if (request.ClearPausedAt)
            {
                session.PausedAt = null;
            }

            if (request.Duration.HasValue)
            {
                session.Duration = request.Duration.Value;
            }

            if (request.Distance.HasValue)
            {
                session.Distance = request.Distance.Value;
            }

            if (request.AveragePace.HasValue)
            {
                session.AveragePace = request.AveragePace.Value;
            }

            if (request.Calories.HasValue)
            {
                session.Calories = request.Calories.Value;
            }

            if (request.RouteJson != null)
            {
                session.RouteJson = request.RouteJson;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Delete a run session
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRunSession(int id)
        {
            var userId = GetCurrentUserId();
            var session = await _context.RunSessions.FindAsync(id);

            if (session == null || session.UserId != userId)
            {
                return NotFound();
            }

            _context.RunSessions.Remove(session);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    /// <summary>
    /// Request model for updating run session status
    /// </summary>
    public class UpdateRunStatusRequest
    {
        public string? Status { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? PausedAt { get; set; }
        public bool ClearPausedAt { get; set; }
        public int? Duration { get; set; }
        public double? Distance { get; set; }
        public double? AveragePace { get; set; }
        public int? Calories { get; set; }
        public string? RouteJson { get; set; }
    }
}
