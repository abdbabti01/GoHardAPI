namespace GoHardAPI.Models
{
    /// <summary>
    /// Overall workout statistics for a user
    /// </summary>
    public class WorkoutStats
    {
        /// <summary>
        /// Total number of completed workouts
        /// </summary>
        public int TotalWorkouts { get; set; }

        /// <summary>
        /// Total workout duration in minutes (sum of all Session.Duration values)
        /// </summary>
        public int TotalDuration { get; set; }

        /// <summary>
        /// Average workout duration in minutes (TotalDuration / TotalWorkouts)
        /// </summary>
        public int AverageDuration { get; set; }

        /// <summary>
        /// Current workout streak (consecutive days)
        /// </summary>
        public int CurrentStreak { get; set; }

        /// <summary>
        /// Longest workout streak
        /// </summary>
        public int LongestStreak { get; set; }

        /// <summary>
        /// Workouts this week
        /// </summary>
        public int WorkoutsThisWeek { get; set; }

        /// <summary>
        /// Workouts this month
        /// </summary>
        public int WorkoutsThisMonth { get; set; }

        /// <summary>
        /// Total sets completed
        /// </summary>
        public int TotalSets { get; set; }

        /// <summary>
        /// Total reps completed
        /// </summary>
        public int TotalReps { get; set; }

        /// <summary>
        /// Total volume (sets × reps × weight) in kg
        /// </summary>
        public double TotalVolume { get; set; }

        /// <summary>
        /// Date of first workout
        /// </summary>
        public DateTime? FirstWorkoutDate { get; set; }

        /// <summary>
        /// Date of most recent workout
        /// </summary>
        public DateTime? LastWorkoutDate { get; set; }
    }
}
