using Asp.Versioning;
using GoHardAPI.Data;
using GoHardAPI.DTOs;
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
    public class ExerciseSetsController : ControllerBase
    {
        private readonly TrainingContext _context;

        public ExerciseSetsController(TrainingContext context)
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

        private async Task<bool> UserOwnsExerciseSet(int exerciseSetId)
        {
            var userId = GetCurrentUserId();
            var exerciseSet = await _context.ExerciseSets
                .Include(es => es.Exercise)
                    .ThenInclude(e => e.Session)
                .FirstOrDefaultAsync(es => es.Id == exerciseSetId);

            return exerciseSet?.Exercise?.Session?.UserId == userId;
        }

        // GET: api/ExerciseSets
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExerciseSet>>> GetExerciseSets()
        {
            var userId = GetCurrentUserId();
            return await _context.ExerciseSets
                .Include(es => es.Exercise)
                    .ThenInclude(e => e.Session)
                .Where(es => es.Exercise.Session.UserId == userId)
                .ToListAsync();
        }

        // GET: api/ExerciseSets/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ExerciseSet>> GetExerciseSet(int id)
        {
            if (!await UserOwnsExerciseSet(id))
            {
                return NotFound();
            }

            var exerciseSet = await _context.ExerciseSets
                .Include(es => es.Exercise)
                .FirstOrDefaultAsync(es => es.Id == id);

            if (exerciseSet == null)
            {
                return NotFound();
            }

            return exerciseSet;
        }

        // GET: api/ExerciseSets/exercise/5
        [HttpGet("exercise/{exerciseId}")]
        public async Task<ActionResult<IEnumerable<ExerciseSet>>> GetExerciseSetsByExercise(int exerciseId)
        {
            var userId = GetCurrentUserId();

            // Verify user owns the exercise
            var exercise = await _context.Exercises
                .Include(e => e.Session)
                .FirstOrDefaultAsync(e => e.Id == exerciseId);

            if (exercise == null || exercise.Session?.UserId != userId)
            {
                return NotFound();
            }

            var exerciseSets = await _context.ExerciseSets
                .Where(es => es.ExerciseId == exerciseId)
                .OrderBy(es => es.SetNumber)
                .ToListAsync();

            return exerciseSets;
        }

        // POST: api/ExerciseSets
        [HttpPost]
        public async Task<ActionResult<ExerciseSet>> CreateExerciseSet(ExerciseSet exerciseSet)
        {
            var userId = GetCurrentUserId();

            // Verify the exercise exists and user owns it
            var exercise = await _context.Exercises
                .Include(e => e.Session)
                .FirstOrDefaultAsync(e => e.Id == exerciseSet.ExerciseId);

            if (exercise == null)
            {
                return BadRequest(new { message = "Exercise not found" });
            }

            if (exercise.Session?.UserId != userId)
            {
                return Unauthorized(new { message = "You don't have access to this exercise" });
            }

            _context.ExerciseSets.Add(exerciseSet);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetExerciseSet), new { id = exerciseSet.Id }, exerciseSet);
        }

        // PUT: api/ExerciseSets/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExerciseSet(int id, [FromBody] ExerciseSetUpdateRequestDto request)
        {
            // The route segment is the identity of record. A body that disagrees
            // is malformed - fail deterministically before touching the database.
            if (id != request.Id)
            {
                return BadRequest(new { message = "Route id and body id must match." });
            }

            var userId = GetCurrentUserId();

            // Resolve the full parent chain (set -> exercise -> session) and confirm
            // the authenticated user owns the session. A foreign or missing set is a
            // non-disclosing 404, exactly like the other endpoints on this controller.
            var existing = await _context.ExerciseSets
                .Include(es => es.Exercise)
                    .ThenInclude(e => e!.Session)
                .FirstOrDefaultAsync(es => es.Id == id);

            if (existing == null || existing.Exercise?.Session?.UserId != userId)
            {
                return NotFound();
            }

            // The set stays attached to the exercise it already belongs to. A body
            // naming any other parent - in particular one owned by another user - is
            // rejected: this endpoint never reparents a set.
            if (request.ExerciseId != existing.ExerciseId)
            {
                return BadRequest(new { message = "An exercise set cannot be moved to a different exercise." });
            }

            // Update only the scalar fields the client may change. The parent FK
            // (ExerciseId) and the Version column are deliberately not assignable
            // through this contract.
            existing.SetNumber = request.SetNumber;
            existing.Reps = request.Reps;
            existing.Weight = request.Weight;
            existing.Duration = request.Duration;
            existing.IsCompleted = request.IsCompleted;
            existing.CompletedAt = request.CompletedAt;
            existing.Notes = request.Notes;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.ExerciseSets.AnyAsync(e => e.Id == id))
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

        // PATCH: api/ExerciseSets/5/complete
        [HttpPatch("{id}/complete")]
        public async Task<IActionResult> CompleteExerciseSet(int id)
        {
            if (!await UserOwnsExerciseSet(id))
            {
                return NotFound();
            }

            var exerciseSet = await _context.ExerciseSets.FindAsync(id);
            if (exerciseSet == null)
            {
                return NotFound();
            }

            exerciseSet.IsCompleted = true;
            exerciseSet.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/ExerciseSets/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExerciseSet(int id)
        {
            if (!await UserOwnsExerciseSet(id))
            {
                return NotFound();
            }

            var exerciseSet = await _context.ExerciseSets.FindAsync(id);
            if (exerciseSet == null)
            {
                return NotFound();
            }

            _context.ExerciseSets.Remove(exerciseSet);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
