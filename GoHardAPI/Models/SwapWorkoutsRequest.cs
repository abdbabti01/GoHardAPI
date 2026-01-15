using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.Models
{
    /// <summary>
    /// Request model for swapping two program workouts
    /// </summary>
    public class SwapWorkoutsRequest
    {
        /// <summary>
        /// ID of the first workout to swap
        /// </summary>
        [Required]
        public int Workout1Id { get; set; }

        /// <summary>
        /// ID of the second workout to swap
        /// </summary>
        [Required]
        public int Workout2Id { get; set; }
    }
}
