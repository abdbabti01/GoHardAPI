namespace GoHardAPI.DTOs
{
    public class CreateSessionFromProgramWorkoutDto
    {
        public int ProgramWorkoutId { get; set; }
        public int ProgramId { get; set; } // Pass programId to fix old data issue
        public DateTime? ScheduledDate { get; set; } // Optional: client-calculated date (in local timezone)
    }

    public class StartPlannedWorkoutRequest
    {
        public DateTime? Date { get; set; }
        public DateTime? StartedAt { get; set; }
    }

    /// <summary>
    /// Request to reorder exercises within a session
    /// </summary>
    public class ReorderExercisesRequest
    {
        /// <summary>
        /// List of exercise IDs in the desired order.
        /// The index in this list becomes the new SortOrder.
        /// </summary>
        public required List<int> ExerciseIds { get; set; }
    }
}
