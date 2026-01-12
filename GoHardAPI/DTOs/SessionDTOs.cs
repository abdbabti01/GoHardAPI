namespace GoHardAPI.DTOs
{
    public class CreateSessionFromProgramWorkoutDto
    {
        public int ProgramWorkoutId { get; set; }
        public int ProgramId { get; set; } // Pass programId to fix old data issue
    }

    public class StartPlannedWorkoutRequest
    {
        public DateTime? Date { get; set; }
        public DateTime? StartedAt { get; set; }
    }
}
