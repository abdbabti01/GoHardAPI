using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.Models
{
    /// <summary>
    /// Represents an individual food item within a meal entry
    /// </summary>
    public class FoodItem
    {
        public int Id { get; set; }

        [Required]
        public int MealEntryId { get; set; }

        /// <summary>
        /// Optional reference to a food template (can be null for custom foods)
        /// </summary>
        public int? FoodTemplateId { get; set; }

        /// <summary>
        /// Name of the food (copied from template or custom entered)
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Brand { get; set; }

        /// <summary>
        /// Number of servings consumed
        /// </summary>
        public decimal Quantity { get; set; } = 1;

        /// <summary>
        /// Size of one serving
        /// </summary>
        public decimal ServingSize { get; set; } = 100;

        /// <summary>
        /// Unit for serving size (g, ml, oz, cup, etc.)
        /// </summary>
        [MaxLength(20)]
        public string ServingUnit { get; set; } = "g";

        // Calculated nutritional values (based on quantity)
        public decimal Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbohydrates { get; set; }
        public decimal Fat { get; set; }
        public decimal? Fiber { get; set; }
        public decimal? Sugar { get; set; }
        public decimal? Sodium { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public MealEntry? MealEntry { get; set; }
        public FoodTemplate? FoodTemplate { get; set; }

        /// <summary>
        /// Calculate nutritional values from a food template and quantity
        /// </summary>
        public void CalculateFromTemplate(FoodTemplate template, decimal quantity)
        {
            FoodTemplateId = template.Id;
            Name = template.Name;
            Brand = template.Brand;
            Quantity = quantity;
            ServingSize = template.ServingSize;
            ServingUnit = template.ServingUnit;

            // Calculate based on quantity
            Calories = template.Calories * quantity;
            Protein = template.Protein * quantity;
            Carbohydrates = template.Carbohydrates * quantity;
            Fat = template.Fat * quantity;
            Fiber = template.Fiber.HasValue ? template.Fiber.Value * quantity : null;
            Sugar = template.Sugar.HasValue ? template.Sugar.Value * quantity : null;
            Sodium = template.Sodium.HasValue ? template.Sodium.Value * quantity : null;
        }
    }
}
