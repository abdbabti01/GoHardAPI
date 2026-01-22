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
    [Route("api/[controller]")]
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

        // ============ Admin Endpoints for System Templates ============

        // POST: api/exercisetemplates/admin
        // Admin endpoint to create system exercise templates
        [HttpPost("admin")]
        [Authorize]
        public async Task<ActionResult<ExerciseTemplate>> CreateSystemTemplate([FromBody] CreateExerciseTemplateDto dto)
        {
            // TODO: Add proper admin role check when roles are implemented
            // For now, any authenticated user can add system templates

            if (await _templateRepository.NameExistsAsync(dto.Name))
            {
                return BadRequest(new { message = "An exercise template with this name already exists" });
            }

            var template = new ExerciseTemplate
            {
                Name = dto.Name,
                Description = dto.Description,
                Category = dto.Category,
                MuscleGroup = dto.MuscleGroup,
                Equipment = dto.Equipment,
                Difficulty = dto.Difficulty,
                VideoUrl = dto.VideoUrl,
                ImageUrl = dto.ImageUrl,
                Instructions = dto.Instructions,
                IsCustom = false,  // System template
                CreatedByUserId = null
            };

            await _templateRepository.AddAsync(template);
            await _templateRepository.SaveChangesAsync();

            return CreatedAtAction(nameof(GetExerciseTemplate), new { id = template.Id }, template);
        }

        // PUT: api/exercisetemplates/admin/5
        // Admin endpoint to update system exercise templates
        [HttpPut("admin/{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateSystemTemplate(int id, [FromBody] CreateExerciseTemplateDto dto)
        {
            // TODO: Add proper admin role check when roles are implemented

            var existing = await _templateRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            if (await _templateRepository.NameExistsAsync(dto.Name, id))
            {
                return BadRequest(new { message = "An exercise template with this name already exists" });
            }

            existing.Name = dto.Name;
            existing.Description = dto.Description;
            existing.Category = dto.Category;
            existing.MuscleGroup = dto.MuscleGroup;
            existing.Equipment = dto.Equipment;
            existing.Difficulty = dto.Difficulty;
            existing.VideoUrl = dto.VideoUrl;
            existing.ImageUrl = dto.ImageUrl;
            existing.Instructions = dto.Instructions;

            _templateRepository.Update(existing);
            await _templateRepository.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/exercisetemplates/admin/5
        // Admin endpoint to delete any exercise template
        [HttpDelete("admin/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteSystemTemplate(int id)
        {
            // TODO: Add proper admin role check when roles are implemented

            var template = await _templateRepository.GetByIdAsync(id);
            if (template == null)
            {
                return NotFound();
            }

            _templateRepository.Remove(template);
            await _templateRepository.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/exercisetemplates/admin/bulk
        // Admin endpoint to bulk create exercise templates
        [HttpPost("admin/bulk")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ExerciseTemplate>>> BulkCreateTemplates([FromBody] IEnumerable<CreateExerciseTemplateDto> dtos)
        {
            // TODO: Add proper admin role check when roles are implemented

            var templates = new List<ExerciseTemplate>();
            var errors = new List<string>();

            foreach (var dto in dtos)
            {
                if (await _templateRepository.NameExistsAsync(dto.Name))
                {
                    errors.Add($"Template '{dto.Name}' already exists");
                    continue;
                }

                templates.Add(new ExerciseTemplate
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Category = dto.Category,
                    MuscleGroup = dto.MuscleGroup,
                    Equipment = dto.Equipment,
                    Difficulty = dto.Difficulty,
                    VideoUrl = dto.VideoUrl,
                    ImageUrl = dto.ImageUrl,
                    Instructions = dto.Instructions,
                    IsCustom = false,
                    CreatedByUserId = null
                });
            }

            if (templates.Any())
            {
                await _templateRepository.AddRangeAsync(templates);
                await _templateRepository.SaveChangesAsync();
            }

            return Ok(new
            {
                created = templates.Count,
                errors = errors
            });
        }
    }

    // DTO for creating exercise templates (avoids exposing internal fields)
    public class CreateExerciseTemplateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? MuscleGroup { get; set; }
        public string? Equipment { get; set; }
        public string? Difficulty { get; set; }
        public string? VideoUrl { get; set; }
        public string? ImageUrl { get; set; }
        public string? Instructions { get; set; }
    }
}
