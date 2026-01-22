using Asp.Versioning;
using GoHardAPI.Data;
using GoHardAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoHardAPI.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class ExerciseTemplatesController : ControllerBase
    {
        private readonly TrainingContext _context;

        public ExerciseTemplatesController(TrainingContext context)
        {
            _context = context;
        }

        // GET: api/exercisetemplates
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExerciseTemplate>>> GetExerciseTemplates(
            [FromQuery] string? category = null,
            [FromQuery] string? muscleGroup = null,
            [FromQuery] string? equipment = null,
            [FromQuery] bool? isCustom = null)
        {
            var query = _context.ExerciseTemplates.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(et => et.Category == category);
            }

            if (!string.IsNullOrEmpty(muscleGroup))
            {
                query = query.Where(et => et.MuscleGroup == muscleGroup);
            }

            if (!string.IsNullOrEmpty(equipment))
            {
                query = query.Where(et => et.Equipment == equipment);
            }

            if (isCustom.HasValue)
            {
                query = query.Where(et => et.IsCustom == isCustom.Value);
            }

            return await query.ToListAsync();
        }

        // GET: api/exercisetemplates/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ExerciseTemplate>> GetExerciseTemplate(int id)
        {
            var exerciseTemplate = await _context.ExerciseTemplates.FindAsync(id);

            if (exerciseTemplate == null)
            {
                return NotFound();
            }

            return exerciseTemplate;
        }

        // GET: api/exercisetemplates/categories
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            var categories = await _context.ExerciseTemplates
                .Where(et => et.Category != null)
                .Select(et => et.Category)
                .Distinct()
                .ToListAsync();

            return Ok(categories);
        }

        // GET: api/exercisetemplates/musclegroups
        [HttpGet("musclegroups")]
        public async Task<ActionResult<IEnumerable<string>>> GetMuscleGroups()
        {
            var muscleGroups = await _context.ExerciseTemplates
                .Where(et => et.MuscleGroup != null)
                .Select(et => et.MuscleGroup)
                .Distinct()
                .ToListAsync();

            return Ok(muscleGroups);
        }

        // POST: api/exercisetemplates
        [HttpPost]
        public async Task<ActionResult<ExerciseTemplate>> CreateExerciseTemplate(ExerciseTemplate exerciseTemplate)
        {
            // Ensure custom templates are marked as custom
            if (exerciseTemplate.CreatedByUserId.HasValue)
            {
                exerciseTemplate.IsCustom = true;
            }

            _context.ExerciseTemplates.Add(exerciseTemplate);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetExerciseTemplate), new { id = exerciseTemplate.Id }, exerciseTemplate);
        }

        // PUT: api/exercisetemplates/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExerciseTemplate(int id, ExerciseTemplate exerciseTemplate)
        {
            if (id != exerciseTemplate.Id)
            {
                return BadRequest();
            }

            _context.Entry(exerciseTemplate).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.ExerciseTemplates.Any(e => e.Id == id))
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

        // DELETE: api/exercisetemplates/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExerciseTemplate(int id)
        {
            var exerciseTemplate = await _context.ExerciseTemplates.FindAsync(id);
            if (exerciseTemplate == null)
            {
                return NotFound();
            }

            // Don't allow deletion of system templates
            if (!exerciseTemplate.IsCustom)
            {
                return BadRequest("Cannot delete system exercise templates");
            }

            _context.ExerciseTemplates.Remove(exerciseTemplate);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
