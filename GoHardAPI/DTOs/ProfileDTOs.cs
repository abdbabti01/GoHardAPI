using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.DTOs
{
    /// <summary>
    /// Request to update user profile.
    ///
    /// <para><b>Username</b> is optional: <c>null</c> leaves the current username
    /// untouched. When supplied it must satisfy the SAME rules as signup
    /// (<see cref="SignupRequest.Username"/>): 1-30 chars, letters/digits/underscore
    /// only. Uniqueness is enforced against every other account (see
    /// <c>ProfileController.UpdateProfile</c>); re-submitting the caller's own current
    /// username is a no-op and succeeds.</para>
    ///
    /// <para><b>Height / Weight / BodyFatPercentage</b> are accepted for backward
    /// compatibility with older mobile builds but are NO LONGER applied to the user
    /// record: current body measurements are owned by <c>/bodymetrics</c> and projected
    /// onto the profile from the latest measurement. A profile edit never writes these
    /// values and never inserts a measurement-history row.</para>
    /// </summary>
    public record UpdateProfileRequest(
        [MaxLength(100)] string? Name,
        // At least the same rules as SignupRequest.Username, minus [Required]
        // (null == "leave unchanged"). [MinLength(1)] is explicit here because
        // RegularExpressionAttribute and MaxLengthAttribute both treat "" as
        // valid and there is no [Required] to reject it - without MinLength a
        // body of {"username":""} would blank the handle, which signup forbids
        // (its [Required] + "+" quantifier already exclude "").
        [MinLength(1)]
        [MaxLength(30)]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscores")]
        string? Username,
        [MaxLength(500)] string? Bio,
        DateTime? DateOfBirth,
        string? Gender, // Male, Female, Other, PreferNotToSay
        double? Height,
        double? Weight,
        double? TargetWeight,
        double? BodyFatPercentage,
        string? ExperienceLevel, // Beginner, Intermediate, Advanced, Expert
        string? PrimaryGoal, // WeightLoss, MuscleGain, Strength, Endurance, GeneralFitness
        [MaxLength(500)] string? Goals,
        string? UnitPreference, // Metric, Imperial
        string? ThemePreference, // Light, Dark, System
        string? FavoriteExercises
    );

    /// <summary>
    /// Profile response with calculated fields and stats.
    ///
    /// <para><c>Height</c>/<c>Weight</c>/<c>BodyFatPercentage</c>/<c>BMI</c> are the
    /// authoritative current values projected from the user's Body Metrics history
    /// (latest non-null value per field); see <c>UserMeasurementSummaryService</c>.</para>
    /// </summary>
    public record ProfileResponse(
        int Id,
        string Name,
        string Username,
        string Email,
        string? ProfilePhotoUrl,
        string? Bio,
        DateTime? DateOfBirth,
        int? Age,
        string? Gender,
        double? Height,
        double? Weight,
        double? TargetWeight,
        double? BodyFatPercentage,
        double? BMI,
        string? ExperienceLevel,
        string? PrimaryGoal,
        string? Goals,
        string UnitPreference,
        string? ThemePreference,
        string? FavoriteExercises,
        DateTime DateCreated,
        ProfileStats? Stats
    );

    /// <summary>
    /// Profile statistics from analytics
    /// </summary>
    public record ProfileStats(
        int TotalWorkouts,
        int CurrentStreak,
        int PersonalRecords
    );

    /// <summary>
    /// Response after uploading profile photo
    /// </summary>
    public record PhotoUploadResponse(
        string PhotoUrl
    );
}
