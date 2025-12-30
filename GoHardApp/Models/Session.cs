namespace GoHardApp.Models
{
    public class Session
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime Date { get; set; }
        public int? Duration { get; set; }
        public string? Notes { get; set; }
        public string? Type { get; set; }
        public string Status { get; set; } = "draft";
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<Exercise> Exercises { get; set; } = new List<Exercise>();
    }
}
