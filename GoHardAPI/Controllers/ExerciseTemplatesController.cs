using Asp.Versioning;
using GoHardAPI.Models;
using GoHardAPI.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GoHardAPI.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class ExerciseTemplatesController : ControllerBase
    {
        private readonly IExerciseTemplateRepository _templateRepository;

        public ExerciseTemplatesController(IExerciseTemplateRepository templateRepository)
        {
            _templateRepository = templateRepository;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return null;
            }
            return userId;
        }

        // GET: api/exercisetemplates
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExerciseTemplate>>> GetExerciseTemplates(
            [FromQuery] string? category = null,
            [FromQuery] string? muscleGroup = null,
            [FromQuery] string? equipment = null,
            [FromQuery] bool? isCustom = null)
        {
            var templates = await _templateRepository.SearchAsync(category, muscleGroup, equipment);

            if (isCustom.HasValue)
            {
                templates = templates.Where(t => t.IsCustom == isCustom.Value);
            }

            return Ok(templates);
        }

        // GET: api/exercisetemplates/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ExerciseTemplate>> GetExerciseTemplate(int id)
        {
            var exerciseTemplate = await _templateRepository.GetByIdAsync(id);

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
            var categories = await _templateRepository.GetDistinctCategoriesAsync();
            return Ok(categories);
        }

        // GET: api/exercisetemplates/musclegroups
        [HttpGet("musclegroups")]
        public async Task<ActionResult<IEnumerable<string>>> GetMuscleGroups()
        {
            var muscleGroups = await _templateRepository.GetDistinctMuscleGroupsAsync();
            return Ok(muscleGroups);
        }

        // POST: api/exercisetemplates
        // Users can create custom templates for themselves
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ExerciseTemplate>> CreateExerciseTemplate(ExerciseTemplate exerciseTemplate)
        {
            var userId = GetCurrentUserId();

            // Validate name uniqueness
            if (await _templateRepository.NameExistsAsync(exerciseTemplate.Name))
            {
                return BadRequest(new { message = "An exercise template with this name already exists" });
            }

            // User-created templates are always marked as custom
            exerciseTemplate.IsCustom = true;
            exerciseTemplate.CreatedByUserId = userId;

            await _templateRepository.AddAsync(exerciseTemplate);
            await _templateRepository.SaveChangesAsync();

            return CreatedAtAction(nameof(GetExerciseTemplate), new { id = exerciseTemplate.Id }, exerciseTemplate);
        }

        // PUT: api/exercisetemplates/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateExerciseTemplate(int id, ExerciseTemplate exerciseTemplate)
        {
            if (id != exerciseTemplate.Id)
            {
                return BadRequest();
            }

            var existing = await _templateRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();

            // Only allow updating custom templates by their creator
            if (existing.IsCustom && existing.CreatedByUserId != userId)
            {
                return Forbid();
            }

            // Don't allow updating system templates unless admin
            if (!existing.IsCustom)
            {
                return BadRequest(new { message = "Cannot modify system exercise templates" });
            }

            // Check name uniqueness (excluding current template)
            if (await _templateRepository.NameExistsAsync(exerciseTemplate.Name, id))
            {
                return BadRequest(new { message = "An exercise template with this name already exists" });
            }

            // Update fields
            existing.Name = exerciseTemplate.Name;
            existing.Description = exerciseTemplate.Description;
            existing.Category = exerciseTemplate.Category;
            existing.MuscleGroup = exerciseTemplate.MuscleGroup;
            existing.Equipment = exerciseTemplate.Equipment;
            existing.Difficulty = exerciseTemplate.Difficulty;
            existing.VideoUrl = exerciseTemplate.VideoUrl;
            existing.ImageUrl = exerciseTemplate.ImageUrl;
            existing.Instructions = exerciseTemplate.Instructions;

            _templateRepository.Update(existing);
            await _templateRepository.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/exercisetemplates/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteExerciseTemplate(int id)
        {
            var exerciseTemplate = await _templateRepository.GetByIdAsync(id);
            if (exerciseTemplate == null)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();

            // Don't allow deletion of system templates
            if (!exerciseTemplate.IsCustom)
            {
                return BadRequest(new { message = "Cannot delete system exercise templates" });
            }

            // Only allow deleting own custom templates
            if (exerciseTemplate.CreatedByUserId != userId)
            {
                return Forbid();
            }

            _templateRepository.Remove(exerciseTemplate);
            await _templateRepository.SaveChangesAsync();

            return NoContent();
        }
    }
}
