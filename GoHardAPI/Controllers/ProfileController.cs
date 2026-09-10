using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GoHardAPI.Data;
using GoHardAPI.DTOs;
using GoHardAPI.Models;
using GoHardAPI.Repositories;
using GoHardAPI.Services;
using System.Security.Claims;

namespace GoHardAPI.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        /// <summary>
        /// The unique index on <c>Users.Username</c>
        /// (<c>TrainingContext.OnModelCreating</c>). Used to recognise the one
        /// concurrent-claim violation that maps to 409.
        /// </summary>
        private const string UsernameUniqueIndex = "IX_Users_Username";

        private readonly TrainingContext _context;
        private readonly FileUploadService _fileUploadService;
        private readonly IUserRepository _userRepository;
        private readonly CurrentMeasurementsService _currentMeasurements;
        private readonly IServiceScopeFactory _scopeFactory;

        /// <summary>
        /// Test seam: invoked once inside <see cref="UploadPhoto"/> immediately
        /// after the compare-and-set <c>ExecuteUpdateAsync</c> has returned
        /// successfully (the write is committed) but before the caller sees
        /// success. Throwing here reproduces a lost confirmation - a transient
        /// error or a client disconnect that lands after the row was already
        /// updated - so the post-exception reconciliation path can be exercised
        /// against a genuinely committed write. Always <c>null</c> in production.
        /// </summary>
        internal Action? AfterPhotoCompareAndSetForTests { get; set; }

        public ProfileController(
            TrainingContext context,
            FileUploadService fileUploadService,
            IUserRepository userRepository,
            CurrentMeasurementsService currentMeasurements,
            IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _userRepository = userRepository;
            _currentMeasurements = currentMeasurements;
            _scopeFactory = scopeFactory;
        }

        /// <summary>
        /// Get current user ID from JWT token
        /// </summary>
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim ?? "0");
        }

        /// <summary>
        /// Calculate age from date of birth
        /// </summary>
        private int? CalculateAge(DateTime? dateOfBirth)
        {
            if (dateOfBirth == null) return null;

            var today = DateTime.UtcNow;
            var age = today.Year - dateOfBirth.Value.Year;

            if (dateOfBirth.Value.Date > today.AddYears(-age))
                age--;

            return age;
        }

        /// <summary>
        /// Get workout statistics for user
        /// </summary>
        private async Task<ProfileStats> GetProfileStats(int userId)
        {
            var completedSessions = await _context.Sessions
                .Where(s => s.UserId == userId && s.Status == SessionStatus.Completed)
                .OrderBy(s => s.Date)
                .ToListAsync();

            var totalWorkouts = completedSessions.Count;

            // Calculate current streak
            var currentStreak = 0;
            if (completedSessions.Any())
            {
                var dates = completedSessions.Select(s => s.Date.Date).Distinct().OrderBy(d => d).ToList();
                var yesterday = DateTime.UtcNow.Date.AddDays(-1);
                var today = DateTime.UtcNow.Date;

                if (dates.Contains(today) || dates.Contains(yesterday))
                {
                    currentStreak = 1;
                    var checkDate = dates.Contains(today) ? today : yesterday;

                    for (int i = dates.Count - 2; i >= 0; i--)
                    {
                        var expectedDate = checkDate.AddDays(-1);
                        if (dates[i] == expectedDate)
                        {
                            currentStreak++;
                            checkDate = expectedDate;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            // Count personal records (distinct exercises with PRs)
            var prCount = await _context.Exercises
                .Where(e => e.Session.UserId == userId && e.Session.Status == SessionStatus.Completed)
                .Where(e => e.ExerciseTemplateId.HasValue)
                .Select(e => e.ExerciseTemplateId)
                .Distinct()
                .CountAsync();

            return new ProfileStats(totalWorkouts, currentStreak, prCount);
        }

        /// <summary>
        /// GET /api/profile - Get current user's full profile
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ProfileResponse>> GetProfile()
        {
            var userId = GetCurrentUserId();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found");

            // Calculate derived fields
            var age = CalculateAge(user.DateOfBirth);

            // Current body measurements are DERIVED from Body Metrics history on
            // every read (per field: newest RecordedAt, then newest Id, first
            // non-null; legacy User.X only when history has never covered X).
            // There is no persisted summary, so nothing here can be left stale by
            // a concurrent body-metric write.
            var measurements = await _currentMeasurements.GetForUserAsync(user);

            // Get stats
            var stats = await GetProfileStats(userId);

            var response = new ProfileResponse(
                user.Id,
                user.Name,
                user.Username,
                user.Email,
                user.ProfilePhotoUrl,
                user.Bio,
                user.DateOfBirth,
                age,
                user.Gender,
                measurements.HeightCm,
                measurements.WeightKg,
                user.TargetWeight,
                measurements.BodyFatPercentage,
                measurements.Bmi,
                user.ExperienceLevel,
                user.PrimaryGoal,
                user.Goals,
                user.UnitPreference,
                user.ThemePreference,
                user.FavoriteExercises,
                user.DateCreated,
                stats
            );

            return Ok(response);
        }

        /// <summary>
        /// PUT /api/profile - Update current user's profile
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<ProfileResponse>> UpdateProfile(UpdateProfileRequest request)
        {
            var userId = GetCurrentUserId();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found");

            // Username: optional. null -> unchanged. Re-submitting the caller's own
            // current value (ordinal) -> no-op, succeeds. A different value must be
            // free across every OTHER account (ownership is the JWT-derived userId,
            // never anything in the body). The pre-check is best-effort; the
            // IX_Users_Username unique index is the real guarantee against a
            // concurrent claim - handled on SaveChanges below. Case comparison is
            // delegated to UsernameExistsAsync, exactly as signup does, so the two
            // entry points stay identical on any given provider (see PR notes on
            // SQL Server CI vs PostgreSQL CS collation).
            if (request.Username != null
                && !string.Equals(request.Username, user.Username, StringComparison.Ordinal))
            {
                // excludeUserId: userId so a caller re-casing their OWN handle
                // ("alice" -> "Alice") is not blocked by their own row on a
                // case-insensitive collation, and "taken" strictly means "by
                // another account".
                if (await _userRepository.UsernameExistsAsync(request.Username, excludeUserId: userId))
                {
                    return Conflict(new { message = "Username already taken" });
                }

                user.Username = request.Username;
            }

            // Update fields (only update if provided)
            if (request.Name != null) user.Name = request.Name;
            if (request.Bio != null) user.Bio = request.Bio;
            if (request.DateOfBirth.HasValue) user.DateOfBirth = request.DateOfBirth;
            if (request.Gender != null) user.Gender = request.Gender;
            if (request.TargetWeight.HasValue) user.TargetWeight = request.TargetWeight;
            if (request.ExperienceLevel != null) user.ExperienceLevel = request.ExperienceLevel;
            if (request.PrimaryGoal != null) user.PrimaryGoal = request.PrimaryGoal;
            if (request.Goals != null) user.Goals = request.Goals;
            if (request.UnitPreference != null) user.UnitPreference = request.UnitPreference;
            if (request.ThemePreference != null) user.ThemePreference = request.ThemePreference;
            if (request.FavoriteExercises != null) user.FavoriteExercises = request.FavoriteExercises;

            // Height / Weight / BodyFatPercentage / BMI are intentionally NOT
            // written from a profile edit. Current measurements are owned by
            // /bodymetrics and derived on read by CurrentMeasurementsService;
            // this endpoint must neither set them directly nor insert a synthetic
            // "Updated from profile" measurement-history row (both used to happen
            // here). The fields stay on the DTO only so an older mobile build's
            // request body still binds without a 400.

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
                when (UniqueConstraintViolation.Matches(ex, UsernameUniqueIndex, "Users.Username"))
            {
                // Lost a concurrent race for the same username between the
                // pre-check and this write. ONLY the username index maps to 409;
                // every other DbUpdateException propagates unchanged.
                return Conflict(new { message = "Username already taken" });
            }

            // Return updated profile
            return await GetProfile();
        }

        /// <summary>
        /// POST /api/profile/photo - Upload (or replace) the caller's profile
        /// photo. The new image is validated and written to a fresh file BEFORE
        /// anything else changes; the previous photo is only removed after the
        /// new reference is committed. A failed upload or a lost concurrent race
        /// never destroys the previous photo.
        /// </summary>
        [HttpPost("photo")]
        [RequestSizeLimit(6 * 1024 * 1024)]                       // ~5 MB image + multipart overhead
        [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
        public async Task<ActionResult<PhotoUploadResponse>> UploadPhoto(IFormFile photo)
        {
            var userId = GetCurrentUserId();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found");

            var previousUrl = user.ProfilePhotoUrl;
            // The row is mutated below via ExecuteUpdateAsync (a compare-and-set,
            // bypassing the change tracker). Detach the tracked copy so a future
            // SaveChanges() added to this method can't write its stale
            // ProfilePhotoUrl back over the CAS result.
            _context.Entry(user).State = EntityState.Detached;

            // 1. Validate + write the NEW file. Nothing existing is touched.
            SavedProfilePhoto saved;
            try
            {
                saved = await _fileUploadService.SaveNewAsync(userId, photo, HttpContext.RequestAborted);
            }
            catch (PhotoValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            // 2. Publish the new reference with a database compare-and-set: swap
            //    only if the current value is still what this request started
            //    from. This serialises concurrent uploads/removes ACROSS
            //    instances (never an in-process lock) - exactly one write wins.
            int affected;
            try
            {
                affected = await _context.Users
                    .Where(u => u.Id == userId && u.ProfilePhotoUrl == previousUrl)
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(u => u.ProfilePhotoUrl, saved.RelativeUrl),
                        HttpContext.RequestAborted);

                // Test-only: simulate a lost confirmation on the now-committed
                // write (no-op in production).
                AfterPhotoCompareAndSetForTests?.Invoke();
            }
            catch (Exception)
            {
                // The CAS call threw or was cancelled. That tells us only that
                // THIS request stopped waiting for confirmation - it does not
                // establish what happened on the server. The UPDATE may have
                // committed, may have failed, or may still be running.
                //
                // The only outcome we can act on is a positive one: a fresh-
                // connection read that shows the row ALREADY holds the exact
                // unique URL this request wrote (a name only this request can
                // produce). That proves our CAS committed - treat it as success.
                //
                // Every other case - a different URL, a null, or a failed read -
                // is inconclusive. A client-side exception is not evidence that
                // the server operation finished, so a different observed value
                // must NOT be read as "our write rolled back": it may commit a
                // moment later. In all of these cases we delete NEITHER file and
                // propagate the failure. Preserving both is deliberate: an
                // orphaned new file is recoverable; deleting a photo the row
                // does (or is about to) reference is not.
                var (reconciled, committedUrl) = await TryReadCommittedPhotoUrlAsync(userId);

                if (!(reconciled
                      && string.Equals(committedUrl, saved.RelativeUrl, StringComparison.Ordinal)))
                {
                    // Outcome uncertain - keep BOTH files, surface the error.
                    throw;
                }

                // Positively confirmed: the row holds our new URL, so the CAS
                // committed. Continue exactly as for affected == 1.
                affected = 1;
            }

            if (affected == 0)
            {
                // Another upload/remove for this user committed first. Our new
                // file is unreferenced - drop it - and ask the client to retry.
                _fileUploadService.TryDeleteByRelativeUrl(saved.RelativeUrl);
                return Conflict(new
                {
                    message = "Your profile photo was changed by another request. Please try again.",
                });
            }

            // 3. New reference is committed. Only now remove the OLD file, best
            //    effort - a cleanup failure must not fail an already successful
            //    replacement (leaves a recoverable orphan, logged in the service).
            if (!string.IsNullOrEmpty(previousUrl)
                && !string.Equals(previousUrl, saved.RelativeUrl, StringComparison.Ordinal))
            {
                _fileUploadService.TryDeleteByRelativeUrl(previousUrl);
            }

            return Ok(new PhotoUploadResponse(saved.RelativeUrl));
        }

        /// <summary>
        /// Reads the committed <c>ProfilePhotoUrl</c> for <paramref name="userId"/>
        /// on a FRESH context (its own connection) with a token that is NOT
        /// <c>HttpContext.RequestAborted</c> - so a client disconnect that just
        /// aborted the compare-and-set cannot also abort this read.
        ///
        /// This is used for ONE decision only: has the row already committed the
        /// exact unique URL the current request wrote? A match is a positive
        /// confirmation that the CAS committed (only that request can produce
        /// that name). It is NOT used to conclude the opposite: a non-matching
        /// value, a null, or a failed read (<paramref name="reconciled"/> ==
        /// <c>false</c>) all mean "unknown" - the earlier exception is not
        /// evidence that the server write finished, so an in-flight commit is
        /// still possible. Callers must treat every non-positive result as
        /// uncertain and preserve files accordingly.
        /// </summary>
        private async Task<(bool reconciled, string? photoUrl)> TryReadCommittedPhotoUrlAsync(int userId)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<TrainingContext>();
                var url = await context.Users
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => u.ProfilePhotoUrl)
                    .FirstOrDefaultAsync(cts.Token);
                return (true, url);
            }
            catch
            {
                return (false, null);
            }
        }

        /// <summary>
        /// DELETE /api/profile/photo - Remove the caller's profile photo. The
        /// database reference is cleared first; the file is cleaned up
        /// afterwards, best effort.
        /// </summary>
        [HttpDelete("photo")]
        public async Task<IActionResult> DeletePhoto()
        {
            var userId = GetCurrentUserId();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found");

            var currentUrl = user.ProfilePhotoUrl;
            if (string.IsNullOrEmpty(currentUrl))
                return NotFound("No profile photo to delete");
            _context.Entry(user).State = EntityState.Detached; // see UploadPhoto

            // 1. Clear the reference FIRST, and only for the exact photo the
            //    caller saw - a concurrent upload that already replaced it wins
            //    (affected == 0) and owns its own file's lifecycle.
            var affected = await _context.Users
                .Where(u => u.Id == userId && u.ProfilePhotoUrl == currentUrl)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(u => u.ProfilePhotoUrl, (string?)null),
                    HttpContext.RequestAborted);

            // 2. Reference removed - now best-effort file cleanup, only for the
            //    photo we actually un-referenced.
            if (affected == 1)
            {
                _fileUploadService.TryDeleteByRelativeUrl(currentUrl);
            }

            return NoContent();
        }
    }
}
