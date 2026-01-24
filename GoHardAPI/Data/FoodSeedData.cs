using GoHardAPI.Models;

namespace GoHardAPI.Data
{
    public static class FoodSeedData
    {
        public static void Initialize(TrainingContext context)
        {
            var templates = GetAllFoodTemplates();

            // Check if we already have food templates
            bool hasExistingTemplates = context.FoodTemplates.Any();

            if (hasExistingTemplates)
            {
                // Add any missing templates
                AddMissingTemplates(context, templates);
                return;
            }

            // First run - add all templates
            context.FoodTemplates.AddRange(templates);
            context.SaveChanges();
            Console.WriteLine($"Added {templates.Count} food templates");
        }

        private static void AddMissingTemplates(TrainingContext context, List<FoodTemplate> templates)
        {
            var existingNames = context.FoodTemplates
                .Select(t => t.Name.ToLower())
                .ToHashSet();

            var missingTemplates = templates
                .Where(t => !existingNames.Contains(t.Name.ToLower()))
                .ToList();

            if (missingTemplates.Any())
            {
                context.FoodTemplates.AddRange(missingTemplates);
                context.SaveChanges();
                Console.WriteLine($"Added {missingTemplates.Count} missing food templates");
            }
        }

        private static List<FoodTemplate> GetAllFoodTemplates()
        {
            return new List<FoodTemplate>
            {
                // ============ PROTEINS (15 items) ============
                new FoodTemplate
                {
                    Name = "Chicken Breast",
                    Category = "Proteins",
                    ServingSize = 100,
                    ServingUnit = "g",
                    Calories = 165,
                    Protein = 31,
                    Carbohydrates = 0,
                    Fat = 3.6m,
                    Fiber = 0,
                    Sugar = 0,
                    Sodium = 74,
                    Cholesterol = 85,
                    Description = "Boneless, skinless chicken breast, cooked"
                },
                new FoodTemplate
                {
                    Name = "Eggs",
                    Category = "Proteins",
                    ServingSize = 50,
                    ServingUnit = "g",
                    Calories = 78,
                    Protein = 6,
                    Carbohydrates = 0.6m,
                    Fat = 5,
                    Fiber = 0,
                    Sugar = 0.6m,
                    Sodium = 62,
                    Cholesterol = 186,
                    Description = "Large egg, whole"
                },
                new FoodTemplate
                {
                    Name = "Salmon",
                    Category = "Proteins",
                    ServingSize = 100,
                    ServingUnit = "g",
                    Calories = 208,
                    Protein = 20,
                    Carbohydrates = 0,
                    Fat = 13,
                    SaturatedFat = 3.1m,
                    Fiber = 0,
                    Sodium = 59,
                    Cholesterol = 55,
                    Description = "Atlantic salmon, cooked"
                },
                new FoodTemplate
                {
                    Name = "Ground Beef (90% lean)",
                    Category = "Proteins",
                    ServingSize = 100,
                    ServingUnit = "g",
                    Calories = 176,
                    Protein = 26,
                    Carbohydrates = 0,
                    Fat = 8,
                    SaturatedFat = 3.1m,
                    Fiber = 0,
                    Sodium = 66,
                    Cholesterol = 78,
                    Description = "90% lean ground beef, cooked"
                },
                new FoodTemplate
                {
                    Name = "Greek Yogurt",
                    Category = "Proteins",
                    ServingSize = 170,
                    ServingUnit = "g",
                    Calories = 100,
                    Protein = 17,
                    Carbohydrates = 6,
                    Fat = 0.7m,
                    Sugar = 4,
                    Calcium = 187,
                    Description = "Plain non-fat Greek yogurt"
                },
                new FoodTemplate
                {
                    Name = "Tofu",
                    Category = "Proteins",
                    ServingSize = 100,
                    ServingUnit = "g",
                    Calories = 76,
                    Protein = 8,
                    Carbohydrates = 1.9m,
                    Fat = 4.8m,
                    Fiber = 0.3m,
                    Calcium = 350,
                    Iron = 5.4m,
                    Description = "Firm tofu"
                },
                new FoodTemplate
                {
                    Name = "Tuna (canned in water)",
                    Category = "Proteins",
                    ServingSize = 85,
                    ServingUnit = "g",
                    Calories = 73,
                    Protein = 17,
                    Carbohydrates = 0,
                    Fat = 0.8m,
                    Sodium = 230,
                    Description = "Chunk light tuna, drained"
                },
                new FoodTemplate
                {
                    Name = "Turkey Breast",
                    Category = "Proteins",
                    ServingSize = 100,
                    ServingUnit = "g",
                    Calories = 135,
                    Protein = 30,
                    Carbohydrates = 0,
                    Fat = 0.7m,
                    Sodium = 46,
                    Description = "Roasted turkey breast, skinless"
                },
                new FoodTemplate
                {
                    Name = "Shrimp",
                    Category = "Proteins",
                    ServingSize = 100,
                    ServingUnit = "g",
                    Calories = 99,
                    Protein = 24,
                    Carbohydrates = 0.2m,
                    Fat = 0.3m,
                    Cholesterol = 189,
                    Sodium = 111,
                    Description = "Cooked shrimp"
                },
                new FoodTemplate
                {
                    Name = "Cottage Cheese (Low-fat)",
                    Category = "Proteins",
                    ServingSize = 113,
                    ServingUnit = "g",
                    Calories = 81,
                    Protein = 14,
                    Carbohydrates = 3.4m,
                    Fat = 1.1m,
                    Sugar = 2.7m,
                    Sodium = 348,
                    Calcium = 83,
                    Description = "1% milkfat cottage cheese"
                },
                new FoodTemplate
                {
                    Name = "Pork Tenderloin",
                    Category = "Proteins",
                    ServingSize = 100,
                    ServingUnit = "g",
                    Calories = 143,
                    Protein = 26,
                    Carbohydrates = 0,
                    Fat = 3.5m,
                    Sodium = 48,
                    Description = "Lean pork tenderloin, roasted"
                },
                new FoodTemplate
                {
                    Name = "Egg Whites",
                    Category = "Proteins",
                    ServingSize = 100,
                    ServingUnit = "g",
                    Calories = 52,
                    Protein = 11,
                    Carbohydrates = 0.7m,
                    Fat = 0.2m,
                    Sodium = 166,
                    Description = "Egg whites only"
                },
                new FoodTemplate
                {
                    Name = "Tempeh",
                    Category = "Proteins",
                    ServingSize = 100,
                    ServingUnit = "g",
                    Calories = 192,
                    Protein = 20,
                    Carbohydrates = 7.6m,
                    Fat = 11,
                    Fiber = 7.5m,
                    Iron = 2.7m,
                    Description = "Fermented soybean cake"
                },
                new FoodTemplate
                {
                    Name = "Cod",
                    Category = "Proteins",
                    ServingSize = 100,
                    ServingUnit = "g",
                    Calories = 82,
                    Protein = 18,
                    Carbohydrates = 0,
                    Fat = 0.7m,
                    Sodium = 54,
                    Description = "Atlantic cod, cooked"
                },
                new FoodTemplate
                {
                    Name = "Whey Protein Powder",
                    Category = "Proteins",
                    ServingSize = 30,
                    ServingUnit = "g",
                    Calories = 120,
                    Protein = 24,
                    Carbohydrates = 3,
                    Fat = 1.5m,
                    Sugar = 2,
                    Description = "Whey protein isolate powder"
                },

                // ============ CARBOHYDRATES (15 items) ============
                new FoodTemplate
                {
                    Name = "Brown Rice",
                    Category = "Carbohydrates",
                    ServingSize = 195,
                    ServingUnit = "g",
                    Calories = 248,
                    Protein = 5.5m,
                    Carbohydrates = 52,
                    Fat = 2,
                    Fiber = 3.2m,
                    Description = "Cooked long-grain brown rice"
                },
                new FoodTemplate
                {
                    Name = "Oatmeal",
                    Category = "Carbohydrates",
                    ServingSize = 40,
                    ServingUnit = "g",
                    Calories = 150,
                    Protein = 5,
                    Carbohydrates = 27,
                    Fat = 3,
                    Fiber = 4,
                    Description = "Rolled oats, dry"
                },
                new FoodTemplate
                {
                    Name = "Sweet Potato",
                    Category = "Carbohydrates",
                    ServingSize = 150,
                    ServingUnit = "g",
                    Calories = 129,
                    Protein = 2.4m,
                    Carbohydrates = 30,
                    Fat = 0.1m,
                    Fiber = 4.5m,
                    Sugar = 6.5m,
                    VitaminA = 1403,
                    Description = "Baked sweet potato with skin"
                },
                new FoodTemplate
                {
                    Name = "Quinoa",
                    Category = "Carbohydrates",
                    ServingSize = 185,
                    ServingUnit = "g",
                    Calories = 222,
                    Protein = 8,
                    Carbohydrates = 39,
                    Fat = 3.6m,
                    Fiber = 5,
                    Iron = 2.8m,
                    Description = "Cooked quinoa"
                },
                new FoodTemplate
                {
                    Name = "Whole Wheat Bread",
                    Category = "Carbohydrates",
                    ServingSize = 43,
                    ServingUnit = "g",
                    Calories = 110,
                    Protein = 5,
                    Carbohydrates = 20,
                    Fat = 2,
                    Fiber = 3,
                    Sodium = 200,
                    Description = "Whole wheat bread slice"
                },
                new FoodTemplate
                {
                    Name = "White Rice",
                    Category = "Carbohydrates",
                    ServingSize = 158,
                    ServingUnit = "g",
                    Calories = 206,
                    Protein = 4.3m,
                    Carbohydrates = 45,
                    Fat = 0.4m,
                    Fiber = 0.6m,
                    Description = "Cooked white rice, long grain"
                },
                new FoodTemplate
                {
                    Name = "Whole Wheat Pasta",
                    Category = "Carbohydrates",
                    ServingSize = 140,
                    ServingUnit = "g",
                    Calories = 174,
                    Protein = 7.5m,
                    Carbohydrates = 37,
                    Fat = 0.8m,
                    Fiber = 6.3m,
                    Description = "Cooked whole wheat spaghetti"
                },
                new FoodTemplate
                {
                    Name = "Potato",
                    Category = "Carbohydrates",
                    ServingSize = 173,
                    ServingUnit = "g",
                    Calories = 161,
                    Protein = 4.3m,
                    Carbohydrates = 37,
                    Fat = 0.2m,
                    Fiber = 3.8m,
                    Potassium = 926,
                    VitaminC = 17,
                    Description = "Medium baked potato with skin"
                },
                new FoodTemplate
                {
                    Name = "Black Beans",
                    Category = "Carbohydrates",
                    ServingSize = 172,
                    ServingUnit = "g",
                    Calories = 227,
                    Protein = 15,
                    Carbohydrates = 41,
                    Fat = 0.9m,
                    Fiber = 15,
                    Iron = 3.6m,
                    Description = "Cooked black beans"
                },
                new FoodTemplate
                {
                    Name = "Chickpeas",
                    Category = "Carbohydrates",
                    ServingSize = 164,
                    ServingUnit = "g",
                    Calories = 269,
                    Protein = 15,
                    Carbohydrates = 45,
                    Fat = 4.2m,
                    Fiber = 12.5m,
                    Iron = 4.7m,
                    Description = "Cooked chickpeas (garbanzo beans)"
                },
                new FoodTemplate
                {
                    Name = "Lentils",
                    Category = "Carbohydrates",
                    ServingSize = 198,
                    ServingUnit = "g",
                    Calories = 230,
                    Protein = 18,
                    Carbohydrates = 40,
                    Fat = 0.8m,
                    Fiber = 16,
                    Iron = 6.6m,
                    Description = "Cooked lentils"
                },
                new FoodTemplate
                {
                    Name = "Banana",
                    Category = "Carbohydrates",
                    ServingSize = 118,
                    ServingUnit = "g",
                    Calories = 105,
                    Protein = 1.3m,
                    Carbohydrates = 27,
                    Fat = 0.4m,
                    Fiber = 3.1m,
                    Sugar = 14,
                    Potassium = 422,
                    Description = "Medium banana"
                },
                new FoodTemplate
                {
                    Name = "Corn Tortilla",
                    Category = "Carbohydrates",
                    ServingSize = 26,
                    ServingUnit = "g",
                    Calories = 52,
                    Protein = 1.4m,
                    Carbohydrates = 11,
                    Fat = 0.7m,
                    Fiber = 1.5m,
                    Description = "6-inch corn tortilla"
                },
                new FoodTemplate
                {
                    Name = "Bagel",
                    Category = "Carbohydrates",
                    ServingSize = 98,
                    ServingUnit = "g",
                    Calories = 277,
                    Protein = 11,
                    Carbohydrates = 54,
                    Fat = 1.4m,
                    Fiber = 2.4m,
                    Sodium = 452,
                    Description = "Plain bagel"
                },
                new FoodTemplate
                {
                    Name = "English Muffin",
                    Category = "Carbohydrates",
                    ServingSize = 57,
                    ServingUnit = "g",
                    Calories = 134,
                    Protein = 5,
                    Carbohydrates = 26,
                    Fat = 1,
                    Fiber = 2,
                    Description = "Whole wheat English muffin"
                },

                // ============ VEGETABLES (12 items) ============
                new FoodTemplate
                {
                    Name = "Broccoli",
                    Category = "Vegetables",
                    ServingSize = 91,
                    ServingUnit = "g",
                    Calories = 31,
                    Protein = 2.5m,
                    Carbohydrates = 6,
                    Fat = 0.4m,
                    Fiber = 2.4m,
                    VitaminC = 81,
                    VitaminA = 31,
                    Description = "Raw broccoli florets"
                },
                new FoodTemplate
                {
                    Name = "Spinach",
                    Category = "Vegetables",
                    ServingSize = 30,
                    ServingUnit = "g",
                    Calories = 7,
                    Protein = 0.9m,
                    Carbohydrates = 1.1m,
                    Fat = 0.1m,
                    Fiber = 0.7m,
                    Iron = 0.8m,
                    VitaminA = 141,
                    Description = "Raw spinach leaves"
                },
                new FoodTemplate
                {
                    Name = "Kale",
                    Category = "Vegetables",
                    ServingSize = 67,
                    ServingUnit = "g",
                    Calories = 33,
                    Protein = 2.2m,
                    Carbohydrates = 6,
                    Fat = 0.5m,
                    Fiber = 1.3m,
                    VitaminA = 241,
                    VitaminC = 80,
                    Description = "Raw kale, chopped"
                },
                new FoodTemplate
                {
                    Name = "Bell Pepper",
                    Category = "Vegetables",
                    ServingSize = 119,
                    ServingUnit = "g",
                    Calories = 31,
                    Protein = 1,
                    Carbohydrates = 6,
                    Fat = 0.4m,
                    Fiber = 2.1m,
                    VitaminC = 152,
                    VitaminA = 18,
                    Description = "Medium red bell pepper"
                },
                new FoodTemplate
                {
                    Name = "Carrots",
                    Category = "Vegetables",
                    ServingSize = 61,
                    ServingUnit = "g",
                    Calories = 25,
                    Protein = 0.6m,
                    Carbohydrates = 6,
                    Fat = 0.1m,
                    Fiber = 1.7m,
                    Sugar = 2.9m,
                    VitaminA = 509,
                    Description = "Medium raw carrot"
                },
                new FoodTemplate
                {
                    Name = "Cucumber",
                    Category = "Vegetables",
                    ServingSize = 104,
                    ServingUnit = "g",
                    Calories = 16,
                    Protein = 0.7m,
                    Carbohydrates = 3.8m,
                    Fat = 0.1m,
                    Fiber = 0.5m,
                    Description = "Raw cucumber with peel"
                },
                new FoodTemplate
                {
                    Name = "Tomato",
                    Category = "Vegetables",
                    ServingSize = 123,
                    ServingUnit = "g",
                    Calories = 22,
                    Protein = 1.1m,
                    Carbohydrates = 4.8m,
                    Fat = 0.2m,
                    Fiber = 1.5m,
                    VitaminC = 17,
                    Description = "Medium tomato"
                },
                new FoodTemplate
                {
                    Name = "Asparagus",
                    Category = "Vegetables",
                    ServingSize = 134,
                    ServingUnit = "g",
                    Calories = 27,
                    Protein = 3,
                    Carbohydrates = 5,
                    Fat = 0.2m,
                    Fiber = 2.8m,
                    VitaminA = 51,
                    Description = "Cooked asparagus spears"
                },
                new FoodTemplate
                {
                    Name = "Zucchini",
                    Category = "Vegetables",
                    ServingSize = 124,
                    ServingUnit = "g",
                    Calories = 21,
                    Protein = 1.5m,
                    Carbohydrates = 3.9m,
                    Fat = 0.4m,
                    Fiber = 1.2m,
                    Description = "Medium zucchini, raw"
                },
                new FoodTemplate
                {
                    Name = "Cauliflower",
                    Category = "Vegetables",
                    ServingSize = 107,
                    ServingUnit = "g",
                    Calories = 27,
                    Protein = 2,
                    Carbohydrates = 5,
                    Fat = 0.3m,
                    Fiber = 2.1m,
                    VitaminC = 52,
                    Description = "Raw cauliflower florets"
                },
                new FoodTemplate
                {
                    Name = "Green Beans",
                    Category = "Vegetables",
                    ServingSize = 100,
                    ServingUnit = "g",
                    Calories = 31,
                    Protein = 1.8m,
                    Carbohydrates = 7,
                    Fat = 0.1m,
                    Fiber = 2.7m,
                    VitaminA = 35,
                    Description = "Cooked green beans"
                },
                new FoodTemplate
                {
                    Name = "Brussels Sprouts",
                    Category = "Vegetables",
                    ServingSize = 88,
                    ServingUnit = "g",
                    Calories = 38,
                    Protein = 3,
                    Carbohydrates = 8,
                    Fat = 0.3m,
                    Fiber = 3.3m,
                    VitaminC = 75,
                    Description = "Cooked Brussels sprouts"
                },

                // ============ FRUITS (8 items) ============
                new FoodTemplate
                {
                    Name = "Apple",
                    Category = "Fruits",
                    ServingSize = 182,
                    ServingUnit = "g",
                    Calories = 95,
                    Protein = 0.5m,
                    Carbohydrates = 25,
                    Fat = 0.3m,
                    Fiber = 4.4m,
                    Sugar = 19,
                    VitaminC = 8,
                    Description = "Medium apple with skin"
                },
                new FoodTemplate
                {
                    Name = "Blueberries",
                    Category = "Fruits",
                    ServingSize = 148,
                    ServingUnit = "g",
                    Calories = 84,
                    Protein = 1.1m,
                    Carbohydrates = 21,
                    Fat = 0.5m,
                    Fiber = 3.6m,
                    Sugar = 15,
                    VitaminC = 14,
                    Description = "Fresh blueberries"
                },
                new FoodTemplate
                {
                    Name = "Orange",
                    Category = "Fruits",
                    ServingSize = 131,
                    ServingUnit = "g",
                    Calories = 62,
                    Protein = 1.2m,
                    Carbohydrates = 15,
                    Fat = 0.2m,
                    Fiber = 3.1m,
                    Sugar = 12,
                    VitaminC = 70,
                    Description = "Medium orange"
                },
                new FoodTemplate
                {
                    Name = "Avocado",
                    Category = "Fruits",
                    ServingSize = 150,
                    ServingUnit = "g",
                    Calories = 240,
                    Protein = 3,
                    Carbohydrates = 13,
                    Fat = 22,
                    Fiber = 10,
                    Potassium = 728,
                    Description = "Medium avocado"
                },
                new FoodTemplate
                {
                    Name = "Strawberries",
                    Category = "Fruits",
                    ServingSize = 152,
                    ServingUnit = "g",
                    Calories = 49,
                    Protein = 1,
                    Carbohydrates = 12,
                    Fat = 0.5m,
                    Fiber = 3,
                    Sugar = 7,
                    VitaminC = 89,
                    Description = "Fresh strawberries"
                },
                new FoodTemplate
                {
                    Name = "Grapes",
                    Category = "Fruits",
                    ServingSize = 151,
                    ServingUnit = "g",
                    Calories = 104,
                    Protein = 1.1m,
                    Carbohydrates = 27,
                    Fat = 0.2m,
                    Fiber = 1.4m,
                    Sugar = 23,
                    Description = "Red or green grapes"
                },
                new FoodTemplate
                {
                    Name = "Mango",
                    Category = "Fruits",
                    ServingSize = 165,
                    ServingUnit = "g",
                    Calories = 99,
                    Protein = 1.4m,
                    Carbohydrates = 25,
                    Fat = 0.6m,
                    Fiber = 2.6m,
                    Sugar = 23,
                    VitaminC = 60,
                    VitaminA = 89,
                    Description = "Fresh mango, sliced"
                },
                new FoodTemplate
                {
                    Name = "Pineapple",
                    Category = "Fruits",
                    ServingSize = 165,
                    ServingUnit = "g",
                    Calories = 82,
                    Protein = 0.9m,
                    Carbohydrates = 22,
                    Fat = 0.2m,
                    Fiber = 2.3m,
                    Sugar = 16,
                    VitaminC = 79,
                    Description = "Fresh pineapple chunks"
                },

                // ============ DAIRY (8 items) ============
                new FoodTemplate
                {
                    Name = "Milk (2%)",
                    Category = "Dairy",
                    ServingSize = 244,
                    ServingUnit = "ml",
                    Calories = 122,
                    Protein = 8,
                    Carbohydrates = 12,
                    Fat = 5,
                    Sugar = 12,
                    Calcium = 293,
                    VitaminD = 2.9m,
                    Description = "2% reduced fat milk"
                },
                new FoodTemplate
                {
                    Name = "Cheddar Cheese",
                    Category = "Dairy",
                    ServingSize = 28,
                    ServingUnit = "g",
                    Calories = 113,
                    Protein = 7,
                    Carbohydrates = 0.4m,
                    Fat = 9,
                    SaturatedFat = 6,
                    Calcium = 200,
                    Sodium = 174,
                    Description = "Sharp cheddar cheese"
                },
                new FoodTemplate
                {
                    Name = "Almond Milk (Unsweetened)",
                    Category = "Dairy",
                    ServingSize = 240,
                    ServingUnit = "ml",
                    Calories = 30,
                    Protein = 1,
                    Carbohydrates = 1,
                    Fat = 2.5m,
                    Calcium = 450,
                    Description = "Unsweetened almond milk"
                },
                new FoodTemplate
                {
                    Name = "Mozzarella Cheese",
                    Category = "Dairy",
                    ServingSize = 28,
                    ServingUnit = "g",
                    Calories = 85,
                    Protein = 6,
                    Carbohydrates = 0.6m,
                    Fat = 6,
                    Calcium = 143,
                    Sodium = 138,
                    Description = "Part-skim mozzarella"
                },
                new FoodTemplate
                {
                    Name = "Skim Milk",
                    Category = "Dairy",
                    ServingSize = 244,
                    ServingUnit = "ml",
                    Calories = 83,
                    Protein = 8,
                    Carbohydrates = 12,
                    Fat = 0.2m,
                    Sugar = 12,
                    Calcium = 299,
                    Description = "Fat-free skim milk"
                },
                new FoodTemplate
                {
                    Name = "Parmesan Cheese",
                    Category = "Dairy",
                    ServingSize = 28,
                    ServingUnit = "g",
                    Calories = 111,
                    Protein = 10,
                    Carbohydrates = 0.9m,
                    Fat = 7,
                    Calcium = 336,
                    Sodium = 454,
                    Description = "Grated parmesan cheese"
                },
                new FoodTemplate
                {
                    Name = "Butter",
                    Category = "Dairy",
                    ServingSize = 14,
                    ServingUnit = "g",
                    Calories = 102,
                    Protein = 0.1m,
                    Carbohydrates = 0,
                    Fat = 11.5m,
                    SaturatedFat = 7.3m,
                    Cholesterol = 31,
                    Description = "Salted butter"
                },
                new FoodTemplate
                {
                    Name = "Cream Cheese",
                    Category = "Dairy",
                    ServingSize = 28,
                    ServingUnit = "g",
                    Calories = 99,
                    Protein = 1.7m,
                    Carbohydrates = 1.6m,
                    Fat = 10,
                    SaturatedFat = 5.7m,
                    Sodium = 91,
                    Description = "Regular cream cheese"
                },

                // ============ FATS & OILS (6 items) ============
                new FoodTemplate
                {
                    Name = "Olive Oil",
                    Category = "Fats & Oils",
                    ServingSize = 14,
                    ServingUnit = "ml",
                    Calories = 119,
                    Protein = 0,
                    Carbohydrates = 0,
                    Fat = 13.5m,
                    SaturatedFat = 1.9m,
                    Description = "Extra virgin olive oil"
                },
                new FoodTemplate
                {
                    Name = "Almonds",
                    Category = "Fats & Oils",
                    ServingSize = 28,
                    ServingUnit = "g",
                    Calories = 164,
                    Protein = 6,
                    Carbohydrates = 6,
                    Fat = 14,
                    Fiber = 3.5m,
                    VitaminA = 0,
                    Calcium = 76,
                    Description = "Raw almonds"
                },
                new FoodTemplate
                {
                    Name = "Peanut Butter",
                    Category = "Fats & Oils",
                    ServingSize = 32,
                    ServingUnit = "g",
                    Calories = 188,
                    Protein = 8,
                    Carbohydrates = 6,
                    Fat = 16,
                    Fiber = 2,
                    Sugar = 3,
                    Sodium = 136,
                    Description = "Creamy peanut butter"
                },
                new FoodTemplate
                {
                    Name = "Chia Seeds",
                    Category = "Fats & Oils",
                    ServingSize = 28,
                    ServingUnit = "g",
                    Calories = 138,
                    Protein = 4.7m,
                    Carbohydrates = 12,
                    Fat = 9,
                    Fiber = 10,
                    Calcium = 179,
                    Description = "Dried chia seeds"
                },
                new FoodTemplate
                {
                    Name = "Walnuts",
                    Category = "Fats & Oils",
                    ServingSize = 28,
                    ServingUnit = "g",
                    Calories = 185,
                    Protein = 4.3m,
                    Carbohydrates = 3.9m,
                    Fat = 18.5m,
                    Fiber = 1.9m,
                    Description = "English walnuts"
                },
                new FoodTemplate
                {
                    Name = "Coconut Oil",
                    Category = "Fats & Oils",
                    ServingSize = 14,
                    ServingUnit = "ml",
                    Calories = 121,
                    Protein = 0,
                    Carbohydrates = 0,
                    Fat = 13.5m,
                    SaturatedFat = 11.2m,
                    Description = "Virgin coconut oil"
                },

                // ============ SNACKS (8 items) ============
                new FoodTemplate
                {
                    Name = "Hummus",
                    Category = "Snacks",
                    ServingSize = 30,
                    ServingUnit = "g",
                    Calories = 52,
                    Protein = 2,
                    Carbohydrates = 4,
                    Fat = 3.5m,
                    Fiber = 1,
                    Description = "Classic hummus"
                },
                new FoodTemplate
                {
                    Name = "Dark Chocolate (70%)",
                    Category = "Snacks",
                    ServingSize = 28,
                    ServingUnit = "g",
                    Calories = 170,
                    Protein = 2,
                    Carbohydrates = 13,
                    Fat = 12,
                    Fiber = 3,
                    Sugar = 7,
                    Iron = 3.3m,
                    Description = "70% cacao dark chocolate"
                },
                new FoodTemplate
                {
                    Name = "Rice Cakes",
                    Category = "Snacks",
                    ServingSize = 9,
                    ServingUnit = "g",
                    Calories = 35,
                    Protein = 0.7m,
                    Carbohydrates = 7.3m,
                    Fat = 0.3m,
                    Fiber = 0.4m,
                    Description = "Plain rice cake"
                },
                new FoodTemplate
                {
                    Name = "Protein Bar",
                    Category = "Snacks",
                    ServingSize = 60,
                    ServingUnit = "g",
                    Calories = 200,
                    Protein = 20,
                    Carbohydrates = 22,
                    Fat = 6,
                    Fiber = 3,
                    Sugar = 5,
                    Description = "Average protein bar"
                },
                new FoodTemplate
                {
                    Name = "Trail Mix",
                    Category = "Snacks",
                    ServingSize = 40,
                    ServingUnit = "g",
                    Calories = 173,
                    Protein = 5,
                    Carbohydrates = 17,
                    Fat = 11,
                    Fiber = 2,
                    Sugar = 9,
                    Description = "Mixed nuts and dried fruit"
                },
                new FoodTemplate
                {
                    Name = "Beef Jerky",
                    Category = "Snacks",
                    ServingSize = 28,
                    ServingUnit = "g",
                    Calories = 116,
                    Protein = 9.4m,
                    Carbohydrates = 3.1m,
                    Fat = 7.3m,
                    Sodium = 506,
                    Description = "Original beef jerky"
                },
                new FoodTemplate
                {
                    Name = "Popcorn (Air-popped)",
                    Category = "Snacks",
                    ServingSize = 8,
                    ServingUnit = "g",
                    Calories = 31,
                    Protein = 1,
                    Carbohydrates = 6.2m,
                    Fat = 0.4m,
                    Fiber = 1.2m,
                    Description = "Plain air-popped popcorn"
                },
                new FoodTemplate
                {
                    Name = "String Cheese",
                    Category = "Snacks",
                    ServingSize = 28,
                    ServingUnit = "g",
                    Calories = 80,
                    Protein = 7,
                    Carbohydrates = 1,
                    Fat = 6,
                    Calcium = 200,
                    Sodium = 200,
                    Description = "Low-moisture part-skim mozzarella"
                },

                // ============ BEVERAGES (4 items) ============
                new FoodTemplate
                {
                    Name = "Coffee (Black)",
                    Category = "Beverages",
                    ServingSize = 240,
                    ServingUnit = "ml",
                    Calories = 2,
                    Protein = 0.3m,
                    Carbohydrates = 0,
                    Fat = 0,
                    Description = "Brewed black coffee"
                },
                new FoodTemplate
                {
                    Name = "Green Tea",
                    Category = "Beverages",
                    ServingSize = 245,
                    ServingUnit = "ml",
                    Calories = 2,
                    Protein = 0,
                    Carbohydrates = 0,
                    Fat = 0,
                    Description = "Brewed green tea, unsweetened"
                },
                new FoodTemplate
                {
                    Name = "Orange Juice",
                    Category = "Beverages",
                    ServingSize = 240,
                    ServingUnit = "ml",
                    Calories = 112,
                    Protein = 2,
                    Carbohydrates = 26,
                    Fat = 0.5m,
                    Sugar = 21,
                    VitaminC = 124,
                    Potassium = 496,
                    Description = "Fresh squeezed orange juice"
                },
                new FoodTemplate
                {
                    Name = "Coconut Water",
                    Category = "Beverages",
                    ServingSize = 240,
                    ServingUnit = "ml",
                    Calories = 46,
                    Protein = 1.7m,
                    Carbohydrates = 9,
                    Fat = 0.5m,
                    Sugar = 6,
                    Potassium = 600,
                    Sodium = 252,
                    Description = "Pure coconut water"
                }
            };
        }
    }
}
