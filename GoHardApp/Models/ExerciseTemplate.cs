namespace GoHardApp.Models
{
    public class ExerciseTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? MuscleGroup { get; set; }
        public string? Equipment { get; set; }
        public string? Difficulty { get; set; }
        public string? VideoUrl { get; set; }
        public string? ImageUrl { get; set; }
        public string? Instructions { get; set; }
        public bool IsCustom { get; set; }
        public int? CreatedByUserId { get; set; }
    }
}
