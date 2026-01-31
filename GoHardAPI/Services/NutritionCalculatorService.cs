namespace GoHardAPI.Services
{
    /// <summary>
    /// Service for calculating personalized nutrition targets based on user metrics and goals.
    /// Uses the Mifflin-St Jeor equation for BMR and standard activity multipliers for TDEE.
    /// </summary>
    public class NutritionCalculatorService
    {
        /// <summary>
        /// Activity level multipliers for TDEE calculation
        /// </summary>
        private static readonly Dictionary<string, decimal> ActivityMultipliers = new()
        {
            { "Sedentary", 1.2m },           // Little or no exercise, desk job
            { "LightlyActive", 1.375m },     // Light exercise 1-3 days/week
            { "ModeratelyActive", 1.55m },   // Moderate exercise 3-5 days/week
            { "VeryActive", 1.725m },        // Hard exercise 6-7 days/week
            { "ExtremelyActive", 1.9m }      // Very hard exercise, physical job, or 2x training
        };

        /// <summary>
        /// Calculate BMR using the Mifflin-St Jeor equation
        /// Men: BMR = (10 × weight in kg) + (6.25 × height in cm) - (5 × age) + 5
        /// Women: BMR = (10 × weight in kg) + (6.25 × height in cm) - (5 × age) - 161
        /// </summary>
        public decimal CalculateBMR(decimal weightKg, decimal heightCm, int age, string gender)
        {
            var genderLower = gender?.ToLower() ?? "male";
            var genderOffset = genderLower == "female" ? -161m : 5m;

            return (10m * weightKg) + (6.25m * heightCm) - (5m * age) + genderOffset;
        }

        /// <summary>
        /// Calculate TDEE (Total Daily Energy Expenditure) from BMR and activity level
        /// </summary>
        public decimal CalculateTDEE(decimal bmr, string activityLevel)
        {
            var level = activityLevel ?? "ModeratelyActive";

            if (!ActivityMultipliers.TryGetValue(level, out var multiplier))
            {
                // Default to moderately active if invalid level
                multiplier = 1.55m;
            }

            return bmr * multiplier;
        }

        /// <summary>
        /// Calculate target calories based on TDEE and goal type
        /// </summary>
        public decimal CalculateTargetCalories(decimal tdee, string goalType, decimal? targetWeightChangePerWeek = null)
        {
            var goal = goalType?.ToLower() ?? "maintenance";

            if (goal.Contains("loss") || goal.Contains("weightloss") || goal.Contains("cut"))
            {
                // Weight loss: create calorie deficit
                // 3500 calories = 1 lb of fat
                // Default to 1 lb/week (500 cal deficit) if not specified
                var weeklyLoss = targetWeightChangePerWeek ?? 1m; // lbs per week
                var dailyDeficit = Math.Min((weeklyLoss * 3500m) / 7m, 1000m); // Cap at 1000 cal/day for safety
                return Math.Max(tdee - dailyDeficit, 1200m); // Minimum 1200 calories for health
            }
            else if (goal.Contains("gain") || goal.Contains("muscle") || goal.Contains("bulk"))
            {
                // Muscle gain: create calorie surplus
                // 250-500 cal surplus for lean gains
                var surplus = targetWeightChangePerWeek.HasValue
                    ? Math.Min((targetWeightChangePerWeek.Value * 3500m) / 7m, 500m)
                    : 300m;
                return tdee + surplus;
            }
            else
            {
                // Maintenance
                return tdee;
            }
        }

        /// <summary>
        /// Calculate protein target based on body weight and goal
        /// </summary>
        public decimal CalculateProtein(decimal weightKg, string goalType)
        {
            var weightLbs = weightKg * 2.205m;
            var goal = goalType?.ToLower() ?? "maintenance";

            if (goal.Contains("loss") || goal.Contains("cut"))
            {
                // Higher protein during weight loss to preserve muscle
                // 1.0-1.2g per lb body weight
                return weightLbs * 1.1m;
            }
            else if (goal.Contains("gain") || goal.Contains("muscle") || goal.Contains("bulk"))
            {
                // High protein for muscle building
                // 1.0-1.2g per lb body weight
                return weightLbs * 1.0m;
            }
            else
            {
                // Maintenance: moderate protein
                // 0.8g per lb body weight
                return weightLbs * 0.8m;
            }
        }

        /// <summary>
        /// Calculate macro split (carbs and fat) based on remaining calories after protein
        /// </summary>
        public (decimal carbs, decimal fat) CalculateMacros(decimal totalCalories, decimal protein, string goalType)
        {
            // Protein calories (4 cal/g)
            var proteinCalories = protein * 4m;
            var remainingCalories = totalCalories - proteinCalories;

            var goal = goalType?.ToLower() ?? "maintenance";

            decimal carbPercentage, fatPercentage;

            if (goal.Contains("loss") || goal.Contains("cut"))
            {
                // Weight loss: moderate carbs, moderate fat
                // Remaining calories split: ~50% carbs, ~50% fat
                carbPercentage = 0.5m;
                fatPercentage = 0.5m;
            }
            else if (goal.Contains("gain") || goal.Contains("muscle") || goal.Contains("bulk"))
            {
                // Muscle gain: higher carbs for energy
                // Remaining calories split: ~60% carbs, ~40% fat
                carbPercentage = 0.6m;
                fatPercentage = 0.4m;
            }
            else
            {
                // Maintenance: balanced
                // Remaining calories split: ~55% carbs, ~45% fat
                carbPercentage = 0.55m;
                fatPercentage = 0.45m;
            }

            // Carbs: 4 cal/g, Fat: 9 cal/g
            var carbCalories = remainingCalories * carbPercentage;
            var fatCalories = remainingCalories * fatPercentage;

            var carbs = carbCalories / 4m;
            var fat = fatCalories / 9m;

            return (Math.Round(carbs), Math.Round(fat));
        }

        /// <summary>
        /// Calculate all nutrition targets at once
        /// </summary>
        public NutritionCalculation CalculateNutrition(
            decimal weightKg,
            decimal heightCm,
            int age,
            string gender,
            string activityLevel,
            string goalType,
            decimal? targetWeightChangePerWeek = null)
        {
            var bmr = CalculateBMR(weightKg, heightCm, age, gender);
            var tdee = CalculateTDEE(bmr, activityLevel);
            var targetCalories = CalculateTargetCalories(tdee, goalType, targetWeightChangePerWeek);
            var protein = CalculateProtein(weightKg, goalType);
            var (carbs, fat) = CalculateMacros(targetCalories, protein, goalType);

            // Calculate calorie adjustment
            var calorieAdjustment = targetCalories - tdee;
            var weeklyWeightChange = (calorieAdjustment * 7m) / 3500m; // lbs per week

            return new NutritionCalculation
            {
                Bmr = Math.Round(bmr),
                Tdee = Math.Round(tdee),
                DailyCalories = Math.Round(targetCalories),
                DailyProtein = Math.Round(protein),
                DailyCarbohydrates = carbs,
                DailyFat = fat,
                CalorieAdjustment = Math.Round(calorieAdjustment),
                ExpectedWeeklyWeightChange = Math.Round(weeklyWeightChange, 2),
                Explanation = GenerateExplanation(bmr, tdee, targetCalories, protein, carbs, fat, goalType, activityLevel, calorieAdjustment)
            };
        }

        private string GenerateExplanation(
            decimal bmr,
            decimal tdee,
            decimal calories,
            decimal protein,
            decimal carbs,
            decimal fat,
            string goalType,
            string activityLevel,
            decimal calorieAdjustment)
        {
            var activityDesc = activityLevel switch
            {
                "Sedentary" => "sedentary (little or no exercise)",
                "LightlyActive" => "lightly active (light exercise 1-3 days/week)",
                "ModeratelyActive" => "moderately active (moderate exercise 3-5 days/week)",
                "VeryActive" => "very active (hard exercise 6-7 days/week)",
                "ExtremelyActive" => "extremely active (very hard exercise or physical job)",
                _ => "moderately active"
            };

            var goal = goalType?.ToLower() ?? "maintenance";
            var goalDesc = goal.Contains("loss") ? "weight loss"
                : goal.Contains("gain") || goal.Contains("muscle") ? "muscle gain"
                : "maintenance";

            var adjustmentDesc = calorieAdjustment < 0
                ? $"a {Math.Abs(calorieAdjustment):F0} calorie deficit"
                : calorieAdjustment > 0
                    ? $"a {calorieAdjustment:F0} calorie surplus"
                    : "maintenance calories";

            return $"Based on your profile, your Basal Metabolic Rate (BMR) is {bmr:F0} calories. " +
                   $"With your {activityDesc} lifestyle, your Total Daily Energy Expenditure (TDEE) is {tdee:F0} calories. " +
                   $"For {goalDesc}, we recommend {calories:F0} calories daily ({adjustmentDesc}). " +
                   $"Your macros: {protein:F0}g protein, {carbs:F0}g carbs, {fat:F0}g fat.";
        }

        /// <summary>
        /// Calculate age from date of birth
        /// </summary>
        public static int CalculateAge(DateTime? dateOfBirth)
        {
            if (!dateOfBirth.HasValue) return 30; // Default age if not provided

            var today = DateTime.UtcNow;
            var age = today.Year - dateOfBirth.Value.Year;

            if (dateOfBirth.Value.Date > today.AddYears(-age))
                age--;

            return age;
        }
    }

    /// <summary>
    /// Result of nutrition calculation
    /// </summary>
    public class NutritionCalculation
    {
        public decimal Bmr { get; set; }
        public decimal Tdee { get; set; }
        public decimal DailyCalories { get; set; }
        public decimal DailyProtein { get; set; }
        public decimal DailyCarbohydrates { get; set; }
        public decimal DailyFat { get; set; }
        public decimal CalorieAdjustment { get; set; }
        public decimal ExpectedWeeklyWeightChange { get; set; }
        public string Explanation { get; set; } = string.Empty;
    }
}
