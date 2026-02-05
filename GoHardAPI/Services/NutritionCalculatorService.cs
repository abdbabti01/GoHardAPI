namespace GoHardAPI.Services
{
    /// <summary>
    /// Nutrition goal types for calculation purposes
    /// </summary>
    public enum NutritionGoalType
    {
        Maintenance,
        WeightLoss,
        MuscleGain
    }

    /// <summary>
    /// Service for calculating personalized nutrition targets based on user metrics and goals.
    /// Uses the Mifflin-St Jeor equation for BMR and standard activity multipliers for TDEE.
    /// </summary>
    public class NutritionCalculatorService
    {
        /// <summary>
        /// Parse a string goal type to the enum, handling various input formats
        /// </summary>
        public static NutritionGoalType ParseGoalType(string? goalType)
        {
            if (string.IsNullOrWhiteSpace(goalType))
                return NutritionGoalType.Maintenance;

            var normalized = goalType.ToLowerInvariant().Trim();

            // Weight loss variants
            if (normalized.Contains("loss") || normalized.Contains("cut") || normalized == "weightloss")
                return NutritionGoalType.WeightLoss;

            // Muscle gain variants
            if (normalized.Contains("gain") || normalized.Contains("muscle") || normalized.Contains("bulk"))
                return NutritionGoalType.MuscleGain;

            // Default to maintenance
            return NutritionGoalType.Maintenance;
        }
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
        public decimal CalculateTargetCalories(decimal tdee, decimal bmr, NutritionGoalType goalType, decimal? targetWeightChangePerWeek = null)
        {
            return goalType switch
            {
                NutritionGoalType.WeightLoss => CalculateWeightLossCalories(tdee, bmr, targetWeightChangePerWeek),
                NutritionGoalType.MuscleGain => CalculateMuscleGainCalories(tdee, targetWeightChangePerWeek),
                _ => tdee // Maintenance
            };
        }

        private static decimal CalculateWeightLossCalories(decimal tdee, decimal bmr, decimal? targetWeightChangePerWeek)
        {
            // Weight loss: create calorie deficit
            // 3500 calories = 1 lb of fat
            // Default to 1 lb/week (500 cal deficit) if not specified
            var weeklyLoss = targetWeightChangePerWeek ?? 1m; // lbs per week
            var dailyDeficit = Math.Min((weeklyLoss * 3500m) / 7m, 1000m); // Cap at 1000 cal/day for safety

            // Body-weight-relative minimum: never go below 80% of BMR
            // This scales appropriately for both small and large individuals
            // Also enforce absolute minimum of 1200 for safety
            var minimumCalories = Math.Max(bmr * 0.8m, 1200m);

            return Math.Max(tdee - dailyDeficit, minimumCalories);
        }

        private static decimal CalculateMuscleGainCalories(decimal tdee, decimal? targetWeightChangePerWeek)
        {
            // Muscle gain: create calorie surplus
            // 250-500 cal surplus for lean gains
            var surplus = targetWeightChangePerWeek.HasValue
                ? Math.Min((targetWeightChangePerWeek.Value * 3500m) / 7m, 500m)
                : 300m;
            return tdee + surplus;
        }

        /// <summary>
        /// Calculate protein target based on body weight and goal
        /// </summary>
        public decimal CalculateProtein(decimal weightKg, NutritionGoalType goalType)
        {
            var weightLbs = weightKg * 2.205m;

            // Protein multiplier per lb body weight
            var multiplier = goalType switch
            {
                NutritionGoalType.WeightLoss => 1.1m,  // Higher protein to preserve muscle during deficit
                NutritionGoalType.MuscleGain => 1.0m,  // High protein for muscle building
                _ => 0.8m                              // Maintenance: moderate protein
            };

            return weightLbs * multiplier;
        }

        /// <summary>
        /// Calculate macro split (carbs and fat) based on remaining calories after protein
        /// </summary>
        public (decimal carbs, decimal fat) CalculateMacros(decimal totalCalories, decimal protein, NutritionGoalType goalType)
        {
            // Protein calories (4 cal/g)
            var proteinCalories = protein * 4m;
            var remainingCalories = totalCalories - proteinCalories;

            // Macro split percentages based on goal
            var (carbPercentage, fatPercentage) = goalType switch
            {
                NutritionGoalType.WeightLoss => (0.5m, 0.5m),   // Balanced split during deficit
                NutritionGoalType.MuscleGain => (0.6m, 0.4m),   // Higher carbs for energy/performance
                _ => (0.55m, 0.45m)                              // Maintenance: slightly more carbs
            };

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
            // Parse string to enum once at entry point
            var parsedGoalType = ParseGoalType(goalType);

            var bmr = CalculateBMR(weightKg, heightCm, age, gender);
            var tdee = CalculateTDEE(bmr, activityLevel);
            var targetCalories = CalculateTargetCalories(tdee, bmr, parsedGoalType, targetWeightChangePerWeek);
            var protein = CalculateProtein(weightKg, parsedGoalType);
            var (carbs, fat) = CalculateMacros(targetCalories, protein, parsedGoalType);

            // Calculate fiber: 14g per 1000 calories (USDA recommendation)
            // Minimum 25g for women, 38g for men, but we'll use calorie-based for simplicity
            var fiber = Math.Max((targetCalories / 1000m) * 14m, 20m); // Minimum 20g

            // Calculate water: 33ml per kg body weight (Institute of Medicine)
            // This is ~0.5 oz per pound, or roughly 8 cups for a 150lb person
            var water = weightKg * 33m;

            // Calculate calorie adjustment
            var calorieAdjustment = targetCalories - tdee;
            var weeklyWeightChange = (calorieAdjustment * 7m) / 3500m; // lbs per week

            // Generate warnings for aggressive plans
            string? warning = null;
            string? recommendation = null;

            var dailyDeficit = Math.Abs(calorieAdjustment);
            if (calorieAdjustment < 0 && dailyDeficit >= 750)
            {
                warning = $"This is an aggressive calorie deficit ({dailyDeficit:F0} cal/day). " +
                         $"You may experience fatigue, muscle loss, or difficulty sustaining this long-term.";
                recommendation = "A 500 cal/day deficit (1 lb/week loss) is often more sustainable and preserves muscle mass better.";
            }
            else if (calorieAdjustment > 0 && calorieAdjustment >= 500)
            {
                warning = $"This is a significant calorie surplus ({calorieAdjustment:F0} cal/day). " +
                         $"Some fat gain is likely along with muscle.";
                recommendation = "A 250-300 cal/day surplus is often sufficient for muscle gain with minimal fat accumulation.";
            }

            // Warn if calories are at minimum floor (body-weight-relative or absolute)
            var minimumCalories = Math.Max(bmr * 0.8m, 1200m);
            if (targetCalories <= minimumCalories)
            {
                var minType = bmr * 0.8m > 1200m ? "80% of your BMR" : "1200 calories";
                warning = $"Your calorie target has been set to the minimum safe level ({minimumCalories:F0} cal, {minType}). " +
                         "Eating below this could slow your metabolism and cause muscle loss.";
                recommendation = "Consider extending your timeframe to allow for a more moderate deficit, or consult a healthcare provider.";
            }

            return new NutritionCalculation
            {
                Bmr = Math.Round(bmr),
                Tdee = Math.Round(tdee),
                DailyCalories = Math.Round(targetCalories),
                DailyProtein = Math.Round(protein),
                DailyCarbohydrates = carbs,
                DailyFat = fat,
                DailyFiber = Math.Round(fiber),
                DailyWater = Math.Round(water),
                CalorieAdjustment = Math.Round(calorieAdjustment),
                ExpectedWeeklyWeightChange = Math.Round(weeklyWeightChange, 2),
                Explanation = GenerateExplanation(bmr, tdee, targetCalories, protein, carbs, fat, parsedGoalType, activityLevel, calorieAdjustment),
                Warning = warning,
                Recommendation = recommendation
            };
        }

        private string GenerateExplanation(
            decimal bmr,
            decimal tdee,
            decimal calories,
            decimal protein,
            decimal carbs,
            decimal fat,
            NutritionGoalType goalType,
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

            var goalDesc = goalType switch
            {
                NutritionGoalType.WeightLoss => "weight loss",
                NutritionGoalType.MuscleGain => "muscle gain",
                _ => "maintenance"
            };

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

        /// <summary>
        /// Daily fiber target in grams (14g per 1000 calories, USDA recommendation)
        /// </summary>
        public decimal DailyFiber { get; set; }

        /// <summary>
        /// Daily water target in ml (33ml per kg body weight, Institute of Medicine recommendation)
        /// </summary>
        public decimal DailyWater { get; set; }

        public decimal CalorieAdjustment { get; set; }
        public decimal ExpectedWeeklyWeightChange { get; set; }
        public string Explanation { get; set; } = string.Empty;

        /// <summary>
        /// Warning message if the calculated values are aggressive
        /// </summary>
        public string? Warning { get; set; }

        /// <summary>
        /// Recommendation for a safer/more sustainable approach
        /// </summary>
        public string? Recommendation { get; set; }
    }
}
