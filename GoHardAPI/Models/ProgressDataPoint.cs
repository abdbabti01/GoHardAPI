namespace GoHardAPI.Models
{
    /// <summary>
    /// Time-series data point for progress charts
    /// </summary>
    public class ProgressDataPoint
    {
        /// <summary>
        /// Date of this data point
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Value (weight, volume, reps, etc.)
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Optional label for this point
        /// </summary>
        public string? Label { get; set; }
    }

    /// <summary>
    /// Volume data by muscle group
    /// </summary>
    public class MuscleGroupVolume
    {
        /// <summary>
        /// Muscle group name
        /// </summary>
        public string MuscleGroup { get; set; } = string.Empty;

        /// <summary>
        /// Total volume for this muscle group
        /// </summary>
        public double Volume { get; set; }

        /// <summary>
        /// Number of exercises for this muscle group
        /// </summary>
        public int ExerciseCount { get; set; }

        /// <summary>
        /// Percentage of total volume
        /// </summary>
        public double Percentage { get; set; }
    }

    /// <summary>
    /// Personal record entry
    /// </summary>
    public class PersonalRecord
    {
        /// <summary>
        /// Exercise name
        /// </summary>
        public string ExerciseName { get; set; } = string.Empty;

        /// <summary>
        /// Exercise template ID
        /// </summary>
        public int ExerciseTemplateId { get; set; }

        /// <summary>
        /// Record weight
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// Reps performed at this weight
        /// </summary>
        public int Reps { get; set; }

        /// <summary>
        /// Date achieved
        /// </summary>
        public DateTime DateAchieved { get; set; }

        /// <summary>
        /// Estimated 1 Rep Max
        /// </summary>
        public double EstimatedOneRepMax { get; set; }

        /// <summary>
        /// Days since last PR
        /// </summary>
        public int DaysSincePR { get; set; }
    }
}
