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
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WorkoutTemplatesController : ControllerBase
    {
        private readonly TrainingContext _context;

        public WorkoutTemplatesController(TrainingContext context)
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
        /// Get all templates for current user (personal templates)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WorkoutTemplate>>> GetTemplates(
            [FromQuery] bool? activeOnly = null)
        {
            var userId = GetCurrentUserId();
            var query = _context.WorkoutTemplates
                .Where(wt => wt.CreatedByUserId == userId);

            if (activeOnly == true)
            {
                query = query.Where(wt => wt.IsActive);
            }

            return await query
                .OrderByDescending(wt => wt.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Get community workout templates
        /// </summary>
        [HttpGet("community")]
        public async Task<ActionResult<IEnumerable<WorkoutTemplate>>> GetCommunityTemplates(
            [FromQuery] string? category = null,
            [FromQuery] int limit = 50)
        {
            var query = _context.WorkoutTemplates
                .Where(wt => !wt.IsCustom || wt.CreatedByUserId != null);

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(wt => wt.Category == category);
            }

            return await query
                .OrderByDescending(wt => wt.Rating ?? 0)
                .ThenByDescending(wt => wt.UsageCount)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Get a specific template by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<WorkoutTemplate>> GetTemplate(int id)
        {
            var template = await _context.WorkoutTemplates.FindAsync(id);

            if (template == null)
            {
                return NotFound();
            }

            return template;
        }

        /// <summary>
        /// Get templates scheduled for a specific date
        /// </summary>
        [HttpGet("scheduled")]
        public async Task<ActionResult<IEnumerable<WorkoutTemplate>>> GetTemplatesForDate(
            [FromQuery] DateTime date)
        {
            var userId = GetCurrentUserId();
            var dayOfWeek = ((int)date.DayOfWeek == 0 ? 7 : (int)date.DayOfWeek); // Convert Sunday from 0 to 7

            // Get active templates for the user
            var templates = await _context.WorkoutTemplates
                .Where(wt => wt.CreatedByUserId == userId && wt.IsActive)
                .ToListAsync();

            // Filter based on recurrence pattern
            var scheduledTemplates = templates.Where(wt =>
            {
                if (wt.RecurrencePattern == "daily")
                {
                    return true;
                }
                else if (wt.RecurrencePattern == "weekly" && !string.IsNullOrEmpty(wt.DaysOfWeek))
                {
                    var days = wt.DaysOfWeek.Split(',').Select(int.Parse).ToList();
                    return days.Contains(dayOfWeek);
                }
                else if (wt.RecurrencePattern == "custom" && wt.IntervalDays.HasValue && wt.LastUsedAt.HasValue)
                {
                    var daysSinceLastUse = (date - wt.LastUsedAt.Value.Date).Days;
                    return daysSinceLastUse >= wt.IntervalDays.Value;
                }
                return false;
            }).ToList();

            return Ok(scheduledTemplates);
        }

        /// <summary>
        /// Create a new workout template
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<WorkoutTemplate>> CreateTemplate(WorkoutTemplate template)
        {
            var userId = GetCurrentUserId();
            template.CreatedByUserId = userId;
            template.IsCustom = true;
            template.CreatedAt = DateTime.UtcNow;
            template.UsageCount = 0;
            template.RatingCount = 0;

            _context.WorkoutTemplates.Add(template);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
        }

        /// <summary>
        /// Update a workout template
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTemplate(int id, WorkoutTemplate template)
        {
            if (id != template.Id)
            {
                return BadRequest();
            }

            var userId = GetCurrentUserId();
            var existingTemplate = await _context.WorkoutTemplates.FindAsync(id);

            if (existingTemplate == null)
            {
                return NotFound();
            }

            // Only allow updating own custom templates
            if (existingTemplate.CreatedByUserId != userId)
            {
                return Forbid();
            }

            // Update fields
            existingTemplate.Name = template.Name;
            existingTemplate.Description = template.Description;
            existingTemplate.ExercisesJson = template.ExercisesJson;
            existingTemplate.RecurrencePattern = template.RecurrencePattern;
            existingTemplate.DaysOfWeek = template.DaysOfWeek;
            existingTemplate.IntervalDays = template.IntervalDays;
            existingTemplate.EstimatedDuration = template.EstimatedDuration;
            existingTemplate.Category = template.Category;
            existingTemplate.IsActive = template.IsActive;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Toggle active status of a template
        /// </summary>
        [HttpPatch("{id}/toggle-active")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var userId = GetCurrentUserId();
            var template = await _context.WorkoutTemplates.FindAsync(id);

            if (template == null)
            {
                return NotFound();
            }

            if (template.CreatedByUserId != userId)
            {
                return Forbid();
            }

            template.IsActive = !template.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { isActive = template.IsActive });
        }

        /// <summary>
        /// Delete a workout template
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            var userId = GetCurrentUserId();
            var template = await _context.WorkoutTemplates.FindAsync(id);

            if (template == null)
            {
                return NotFound();
            }

            // Only allow deleting own custom templates
            if (template.CreatedByUserId != userId)
            {
                return Forbid();
            }

            _context.WorkoutTemplates.Remove(template);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Increment usage count when template is used
        /// </summary>
        [HttpPost("{id}/increment-usage")]
        public async Task<IActionResult> IncrementUsageCount(int id)
        {
            var template = await _context.WorkoutTemplates.FindAsync(id);

            if (template == null)
            {
                return NotFound();
            }

            template.UsageCount++;
            template.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { usageCount = template.UsageCount });
        }

        /// <summary>
        /// Rate a workout template
        /// </summary>
        [HttpPost("{id}/rate")]
        public async Task<IActionResult> RateTemplate(int id, [FromBody] double rating)
        {
            if (rating < 1 || rating > 5)
            {
                return BadRequest("Rating must be between 1 and 5");
            }

            var userId = GetCurrentUserId();
            var template = await _context.WorkoutTemplates.FindAsync(id);

            if (template == null)
            {
                return NotFound();
            }

            // Check if user already rated
            var existingRating = await _context.WorkoutTemplateRatings
                .FirstOrDefaultAsync(r => r.WorkoutTemplateId == id && r.UserId == userId);

            if (existingRating != null)
            {
                // Update existing rating
                var oldRating = existingRating.Rating;
                existingRating.Rating = rating;
                existingRating.RatedAt = DateTime.UtcNow;

                // Recalculate average
                if (template.Rating.HasValue)
                {
                    var totalRating = (template.Rating.Value * template.RatingCount) - oldRating + rating;
                    template.Rating = totalRating / template.RatingCount;
                }
            }
            else
            {
                // Create new rating
                _context.WorkoutTemplateRatings.Add(new WorkoutTemplateRating
                {
                    WorkoutTemplateId = id,
                    UserId = userId,
                    Rating = rating,
                    RatedAt = DateTime.UtcNow
                });

                // Update average rating
                template.RatingCount++;
                if (template.Rating.HasValue)
                {
                    var totalRating = (template.Rating.Value * (template.RatingCount - 1)) + rating;
                    template.Rating = totalRating / template.RatingCount;
                }
                else
                {
                    template.Rating = rating;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { rating = template.Rating, ratingCount = template.RatingCount });
        }
    }
}
