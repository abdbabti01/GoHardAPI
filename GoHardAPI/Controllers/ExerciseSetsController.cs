using GoHardAPI.Data;
using GoHardAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoHardAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExerciseSetsController : ControllerBase
    {
        private readonly TrainingContext _context;

        public ExerciseSetsController(TrainingContext context)
        {
            _context = context;
        }

        // GET: api/ExerciseSets
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExerciseSet>>> GetExerciseSets()
        {
            return await _context.ExerciseSets.Include(es => es.Exercise).ToListAsync();
        }

        // GET: api/ExerciseSets/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ExerciseSet>> GetExerciseSet(int id)
        {
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
            // Verify the exercise exists
            var exercise = await _context.Exercises.FindAsync(exerciseSet.ExerciseId);
            if (exercise == null)
            {
                return BadRequest(new { message = "Exercise not found" });
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
