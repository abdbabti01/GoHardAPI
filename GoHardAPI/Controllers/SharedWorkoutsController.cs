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
        /// Single source of truth for shared-workout visibility: a workout is visible to
        /// <paramref name="userId"/> if they created it (when <paramref name="includeOwn"/> is
        /// true) or if they have an accepted friendship with its creator. Filtering happens as a
        /// database-level WHERE/EXISTS clause so hidden rows are never materialized, and every
        /// read/mutation endpoint below composes off this one query instead of re-deriving the
        /// friendship condition.
        /// </summary>
        private IQueryable<SharedWorkout> SharedWorkoutsVisibleTo(int userId, bool includeOwn = true)
        {
            return _context.SharedWorkouts.Where(sw =>
                (includeOwn && sw.SharedByUserId == userId) ||
                _context.Friendships.Any(f =>
                    f.Status == "accepted" &&
                    ((f.RequesterId == userId && f.AddresseeId == sw.SharedByUserId) ||
                     (f.RequesterId == sw.SharedByUserId && f.AddresseeId == userId))));
        }

        /// <summary>
        /// Single source of truth for the SharedWorkoutDto shape used by every read query
        /// (GetSharedWorkouts, GetSharedWorkout, GetSharedWorkoutsByUser). Built as an
        /// Expression so EF Core translates it - including the correlated like/save EXISTS
        /// subqueries - into the SQL SELECT itself; only <see cref="SharedByUser"/>.Name is
        /// read off the navigation property, so PasswordHash/Email/etc. never leave the
        /// database. ShareWorkout can't use this (it isn't running a query against
        /// SharedWorkouts), so it goes through the sibling <see cref="ToDto"/> overload below -
        /// both funnel into the same SharedWorkoutDto type, so a field added to one call site
        /// without the other fails to compile instead of silently drifting.
        /// </summary>
        private Expression<Func<SharedWorkout, SharedWorkoutDto>> ProjectToDto(int currentUserId)
        {
            return sw => new SharedWorkoutDto
            {
                Id = sw.Id,
                OriginalId = sw.OriginalId,
                Type = sw.Type,
                SharedByUserId = sw.SharedByUserId,
                SharedByUserName = sw.SharedByUser != null ? sw.SharedByUser.Name : "Unknown",
                WorkoutName = sw.WorkoutName,
                Description = sw.Description,
                ExercisesJson = sw.ExercisesJson,
                Duration = sw.Duration,
                Category = sw.Category,
                Difficulty = sw.Difficulty,
                LikeCount = sw.LikeCount,
                SaveCount = sw.SaveCount,
                CommentCount = sw.CommentCount,
                SharedAt = sw.SharedAt,
                UpdatedAt = sw.UpdatedAt,
                IsLikedByCurrentUser = _context.SharedWorkoutLikes.Any(l => l.SharedWorkoutId == sw.Id && l.UserId == currentUserId),
                IsSavedByCurrentUser = _context.SharedWorkoutSaves.Any(s => s.SharedWorkoutId == sw.Id && s.UserId == currentUserId)
            };
        }

        /// <summary>
        /// Maps an already-materialized SharedWorkout (with SharedByUser loaded) to the same
        /// SharedWorkoutDto shape as <see cref="ProjectToDto"/>, for the one call site
        /// (ShareWorkout) that has an entity in hand rather than a query to project. EF Core
        /// cannot translate a call to this method into SQL, which is exactly why it can't be
        /// reused inside a LINQ Select - see ProjectToDto's remarks.
        /// </summary>
        private static SharedWorkoutDto ToDto(SharedWorkout sw, bool isLikedByCurrentUser, bool isSavedByCurrentUser)
        {
            return new SharedWorkoutDto
            {
                Id = sw.Id,
                OriginalId = sw.OriginalId,
                Type = sw.Type,
                SharedByUserId = sw.SharedByUserId,
                SharedByUserName = sw.SharedByUser != null ? sw.SharedByUser.Name : "Unknown",
                WorkoutName = sw.WorkoutName,
                Description = sw.Description,
                ExercisesJson = sw.ExercisesJson,
                Duration = sw.Duration,
                Category = sw.Category,
                Difficulty = sw.Difficulty,
                LikeCount = sw.LikeCount,
                SaveCount = sw.SaveCount,
                CommentCount = sw.CommentCount,
                SharedAt = sw.SharedAt,
                UpdatedAt = sw.UpdatedAt,
                IsLikedByCurrentUser = isLikedByCurrentUser,
                IsSavedByCurrentUser = isSavedByCurrentUser
            };
        }

        /// <summary>
        /// Get community shared workouts with optional filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SharedWorkoutDto>>> GetSharedWorkouts(
            [FromQuery] string? category = null,
            [FromQuery] string? difficulty = null,
            [FromQuery] bool friendsOnly = true,
            [FromQuery] int limit = 50)
        {
            var userId = GetCurrentUserId();

            // friendsOnly=true (default) preserves this endpoint's existing behavior exactly:
            // friends' shares only, not the caller's own. friendsOnly=false was previously an
            // unfiltered "everyone" query param with no consumer in GoHardAPP; it now goes through
            // the same centralized predicate with the caller's own shares included, so opting out
            // of the strict friends-only view can never expose a stranger's content - it only
            // toggles whether the caller's own shares appear alongside their friends'.
            // No .Include(SharedByUser) needed: ProjectToDto only ever reads sw.SharedByUser.Name
            // inside the Select, which EF Core translates into the projection's own JOIN.
            IQueryable<SharedWorkout> query = SharedWorkoutsVisibleTo(userId, includeOwn: !friendsOnly);

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
                .Select(ProjectToDto(userId))
                .ToListAsync();

            return Ok(workouts);
        }

        /// <summary>
        /// Get a specific shared workout by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<SharedWorkoutDto>> GetSharedWorkout(int id)
        {
            var userId = GetCurrentUserId();
            var workout = await SharedWorkoutsVisibleTo(userId)
                .Where(sw => sw.Id == id)
                .Select(ProjectToDto(userId))
                .FirstOrDefaultAsync();

            // A non-friend's workout and a nonexistent id both fall out of
            // SharedWorkoutsVisibleTo and land here identically, so this stays a plain 404 with
            // no distinguishing detail - avoids enumerating which ids exist.
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
        public async Task<ActionResult<IEnumerable<SharedWorkoutDto>>> GetSharedWorkoutsByUser(int userId)
        {
            var currentUserId = GetCurrentUserId();

            // If the requester isn't the target user and isn't their confirmed friend,
            // SharedWorkoutsVisibleTo yields no rows for that SharedByUserId, so this returns the
            // same empty list as a target user with zero shares - it never reveals whether hidden
            // shares exist.
            var workouts = await SharedWorkoutsVisibleTo(currentUserId)
                .Where(sw => sw.SharedByUserId == userId)
                .OrderByDescending(sw => sw.SharedAt)
                .Select(ProjectToDto(currentUserId))
                .ToListAsync();

            return Ok(workouts);
        }

        /// <summary>
        /// Get workouts saved by current user. Saving a workout does not grant permanent access:
        /// if the friendship that made it visible is later revoked (declined, left pending, or
        /// the row is removed), it drops out of this list - the SharedWorkoutSave row itself is
        /// left untouched, it just stops being joinable against a currently-visible workout. The
        /// join is against SharedWorkoutsVisibleTo (not a materialize-then-filter step), so this
        /// is a single database round trip with the same correlated-EXISTS friendship check as
        /// every other read endpoint, and no per-row friendship query.
        /// </summary>
        [HttpGet("saved")]
        public async Task<ActionResult<IEnumerable<SharedWorkoutDto>>> GetSavedWorkouts()
        {
            var userId = GetCurrentUserId();

            var workouts = await _context.SharedWorkoutSaves
                .Where(sws => sws.UserId == userId)
                .Join(
                    SharedWorkoutsVisibleTo(userId),
                    sws => sws.SharedWorkoutId,
                    sw => sw.Id,
                    (sws, sw) => new { sws.SavedAt, Workout = sw })
                .OrderByDescending(x => x.SavedAt)
                .Select(x => x.Workout)
                .Select(ProjectToDto(userId))
                .ToListAsync();

            return Ok(workouts);
        }

        /// <summary>
        /// Share a workout to the community
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SharedWorkoutDto>> ShareWorkout(SharedWorkout sharedWorkout)
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

            // A brand-new share can't have any likes/saves yet - no need to query for them.
            var dto = ToDto(sharedWorkout, isLikedByCurrentUser: false, isSavedByCurrentUser: false);

            return CreatedAtAction(nameof(GetSharedWorkout), new { id = sharedWorkout.Id }, dto);
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
            // Same visibility predicate as the read endpoints: a hidden (non-friend's) workout
            // must be rejected the same way a missing one is, not merely by existence.
            var sharedWorkout = await SharedWorkoutsVisibleTo(userId).FirstOrDefaultAsync(sw => sw.Id == id);

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
            // Same visibility predicate as the read endpoints: a hidden (non-friend's) workout
            // must be rejected the same way a missing one is, not merely by existence.
            var sharedWorkout = await SharedWorkoutsVisibleTo(userId).FirstOrDefaultAsync(sw => sw.Id == id);

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
