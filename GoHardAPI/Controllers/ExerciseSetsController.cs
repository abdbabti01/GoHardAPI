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
        public async Task<IActionResult> UpdateExerciseSet(int id, ExerciseSet exerciseSet)
        {
            if (id != exerciseSet.Id)
            {
                return BadRequest();
            }

            if (!await UserOwnsExerciseSet(id))
            {
                return NotFound();
            }

            _context.Entry(exerciseSet).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.ExerciseSets.Any(e => e.Id == id))
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
