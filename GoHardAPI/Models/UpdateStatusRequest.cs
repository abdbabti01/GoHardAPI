namespace GoHardAPI.Models
{
    public class UpdateStatusRequest
    {
        public string? Status { get; set; }

        // Optional: Client can send its own timestamps to preserve timer state
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? PausedAt { get; set; }
    }
}
