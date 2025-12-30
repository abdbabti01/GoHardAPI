using GoHardAPI.Models;

namespace GoHardAPI.Data
{
    public static class SeedData
    {
        public static void Initialize(TrainingContext context)
        {
            // Check if we already have exercise templates
            if (context.ExerciseTemplates.Any())
            {
                return; // Database has been seeded
            }

            var templates = new List<ExerciseTemplate>
            {
                // Chest Exercises
                new ExerciseTemplate
                {
                    Name = "Bench Press",
                    Description = "Compound exercise targeting the chest, shoulders, and triceps",
                    Category = "Strength",
                    MuscleGroup = "Chest",
                    Equipment = "Barbell",
                    Difficulty = "Intermediate",
                    Instructions = "Lie on bench, grip bar slightly wider than shoulders, lower to chest, press up"
                },
                new ExerciseTemplate
                {
                    Name = "Push-ups",
                    Description = "Bodyweight exercise for chest and triceps",
                    Category = "Strength",
                    MuscleGroup = "Chest",
                    Equipment = "Bodyweight",
                    Difficulty = "Beginner",
                    Instructions = "Start in plank position, lower body until chest nearly touches floor, push back up"
                },
                new ExerciseTemplate
                {
                    Name = "Dumbbell Flyes",
                    Description = "Isolation exercise for chest",
                    Category = "Strength",
                    MuscleGroup = "Chest",
                    Equipment = "Dumbbell",
                    Difficulty = "Intermediate",
                    Instructions = "Lie on bench, hold dumbbells above chest, lower arms out to sides, bring back together"
                },

                // Back Exercises
                new ExerciseTemplate
                {
                    Name = "Deadlift",
                    Description = "Compound exercise targeting back, glutes, and hamstrings",
                    Category = "Strength",
                    MuscleGroup = "Back",
                    Equipment = "Barbell",
                    Difficulty = "Advanced",
                    Instructions = "Stand with feet hip-width, grip bar, lift by extending hips and knees, lower with control"
                },
                new ExerciseTemplate
                {
                    Name = "Pull-ups",
                    Description = "Bodyweight exercise for back and biceps",
                    Category = "Strength",
                    MuscleGroup = "Back",
                    Equipment = "Pull-up Bar",
                    Difficulty = "Intermediate",
                    Instructions = "Hang from bar with overhand grip, pull body up until chin over bar, lower with control"
                },
                new ExerciseTemplate
                {
                    Name = "Bent-Over Row",
                    Description = "Compound exercise for back thickness",
                    Category = "Strength",
                    MuscleGroup = "Back",
                    Equipment = "Barbell",
                    Difficulty = "Intermediate",
                    Instructions = "Bend at hips, grip bar, pull to lower chest, lower with control"
                },

                // Leg Exercises
                new ExerciseTemplate
                {
                    Name = "Squat",
                    Description = "King of leg exercises, targets quads, glutes, and hamstrings",
                    Category = "Strength",
                    MuscleGroup = "Legs",
                    Equipment = "Barbell",
                    Difficulty = "Intermediate",
                    Instructions = "Bar on upper back, feet shoulder-width, lower hips back and down, drive through heels to stand"
                },
                new ExerciseTemplate
                {
                    Name = "Lunges",
                    Description = "Unilateral leg exercise",
                    Category = "Strength",
                    MuscleGroup = "Legs",
                    Equipment = "Bodyweight",
                    Difficulty = "Beginner",
                    Instructions = "Step forward, lower back knee toward ground, push back to starting position"
                },
                new ExerciseTemplate
                {
                    Name = "Leg Press",
                    Description = "Machine-based leg exercise",
                    Category = "Strength",
                    MuscleGroup = "Legs",
                    Equipment = "Machine",
                    Difficulty = "Beginner",
                    Instructions = "Sit in machine, feet on platform, push platform away, return with control"
                },

                // Shoulder Exercises
                new ExerciseTemplate
                {
                    Name = "Overhead Press",
                    Description = "Compound exercise for shoulders and triceps",
                    Category = "Strength",
                    MuscleGroup = "Shoulders",
                    Equipment = "Barbell",
                    Difficulty = "Intermediate",
                    Instructions = "Bar at shoulder height, press overhead, lower with control"
                },
                new ExerciseTemplate
                {
                    Name = "Lateral Raises",
                    Description = "Isolation exercise for side delts",
                    Category = "Strength",
                    MuscleGroup = "Shoulders",
                    Equipment = "Dumbbell",
                    Difficulty = "Beginner",
                    Instructions = "Hold dumbbells at sides, raise arms out to shoulder height, lower with control"
                },

                // Arms
                new ExerciseTemplate
                {
                    Name = "Bicep Curls",
                    Description = "Isolation exercise for biceps",
                    Category = "Strength",
                    MuscleGroup = "Arms",
                    Equipment = "Dumbbell",
                    Difficulty = "Beginner",
                    Instructions = "Hold dumbbells, curl up toward shoulders, lower with control"
                },
                new ExerciseTemplate
                {
                    Name = "Tricep Dips",
                    Description = "Bodyweight exercise for triceps",
                    Category = "Strength",
                    MuscleGroup = "Arms",
                    Equipment = "Parallel Bars",
                    Difficulty = "Intermediate",
                    Instructions = "Support body on bars, lower by bending elbows, push back up"
                },

                // Core
                new ExerciseTemplate
                {
                    Name = "Plank",
                    Description = "Isometric core exercise",
                    Category = "Core",
                    MuscleGroup = "Abs",
                    Equipment = "Bodyweight",
                    Difficulty = "Beginner",
                    Instructions = "Hold body in straight line from head to heels, supported on forearms and toes"
                },
                new ExerciseTemplate
                {
                    Name = "Crunches",
                    Description = "Basic ab exercise",
                    Category = "Core",
                    MuscleGroup = "Abs",
                    Equipment = "Bodyweight",
                    Difficulty = "Beginner",
                    Instructions = "Lie on back, knees bent, lift shoulders off ground, lower with control"
                },

                // Cardio
                new ExerciseTemplate
                {
                    Name = "Running",
                    Description = "Cardio exercise",
                    Category = "Cardio",
                    MuscleGroup = "Full Body",
                    Equipment = "None",
                    Difficulty = "Beginner",
                    Instructions = "Run at steady pace for desired duration or distance"
                },
                new ExerciseTemplate
                {
                    Name = "Jump Rope",
                    Description = "High-intensity cardio",
                    Category = "Cardio",
                    MuscleGroup = "Full Body",
                    Equipment = "Jump Rope",
                    Difficulty = "Beginner",
                    Instructions = "Jump over rope as it passes under feet, maintain steady rhythm"
                },
                new ExerciseTemplate
                {
                    Name = "Burpees",
                    Description = "Full-body conditioning exercise",
                    Category = "Cardio",
                    MuscleGroup = "Full Body",
                    Equipment = "Bodyweight",
                    Difficulty = "Intermediate",
                    Instructions = "Squat down, kick feet back to plank, do push-up, jump feet forward, jump up"
                }
            };

            context.ExerciseTemplates.AddRange(templates);
            context.SaveChanges();
        }
    }
}
