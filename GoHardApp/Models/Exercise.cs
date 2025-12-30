namespace GoHardApp.Models
{
    public class Exercise
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? Duration { get; set; }
        public int? RestTime { get; set; }
        public string? Notes { get; set; }
        public int? ExerciseTemplateId { get; set; }
        public List<ExerciseSet> ExerciseSets { get; set; } = new List<ExerciseSet>();
    }
}
