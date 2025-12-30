namespace GoHardApp.Models
{
    public class ExerciseSet
    {
        public int Id { get; set; }
        public int ExerciseId { get; set; }
        public int SetNumber { get; set; }
        public int? Reps { get; set; }
        public double? Weight { get; set; }
        public int? Duration { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Notes { get; set; }
    }
}
