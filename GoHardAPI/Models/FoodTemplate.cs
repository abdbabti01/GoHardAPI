using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.Models
{
    /// <summary>
    /// Reusable food items with nutritional information
    /// </summary>
    public class FoodTemplate
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Brand { get; set; }

        [MaxLength(50)]
        public string? Category { get; set; } // Proteins, Carbohydrates, Vegetables, Fruits, Dairy, Fats & Oils, Snacks, Beverages

        [MaxLength(100)]
        public string? Barcode { get; set; }

        // Serving Information
        public decimal ServingSize { get; set; } = 100;

        [MaxLength(20)]
        public string ServingUnit { get; set; } = "g"; // g, ml, oz, cup, tbsp, tsp, piece

        // Macronutrients (per serving)
        public decimal Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbohydrates { get; set; }
        public decimal Fat { get; set; }
        public decimal? Fiber { get; set; }
        public decimal? Sugar { get; set; }
        public decimal? SaturatedFat { get; set; }
        public decimal? TransFat { get; set; }

        // Micronutrients (per serving) - all optional
        public decimal? Sodium { get; set; } // mg
        public decimal? Potassium { get; set; } // mg
        public decimal? Cholesterol { get; set; } // mg
        public decimal? VitaminA { get; set; } // mcg
        public decimal? VitaminC { get; set; } // mg
        public decimal? VitaminD { get; set; } // mcg
        public decimal? Calcium { get; set; } // mg
        public decimal? Iron { get; set; } // mg

        [MaxLength(2000)]
        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        // Track if this is a system food or user-created
        public bool IsCustom { get; set; } = false;
        public int? CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public User? CreatedByUser { get; set; }
    }
}
