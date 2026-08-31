using Asp.Versioning;
using GoHardAPI.Data;
using GoHardAPI.DTOs;
using GoHardAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Claims;

namespace GoHardAPI.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
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
        /// Single source of truth for workout-template read visibility: a template is visible to
        /// <paramref name="userId"/> if it is a system template (<c>CreatedByUserId == null</c>,
        /// globally readable), if they created it, or if it is an explicitly published community
        /// template (<c>IsPublic</c>). Community visibility is never inferred from
        /// <c>CreatedByUserId != null</c>. Filtering is a database-level WHERE clause so hidden
        /// rows are never materialized, and every read/rate endpoint composes off this query.
        /// </summary>
        private IQueryable<WorkoutTemplate> TemplatesVisibleTo(int userId)
        {
            return _context.WorkoutTemplates.Where(wt =>
                wt.CreatedByUserId == null ||
                wt.CreatedByUserId == userId ||
                wt.IsPublic);
        }

        /// <summary>
        /// Owner check shared by every mutating endpoint (update / toggle / delete / increment).
        /// Returns null when the caller owns <paramref name="template"/>, otherwise the result to
        /// return: a template the caller cannot even see (another user's private one) is reported
        /// as 404 — identical to a missing id — so its existence is never disclosed; a visible
        /// template the caller does not own (a published custom or a system template) is 403.
        /// </summary>
        private IActionResult? OwnerGuardFailure(WorkoutTemplate template, int userId)
        {
            if (template.CreatedByUserId == userId)
            {
                return null;
            }

            var visibleButNotOwned = template.CreatedByUserId == null || template.IsPublic;
            return visibleButNotOwned ? Forbid() : NotFound();
        }

        /// <summary>
        /// Single source of truth for the <see cref="WorkoutTemplateDto"/> shape used by every
        /// read query (list, community, by-id, scheduled). Built as an Expression so EF Core
        /// translates it into the SQL SELECT itself; only <c>CreatedByUser.Name</c> is read off
        /// the navigation property, so PasswordHash/Email/FcmToken never leave the database.
        /// <see cref="ToDto"/> is the sibling for call sites that already hold a materialized
        /// entity (create); both funnel into the same DTO type so a field added to one without
        /// the other fails to compile instead of silently drifting.
        /// </summary>
        private static Expression<Func<WorkoutTemplate, WorkoutTemplateDto>> ProjectToDto()
        {
            return wt => new WorkoutTemplateDto
            {
                Id = wt.Id,
                Name = wt.Name,
                Description = wt.Description,
                ExercisesJson = wt.ExercisesJson,
                RecurrencePattern = wt.RecurrencePattern,
                DaysOfWeek = wt.DaysOfWeek,
                IntervalDays = wt.IntervalDays,
                EstimatedDuration = wt.EstimatedDuration,
                Category = wt.Category,
                IsActive = wt.IsActive,
                IsCustom = wt.IsCustom,
                IsPublic = wt.IsPublic,
                CreatedByUserId = wt.CreatedByUserId,
                CreatedByUserName = wt.CreatedByUser != null ? wt.CreatedByUser.Name : null,
                UsageCount = wt.UsageCount,
                Rating = wt.Rating,
                RatingCount = wt.RatingCount,
                CreatedAt = wt.CreatedAt,
                LastUsedAt = wt.LastUsedAt
            };
        }

        /// <summary>
        /// Maps an already-materialized <see cref="WorkoutTemplate"/> (with CreatedByUser loaded
        /// where applicable) to the same <see cref="WorkoutTemplateDto"/> shape as
        /// <see cref="ProjectToDto"/>. EF Core cannot translate a call to this method into SQL,
        /// which is exactly why it can't be reused inside a LINQ Select.
        /// </summary>
        private static WorkoutTemplateDto ToDto(WorkoutTemplate wt)
        {
            return new WorkoutTemplateDto
            {
                Id = wt.Id,
                Name = wt.Name,
                Description = wt.Description,
                ExercisesJson = wt.ExercisesJson,
                RecurrencePattern = wt.RecurrencePattern,
                DaysOfWeek = wt.DaysOfWeek,
                IntervalDays = wt.IntervalDays,
                EstimatedDuration = wt.EstimatedDuration,
                Category = wt.Category,
                IsActive = wt.IsActive,
                IsCustom = wt.IsCustom,
                IsPublic = wt.IsPublic,
                CreatedByUserId = wt.CreatedByUserId,
                CreatedByUserName = wt.CreatedByUser != null ? wt.CreatedByUser.Name : null,
                UsageCount = wt.UsageCount,
                Rating = wt.Rating,
                RatingCount = wt.RatingCount,
                CreatedAt = wt.CreatedAt,
                LastUsedAt = wt.LastUsedAt
            };
        }

        /// <summary>
        /// Validates recurrence coherence beyond the per-field data annotations: pattern must be
        /// one of the established values, weekly requires day numbers 1-7, custom requires a
        /// positive interval. Returns null when valid, otherwise a client-facing message.
        /// </summary>
        private static string? ValidateRecurrence(string pattern, string? daysOfWeek, int? intervalDays)
        {
            if (!RecurrencePatterns.IsValid(pattern))
            {
                return $"RecurrencePattern must be one of: {string.Join(", ", RecurrencePatterns.All)}.";
            }

            if (pattern == RecurrencePatterns.Weekly)
            {
                if (string.IsNullOrWhiteSpace(daysOfWeek))
                {
                    return "DaysOfWeek is required when RecurrencePattern is 'weekly'.";
                }

                foreach (var part in daysOfWeek.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!int.TryParse(part, out var day) || day < 1 || day > 7)
                    {
                        return "DaysOfWeek must be comma-separated day numbers from 1 (Monday) to 7 (Sunday).";
                    }
                }
            }

            if (pattern == RecurrencePatterns.Custom && (intervalDays == null || intervalDays < 1))
            {
                return "IntervalDays (>= 1) is required when RecurrencePattern is 'custom'.";
            }

            return null;
        }

        /// <summary>
        /// Whether an active template recurs on <paramref name="date"/>. Preserves the original
        /// scheduling semantics: daily always; weekly by ISO day number (Sunday = 7); custom by
        /// whole days elapsed since <see cref="WorkoutTemplateDto.LastUsedAt"/>. Operates on the
        /// DTO so the query can project directly (no tracked entity, no User navigation loaded).
        /// </summary>
        private static bool IsScheduledForDate(WorkoutTemplateDto wt, DateTime date)
        {
            var dayOfWeek = (int)date.DayOfWeek == 0 ? 7 : (int)date.DayOfWeek;

            if (wt.RecurrencePattern == RecurrencePatterns.Daily)
            {
                return true;
            }

            if (wt.RecurrencePattern == RecurrencePatterns.Weekly && !string.IsNullOrEmpty(wt.DaysOfWeek))
            {
                foreach (var part in wt.DaysOfWeek.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (int.TryParse(part, out var day) && day == dayOfWeek)
                    {
                        return true;
                    }
                }
                return false;
            }

            if (wt.RecurrencePattern == RecurrencePatterns.Custom && wt.IntervalDays.HasValue && wt.LastUsedAt.HasValue)
            {
                var daysSinceLastUse = (date - wt.LastUsedAt.Value.Date).Days;
                return daysSinceLastUse >= wt.IntervalDays.Value;
            }

            return false;
        }

        /// <summary>
        /// Get the current user's own workout templates (system and other users' templates are
        /// never returned here — see <see cref="GetCommunityTemplates"/> for shared ones).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WorkoutTemplateDto>>> GetTemplates(
            [FromQuery] bool? activeOnly = null)
        {
            var userId = GetCurrentUserId();
            var query = _context.WorkoutTemplates
                .Where(wt => wt.CreatedByUserId == userId);

            if (activeOnly == true)
            {
                query = query.Where(wt => wt.IsActive);
            }

            var templates = await query
                .OrderByDescending(wt => wt.CreatedAt)
                .Select(ProjectToDto())
                .ToListAsync();

            return Ok(templates);
        }

        /// <summary>
        /// Get community workout templates: system templates plus custom templates their owners
        /// have explicitly published. Private custom templates are never included.
        /// </summary>
        [HttpGet("community")]
        public async Task<ActionResult<IEnumerable<WorkoutTemplateDto>>> GetCommunityTemplates(
            [FromQuery] string? category = null,
            [FromQuery] int limit = 50)
        {
            // Clamp so a caller cannot request an unbounded (or, via a negative value, an
            // always-empty) community feed.
            limit = Math.Clamp(limit, 1, 200);

            var query = _context.WorkoutTemplates
                .Where(wt => wt.CreatedByUserId == null || wt.IsPublic);

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(wt => wt.Category == category);
            }

            var templates = await query
                .OrderByDescending(wt => wt.Rating ?? 0)
                .ThenByDescending(wt => wt.UsageCount)
                .Take(limit)
                .Select(ProjectToDto())
                .ToListAsync();

            return Ok(templates);
        }

        /// <summary>
        /// Get a specific template by ID. A template hidden from the caller (another user's
        /// private template) and a nonexistent id both return an identical 404, so this endpoint
        /// never reveals which template ids exist.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<WorkoutTemplateDto>> GetTemplate(int id)
        {
            var userId = GetCurrentUserId();

            var template = await TemplatesVisibleTo(userId)
                .Where(wt => wt.Id == id)
                .Select(ProjectToDto())
                .FirstOrDefaultAsync();

            if (template == null)
            {
                return NotFound();
            }

            return Ok(template);
        }

        /// <summary>
        /// Get the caller's own active templates scheduled for a specific date. Only the
        /// requester's templates are considered, so another user's or a shared template's usage
        /// history can never influence the result.
        /// </summary>
        [HttpGet("scheduled")]
        public async Task<ActionResult<IEnumerable<WorkoutTemplateDto>>> GetTemplatesForDate(
            [FromQuery] DateTime date)
        {
            var userId = GetCurrentUserId();

            // Project to the DTO in the query (single LEFT JOIN for the owner name, no tracked
            // entities, no full User row loaded), then apply the recurrence filter in memory.
            var candidates = await _context.WorkoutTemplates
                .AsNoTracking()
                .Where(wt => wt.CreatedByUserId == userId && wt.IsActive)
                .Select(ProjectToDto())
                .ToListAsync();

            var scheduled = candidates
                .Where(dto => IsScheduledForDate(dto, date))
                .ToList();

            return Ok(scheduled);
        }

        /// <summary>
        /// Create a new workout template. It is always owned by the authenticated user, always a
        /// custom template, and private unless the request explicitly sets <c>isPublic</c>.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<WorkoutTemplateDto>> CreateTemplate(CreateWorkoutTemplateRequest request)
        {
            var userId = GetCurrentUserId();

            var recurrenceError = ValidateRecurrence(request.RecurrencePattern, request.DaysOfWeek, request.IntervalDays);
            if (recurrenceError != null)
            {
                return BadRequest(recurrenceError);
            }

            var template = new WorkoutTemplate
            {
                Name = request.Name,
                Description = request.Description,
                ExercisesJson = request.ExercisesJson,
                RecurrencePattern = request.RecurrencePattern,
                DaysOfWeek = request.DaysOfWeek,
                IntervalDays = request.IntervalDays,
                EstimatedDuration = request.EstimatedDuration,
                Category = request.Category,
                IsActive = request.IsActive ?? true,
                IsPublic = request.IsPublic ?? false,
                // Server-owned: never taken from the request.
                CreatedByUserId = userId,
                IsCustom = true,
                CreatedAt = DateTime.UtcNow,
                UsageCount = 0,
                Rating = null,
                RatingCount = 0,
                LastUsedAt = null
            };

            _context.WorkoutTemplates.Add(template);
            await _context.SaveChangesAsync();

            await _context.Entry(template).Reference(t => t.CreatedByUser).LoadAsync();

            return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, ToDto(template));
        }

        /// <summary>
        /// Update a workout template. Only the owner of a custom template may update it; system
        /// templates and other users' templates are rejected. Ownership and all server-computed
        /// fields (usage, rating, timestamps) cannot be changed here.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTemplate(int id, UpdateWorkoutTemplateRequest request)
        {
            var userId = GetCurrentUserId();
            var existing = await _context.WorkoutTemplates.FindAsync(id);

            if (existing == null)
            {
                return NotFound();
            }

            var guard = OwnerGuardFailure(existing, userId);
            if (guard != null)
            {
                return guard;
            }

            var recurrenceError = ValidateRecurrence(request.RecurrencePattern, request.DaysOfWeek, request.IntervalDays);
            if (recurrenceError != null)
            {
                return BadRequest(recurrenceError);
            }

            existing.Name = request.Name;
            existing.Description = request.Description;
            existing.ExercisesJson = request.ExercisesJson;
            existing.RecurrencePattern = request.RecurrencePattern;
            existing.DaysOfWeek = request.DaysOfWeek;
            existing.IntervalDays = request.IntervalDays;
            existing.EstimatedDuration = request.EstimatedDuration;
            existing.Category = request.Category;
            existing.IsActive = request.IsActive ?? true;
            existing.IsPublic = request.IsPublic ?? false;
            // Deliberately not assigned: CreatedByUserId, IsCustom, UsageCount, Rating,
            // RatingCount, CreatedAt, LastUsedAt.

            await _context.SaveChangesAsync();

            // Preserved 204 No Content contract: the mobile client re-fetches on demand and does
            // not read a body from this call.
            return NoContent();
        }

        /// <summary>
        /// Toggle active status of a template. Owner-only.
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

            var guard = OwnerGuardFailure(template, userId);
            if (guard != null)
            {
                return guard;
            }

            template.IsActive = !template.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { isActive = template.IsActive });
        }

        /// <summary>
        /// Delete a workout template. Owner-only; system templates cannot be deleted.
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

            var guard = OwnerGuardFailure(template, userId);
            if (guard != null)
            {
                return guard;
            }

            _context.WorkoutTemplates.Remove(template);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Record that the caller used one of their own templates. Owner-only: a user can only
        /// increment usage on a template they created. Mutates only UsageCount and LastUsedAt.
        /// </summary>
        [HttpPost("{id}/increment-usage")]
        public async Task<IActionResult> IncrementUsageCount(int id)
        {
            var userId = GetCurrentUserId();
            var template = await _context.WorkoutTemplates.FindAsync(id);

            if (template == null)
            {
                return NotFound();
            }

            var guard = OwnerGuardFailure(template, userId);
            if (guard != null)
            {
                return guard;
            }

            template.UsageCount++;
            template.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { usageCount = template.UsageCount });
        }

        /// <summary>
        /// Rate a workout template the caller can see (a system template or a published community
        /// template). A template hidden from the caller returns 404 like a missing one; a caller
        /// cannot rate their own custom template.
        /// </summary>
        [HttpPost("{id}/rate")]
        public async Task<IActionResult> RateTemplate(int id, [FromBody] RateWorkoutTemplateRequest request)
        {
            var userId = GetCurrentUserId();

            if (request.Rating < 1 || request.Rating > 5)
            {
                return BadRequest("Rating must be between 1 and 5");
            }

            var template = await TemplatesVisibleTo(userId)
                .FirstOrDefaultAsync(wt => wt.Id == id);

            if (template == null)
            {
                return NotFound();
            }

            if (template.CreatedByUserId == userId)
            {
                return BadRequest("You cannot rate your own template");
            }

            var existingRating = await _context.WorkoutTemplateRatings
                .FirstOrDefaultAsync(r => r.WorkoutTemplateId == id && r.UserId == userId);

            if (existingRating != null)
            {
                var oldRating = existingRating.Rating;
                existingRating.Rating = request.Rating;
                existingRating.RatedAt = DateTime.UtcNow;

                if (template.Rating.HasValue && template.RatingCount > 0)
                {
                    var totalRating = (template.Rating.Value * template.RatingCount) - oldRating + request.Rating;
                    template.Rating = totalRating / template.RatingCount;
                }
                else
                {
                    template.Rating = request.Rating;
                }
            }
            else
            {
                _context.WorkoutTemplateRatings.Add(new WorkoutTemplateRating
                {
                    WorkoutTemplateId = id,
                    UserId = userId,
                    Rating = request.Rating,
                    RatedAt = DateTime.UtcNow
                });

                template.RatingCount++;
                if (template.Rating.HasValue && template.RatingCount > 1)
                {
                    var totalRating = (template.Rating.Value * (template.RatingCount - 1)) + request.Rating;
                    template.Rating = totalRating / template.RatingCount;
                }
                else
                {
                    template.Rating = request.Rating;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { rating = template.Rating, ratingCount = template.RatingCount });
        }
    }
}
