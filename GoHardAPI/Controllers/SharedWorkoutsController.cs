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
    public class SharedWorkoutsController : ControllerBase
    {
        private readonly TrainingContext _context;

        public SharedWorkoutsController(TrainingContext context)
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
        /// Get community shared workouts with optional filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetSharedWorkouts(
            [FromQuery] string? category = null,
            [FromQuery] string? difficulty = null,
            [FromQuery] int limit = 50)
        {
            var userId = GetCurrentUserId();
            var query = _context.SharedWorkouts
                .Include(sw => sw.SharedByUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(sw => sw.Category == category);
            }

            if (!string.IsNullOrEmpty(difficulty))
            {
                query = query.Where(sw => sw.Difficulty == difficulty);
            }

            var workouts = await query
                .OrderByDescending(sw => sw.SharedAt)
                .Take(limit)
                .Select(sw => new
                {
                    sw.Id,
                    sw.OriginalId,
                    sw.Type,
                    sw.SharedByUserId,
                    SharedByUserName = sw.SharedByUser != null ? sw.SharedByUser.Name : "Unknown",
                    sw.WorkoutName,
                    sw.Description,
                    sw.ExercisesJson,
                    sw.Duration,
                    sw.Category,
                    sw.Difficulty,
                    sw.LikeCount,
                    sw.SaveCount,
                    sw.CommentCount,
                    sw.SharedAt,
                    sw.UpdatedAt,
                    IsLikedByCurrentUser = _context.SharedWorkoutLikes.Any(l => l.SharedWorkoutId == sw.Id && l.UserId == userId),
                    IsSavedByCurrentUser = _context.SharedWorkoutSaves.Any(s => s.SharedWorkoutId == sw.Id && s.UserId == userId)
                })
                .ToListAsync();

            return Ok(workouts);
        }

        /// <summary>
        /// Get a specific shared workout by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetSharedWorkout(int id)
        {
            var userId = GetCurrentUserId();
            var workout = await _context.SharedWorkouts
                .Include(sw => sw.SharedByUser)
                .Where(sw => sw.Id == id)
                .Select(sw => new
                {
                    sw.Id,
                    sw.OriginalId,
                    sw.Type,
                    sw.SharedByUserId,
                    SharedByUserName = sw.SharedByUser != null ? sw.SharedByUser.Name : "Unknown",
                    sw.WorkoutName,
                    sw.Description,
                    sw.ExercisesJson,
                    sw.Duration,
                    sw.Category,
                    sw.Difficulty,
                    sw.LikeCount,
                    sw.SaveCount,
                    sw.CommentCount,
                    sw.SharedAt,
                    sw.UpdatedAt,
                    IsLikedByCurrentUser = _context.SharedWorkoutLikes.Any(l => l.SharedWorkoutId == sw.Id && l.UserId == userId),
                    IsSavedByCurrentUser = _context.SharedWorkoutSaves.Any(s => s.SharedWorkoutId == sw.Id && s.UserId == userId)
                })
                .FirstOrDefaultAsync();

            if (workout == null)
            {
                return NotFound();
            }

            return Ok(workout);
        }

        /// <summary>
        /// Get workouts shared by a specific user
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetSharedWorkoutsByUser(int userId)
        {
            var currentUserId = GetCurrentUserId();
            var workouts = await _context.SharedWorkouts
                .Include(sw => sw.SharedByUser)
                .Where(sw => sw.SharedByUserId == userId)
                .OrderByDescending(sw => sw.SharedAt)
                .Select(sw => new
                {
                    sw.Id,
                    sw.OriginalId,
                    sw.Type,
                    sw.SharedByUserId,
                    SharedByUserName = sw.SharedByUser != null ? sw.SharedByUser.Name : "Unknown",
                    sw.WorkoutName,
                    sw.Description,
                    sw.ExercisesJson,
                    sw.Duration,
                    sw.Category,
                    sw.Difficulty,
                    sw.LikeCount,
                    sw.SaveCount,
                    sw.CommentCount,
                    sw.SharedAt,
                    sw.UpdatedAt,
                    IsLikedByCurrentUser = _context.SharedWorkoutLikes.Any(l => l.SharedWorkoutId == sw.Id && l.UserId == currentUserId),
                    IsSavedByCurrentUser = _context.SharedWorkoutSaves.Any(s => s.SharedWorkoutId == sw.Id && s.UserId == currentUserId)
                })
                .ToListAsync();

            return Ok(workouts);
        }

        /// <summary>
        /// Get workouts saved by current user
        /// </summary>
        [HttpGet("saved")]
        public async Task<ActionResult<IEnumerable<object>>> GetSavedWorkouts()
        {
            var userId = GetCurrentUserId();
            var workouts = await _context.SharedWorkoutSaves
                .Where(sws => sws.UserId == userId)
                .Include(sws => sws.SharedWorkout)
                    .ThenInclude(sw => sw!.SharedByUser)
                .OrderByDescending(sws => sws.SavedAt)
                .Select(sws => new
                {
                    sws.SharedWorkout!.Id,
                    sws.SharedWorkout.OriginalId,
                    sws.SharedWorkout.Type,
                    sws.SharedWorkout.SharedByUserId,
                    SharedByUserName = sws.SharedWorkout.SharedByUser != null ? sws.SharedWorkout.SharedByUser.Name : "Unknown",
                    sws.SharedWorkout.WorkoutName,
                    sws.SharedWorkout.Description,
                    sws.SharedWorkout.ExercisesJson,
                    sws.SharedWorkout.Duration,
                    sws.SharedWorkout.Category,
                    sws.SharedWorkout.Difficulty,
                    sws.SharedWorkout.LikeCount,
                    sws.SharedWorkout.SaveCount,
                    sws.SharedWorkout.CommentCount,
                    sws.SharedWorkout.SharedAt,
                    sws.SharedWorkout.UpdatedAt,
                    IsLikedByCurrentUser = _context.SharedWorkoutLikes.Any(l => l.SharedWorkoutId == sws.SharedWorkout.Id && l.UserId == userId),
                    IsSavedByCurrentUser = true
                })
                .ToListAsync();

            return Ok(workouts);
        }

        /// <summary>
        /// Share a workout to the community
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SharedWorkout>> ShareWorkout(SharedWorkout sharedWorkout)
        {
            var userId = GetCurrentUserId();
            sharedWorkout.SharedByUserId = userId;
            sharedWorkout.SharedAt = DateTime.UtcNow;
            sharedWorkout.LikeCount = 0;
            sharedWorkout.SaveCount = 0;
            sharedWorkout.CommentCount = 0;

            _context.SharedWorkouts.Add(sharedWorkout);
            await _context.SaveChangesAsync();

            // Load the user information
            await _context.Entry(sharedWorkout).Reference(sw => sw.SharedByUser).LoadAsync();

            return CreatedAtAction(nameof(GetSharedWorkout), new { id = sharedWorkout.Id }, sharedWorkout);
        }

        /// <summary>
        /// Delete a shared workout (only if created by current user)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSharedWorkout(int id)
        {
            var userId = GetCurrentUserId();
            var sharedWorkout = await _context.SharedWorkouts.FindAsync(id);

            if (sharedWorkout == null)
            {
                return NotFound();
            }

            if (sharedWorkout.SharedByUserId != userId)
            {
                return Forbid();
            }

            _context.SharedWorkouts.Remove(sharedWorkout);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Toggle like on a shared workout
        /// </summary>
        [HttpPost("{id}/like")]
        public async Task<IActionResult> ToggleLike(int id)
        {
            var userId = GetCurrentUserId();
            var sharedWorkout = await _context.SharedWorkouts.FindAsync(id);

            if (sharedWorkout == null)
            {
                return NotFound();
            }

            var existingLike = await _context.SharedWorkoutLikes
                .FirstOrDefaultAsync(l => l.SharedWorkoutId == id && l.UserId == userId);

            if (existingLike != null)
            {
                // Unlike
                _context.SharedWorkoutLikes.Remove(existingLike);
                sharedWorkout.LikeCount = Math.Max(0, sharedWorkout.LikeCount - 1);
            }
            else
            {
                // Like
                _context.SharedWorkoutLikes.Add(new SharedWorkoutLike
                {
                    SharedWorkoutId = id,
                    UserId = userId,
                    LikedAt = DateTime.UtcNow
                });
                sharedWorkout.LikeCount++;
            }

            await _context.SaveChangesAsync();
            return Ok(new { liked = existingLike == null, likeCount = sharedWorkout.LikeCount });
        }

        /// <summary>
        /// Toggle save on a shared workout
        /// </summary>
        [HttpPost("{id}/save")]
        public async Task<IActionResult> ToggleSave(int id)
        {
            var userId = GetCurrentUserId();
            var sharedWorkout = await _context.SharedWorkouts.FindAsync(id);

            if (sharedWorkout == null)
            {
                return NotFound();
            }

            var existingSave = await _context.SharedWorkoutSaves
                .FirstOrDefaultAsync(s => s.SharedWorkoutId == id && s.UserId == userId);

            if (existingSave != null)
            {
                // Unsave
                _context.SharedWorkoutSaves.Remove(existingSave);
                sharedWorkout.SaveCount = Math.Max(0, sharedWorkout.SaveCount - 1);
            }
            else
            {
                // Save
                _context.SharedWorkoutSaves.Add(new SharedWorkoutSave
                {
                    SharedWorkoutId = id,
                    UserId = userId,
                    SavedAt = DateTime.UtcNow
                });
                sharedWorkout.SaveCount++;
            }

            await _context.SaveChangesAsync();
            return Ok(new { saved = existingSave == null, saveCount = sharedWorkout.SaveCount });
        }
    }
}
