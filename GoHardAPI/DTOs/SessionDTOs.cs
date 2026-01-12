namespace GoHardAPI.DTOs
{
    public class CreateSessionFromProgramWorkoutDto
    {
        public int ProgramWorkoutId { get; set; }
    }

    public class StartPlannedWorkoutRequest
    {
        public DateTime? Date { get; set; }
        public DateTime? StartedAt { get; set; }
    }
}
