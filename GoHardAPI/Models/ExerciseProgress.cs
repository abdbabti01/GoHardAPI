namespace GoHardAPI.Models
{
    /// <summary>
    /// Progress tracking for a specific exercise
    /// </summary>
    public class ExerciseProgress
    {
        /// <summary>
        /// Exercise template ID
        /// </summary>
        public int ExerciseTemplateId { get; set; }

        /// <summary>
        /// Exercise name
        /// </summary>
        public string ExerciseName { get; set; } = string.Empty;

        /// <summary>
        /// Total times performed
        /// </summary>
        public int TimesPerformed { get; set; }

        /// <summary>
        /// Total volume for this exercise (sets × reps × weight)
        /// </summary>
        public double TotalVolume { get; set; }

        /// <summary>
        /// Personal record (highest weight lifted)
        /// </summary>
        public double? PersonalRecord { get; set; }

        /// <summary>
        /// Date when PR was achieved
        /// </summary>
        public DateTime? PersonalRecordDate { get; set; }

        /// <summary>
        /// Most recent weight used
        /// </summary>
        public double? LastWeight { get; set; }

        /// <summary>
        /// Date of last performance
        /// </summary>
        public DateTime? LastPerformedDate { get; set; }

        /// <summary>
        /// Progress trend (percentage change from first to last)
        /// </summary>
        public double? ProgressPercentage { get; set; }
    }
}
