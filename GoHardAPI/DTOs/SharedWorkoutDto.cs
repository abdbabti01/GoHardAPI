namespace GoHardAPI.DTOs
{
    /// <summary>
    /// Public shape of a shared workout returned by every SharedWorkouts read/create endpoint.
    /// Deliberately excludes the SharedByUser navigation property (and anything on User beyond
    /// its display name) so PasswordHash, Email, and other account fields can never be
    /// serialized through this controller.
    /// </summary>
    public class SharedWorkoutDto
    {
        public int Id { get; set; }
        public int OriginalId { get; set; }
        public string Type { get; set; } = string.Empty;
        public int SharedByUserId { get; set; }
        public string SharedByUserName { get; set; } = string.Empty;
        public string WorkoutName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ExercisesJson { get; set; } = string.Empty;
        public int Duration { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? Difficulty { get; set; }
        public int LikeCount { get; set; }
        public int SaveCount { get; set; }
        public int CommentCount { get; set; }
        public DateTime SharedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public bool IsSavedByCurrentUser { get; set; }
    }
}
