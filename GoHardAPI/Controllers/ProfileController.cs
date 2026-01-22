using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GoHardAPI.Data;
using GoHardAPI.DTOs;
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
        private readonly TrainingContext _context;
        private readonly FileUploadService _fileUploadService;

        public ProfileController(TrainingContext context, FileUploadService fileUploadService)
        {
            _context = context;
            _fileUploadService = fileUploadService;
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
        /// Calculate BMI from height and weight
        /// </summary>
        private double? CalculateBMI(double? height, double? weight)
        {
            if (height == null || weight == null || height == 0) return null;

            var heightInMeters = height.Value / 100; // convert cm to meters
            return weight.Value / (heightInMeters * heightInMeters);
        }

        /// <summary>
        /// Get workout statistics for user
        /// </summary>
        private async Task<ProfileStats> GetProfileStats(int userId)
        {
            var completedSessions = await _context.Sessions
                .Where(s => s.UserId == userId && s.Status == "completed")
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
                .Where(e => e.Session.UserId == userId && e.Session.Status == "completed")
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
            var bmi = CalculateBMI(user.Height, user.Weight);

            // Update BMI in database if calculated
            if (bmi.HasValue && user.BMI != bmi.Value)
            {
                user.BMI = bmi.Value;
                await _context.SaveChangesAsync();
            }

            // Get stats
            var stats = await GetProfileStats(userId);

            var response = new ProfileResponse(
                user.Id,
                user.Name,
                user.Email,
                user.ProfilePhotoUrl,
                user.Bio,
                user.DateOfBirth,
                age,
                user.Gender,
                user.Height,
                user.Weight,
                user.TargetWeight,
                user.BodyFatPercentage,
                user.BMI,
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

            // Update fields (only update if provided)
            if (request.Name != null) user.Name = request.Name;
            if (request.Bio != null) user.Bio = request.Bio;
            if (request.DateOfBirth.HasValue) user.DateOfBirth = request.DateOfBirth;
            if (request.Gender != null) user.Gender = request.Gender;
            if (request.Height.HasValue) user.Height = request.Height;
            if (request.Weight.HasValue) user.Weight = request.Weight;
            if (request.TargetWeight.HasValue) user.TargetWeight = request.TargetWeight;
            if (request.BodyFatPercentage.HasValue) user.BodyFatPercentage = request.BodyFatPercentage;
            if (request.ExperienceLevel != null) user.ExperienceLevel = request.ExperienceLevel;
            if (request.PrimaryGoal != null) user.PrimaryGoal = request.PrimaryGoal;
            if (request.Goals != null) user.Goals = request.Goals;
            if (request.UnitPreference != null) user.UnitPreference = request.UnitPreference;
            if (request.ThemePreference != null) user.ThemePreference = request.ThemePreference;
            if (request.FavoriteExercises != null) user.FavoriteExercises = request.FavoriteExercises;

            // Recalculate BMI
            user.BMI = CalculateBMI(user.Height, user.Weight);

            await _context.SaveChangesAsync();

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
