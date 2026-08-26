using System.Text.Json.Serialization;
using GoHardAPI.Converters;

namespace GoHardAPI.DTOs
{
    /// <summary>
    /// Daily nutrition progress (planned and consumed values) derived live from MealLog
    /// entries for a user/date, rather than read from the persisted NutritionProgress
    /// aggregate. Mirrors the NutritionProgress entity's JSON shape so GoHardAPP's
    /// DailyNutritionProgress model keeps parsing it unchanged.
    /// </summary>
    public class NutritionProgressDto
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime Date { get; set; }

        public int? NutritionGoalId { get; set; }

        // ============ Planned Values (sum of all meal entries, consumed or not) ============

        public decimal PlannedCalories { get; set; }
        public decimal PlannedProtein { get; set; }
        public decimal PlannedCarbohydrates { get; set; }
        public decimal PlannedFat { get; set; }
        public decimal PlannedFiber { get; set; }
        public decimal PlannedWater { get; set; }

        // ============ Consumed Values (sum of meal entries where IsConsumed == true) ============

        public decimal ConsumedCalories { get; set; }
        public decimal ConsumedProtein { get; set; }
        public decimal ConsumedCarbohydrates { get; set; }
        public decimal ConsumedFat { get; set; }
        public decimal ConsumedFiber { get; set; }
        public decimal ConsumedWater { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
