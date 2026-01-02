using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.DTOs
{
    /// <summary>
    /// Request to update user profile
    /// </summary>
    public record UpdateProfileRequest(
        [MaxLength(100)] string? Name,
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
    /// Profile response with calculated fields and stats
    /// </summary>
    public record ProfileResponse(
        int Id,
        string Name,
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
