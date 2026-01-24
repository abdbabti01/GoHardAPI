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
    public class FoodTemplatesController : ControllerBase
    {
        private readonly TrainingContext _context;

        public FoodTemplatesController(TrainingContext context)
        {
            _context = context;
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

        /// <summary>
        /// Get all food templates with optional filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FoodTemplate>>> GetFoodTemplates(
            [FromQuery] string? category = null,
            [FromQuery] bool? isCustom = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = _context.FoodTemplates.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(ft => ft.Category == category);
            }

            if (isCustom.HasValue)
            {
                query = query.Where(ft => ft.IsCustom == isCustom.Value);
            }

            var templates = await query
                .OrderBy(ft => ft.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(templates);
        }

        /// <summary>
        /// Get a specific food template by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<FoodTemplate>> GetFoodTemplate(int id)
        {
            var template = await _context.FoodTemplates.FindAsync(id);

            if (template == null)
            {
                return NotFound();
            }

            return Ok(template);
        }

        /// <summary>
        /// Get distinct food categories
        /// </summary>
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            var categories = await _context.FoodTemplates
                .Where(ft => ft.Category != null)
                .Select(ft => ft.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Ok(categories);
        }

        /// <summary>
        /// Search food templates by name
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<FoodTemplate>>> SearchFoodTemplates(
            [FromQuery] string query,
            [FromQuery] string? category = null,
            [FromQuery] int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { message = "Search query is required" });
            }

            var searchQuery = _context.FoodTemplates
                .Where(ft => ft.Name.ToLower().Contains(query.ToLower()) ||
                            (ft.Brand != null && ft.Brand.ToLower().Contains(query.ToLower())));

            if (!string.IsNullOrEmpty(category))
            {
                searchQuery = searchQuery.Where(ft => ft.Category == category);
            }

            var templates = await searchQuery
                .OrderBy(ft => ft.Name)
                .Take(limit)
                .ToListAsync();

            return Ok(templates);
        }

        /// <summary>
        /// Get a food template by barcode
        /// </summary>
        [HttpGet("barcode/{barcode}")]
        public async Task<ActionResult<FoodTemplate>> GetByBarcode(string barcode)
        {
            var template = await _context.FoodTemplates
                .FirstOrDefaultAsync(ft => ft.Barcode == barcode);

            if (template == null)
            {
                return NotFound(new { message = "No food found with this barcode" });
            }

            return Ok(template);
        }

        /// <summary>
        /// Create a custom food template (auth required)
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<FoodTemplate>> CreateFoodTemplate([FromBody] FoodTemplate template)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            template.IsCustom = true;
            template.CreatedByUserId = userId;
            template.CreatedAt = DateTime.UtcNow;

            _context.FoodTemplates.Add(template);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFoodTemplate), new { id = template.Id }, template);
        }

        /// <summary>
        /// Update a custom food template
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateFoodTemplate(int id, [FromBody] FoodTemplate template)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var existing = await _context.FoodTemplates.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            // Only allow updating custom templates by their creator
            if (!existing.IsCustom)
            {
                return BadRequest(new { message = "Cannot modify system food templates" });
            }

            if (existing.CreatedByUserId != userId)
            {
                return Forbid();
            }

            // Update fields
            existing.Name = template.Name;
            existing.Brand = template.Brand;
            existing.Category = template.Category;
            existing.Barcode = template.Barcode;
            existing.ServingSize = template.ServingSize;
            existing.ServingUnit = template.ServingUnit;
            existing.Calories = template.Calories;
            existing.Protein = template.Protein;
            existing.Carbohydrates = template.Carbohydrates;
            existing.Fat = template.Fat;
            existing.Fiber = template.Fiber;
            existing.Sugar = template.Sugar;
            existing.SaturatedFat = template.SaturatedFat;
            existing.TransFat = template.TransFat;
            existing.Sodium = template.Sodium;
            existing.Potassium = template.Potassium;
            existing.Cholesterol = template.Cholesterol;
            existing.VitaminA = template.VitaminA;
            existing.VitaminC = template.VitaminC;
            existing.VitaminD = template.VitaminD;
            existing.Calcium = template.Calcium;
            existing.Iron = template.Iron;
            existing.Description = template.Description;
            existing.ImageUrl = template.ImageUrl;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Delete a custom food template
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteFoodTemplate(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var template = await _context.FoodTemplates.FindAsync(id);
            if (template == null)
            {
                return NotFound();
            }

            if (!template.IsCustom)
            {
                return BadRequest(new { message = "Cannot delete system food templates" });
            }

            if (template.CreatedByUserId != userId)
            {
                return Forbid();
            }

            _context.FoodTemplates.Remove(template);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
