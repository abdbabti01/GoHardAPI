namespace GoHardAPI.Models
{
    public class UpdateStatusRequest
    {
        public string? Status { get; set; }

        // Optional: Client can send its own timestamps to preserve timer state
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? PausedAt { get; set; }

        // Set to true to clear PausedAt (for resume operation)
        // Needed because PausedAt=null in JSON doesn't trigger HasValue
        public bool ClearPausedAt { get; set; } = false;

        // Optional: Duration in minutes (calculated from timer)
        public int? Duration { get; set; }
    }
}
