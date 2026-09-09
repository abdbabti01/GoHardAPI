using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public ProfileController(
            TrainingContext context,
            FileUploadService fileUploadService,
            IUserRepository userRepository,
            CurrentMeasurementsService currentMeasurements)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _userRepository = userRepository;
            _currentMeasurements = currentMeasurements;
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
        /// POST /api/profile/photo - Upload profile photo
        /// </summary>
        [HttpPost("photo")]
        public async Task<ActionResult<PhotoUploadResponse>> UploadPhoto(IFormFile photo)
        {
            var userId = GetCurrentUserId();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found");

            try
            {
                // Delete old photo if exists
                if (!string.IsNullOrEmpty(user.ProfilePhotoUrl))
                {
                    _fileUploadService.DeleteProfilePhoto(user.ProfilePhotoUrl);
                }

                // Upload new photo
                var photoUrl = await _fileUploadService.UploadProfilePhotoAsync(userId, photo);

                // Update user record
                user.ProfilePhotoUrl = photoUrl;
                await _context.SaveChangesAsync();

                return Ok(new PhotoUploadResponse(photoUrl));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// DELETE /api/profile/photo - Delete profile photo
        /// </summary>
        [HttpDelete("photo")]
        public async Task<IActionResult> DeletePhoto()
        {
            var userId = GetCurrentUserId();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found");

            if (string.IsNullOrEmpty(user.ProfilePhotoUrl))
                return NotFound("No profile photo to delete");

            try
            {
                // Delete file
                _fileUploadService.DeleteProfilePhoto(user.ProfilePhotoUrl);

                // Update user record
                user.ProfilePhotoUrl = null;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
