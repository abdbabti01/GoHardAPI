using GoHardAPI.Models;

namespace GoHardAPI.Data
{
    public static class SeedData
    {
        public static void Initialize(TrainingContext context)
        {
            var templates = GetAllTemplates();

            // Check if we already have exercise templates
            bool hasExistingTemplates = context.ExerciseTemplates.Any();

            if (hasExistingTemplates)
            {
                // Add any missing templates and update videos
                AddMissingTemplates(context, templates);
                UpdateExerciseVideos(context);
                return;
            }

            // First run - add all templates
            context.ExerciseTemplates.AddRange(templates);
            context.SaveChanges();
        }

        private static List<ExerciseTemplate> GetAllTemplates()
        {
            return new List<ExerciseTemplate>
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
                    VideoUrl = "https://www.youtube.com/watch?v=gRVjAtPip0Y",
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
                    VideoUrl = "https://www.youtube.com/watch?v=IODxDxX7oi4",
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
                    VideoUrl = "https://www.youtube.com/watch?v=eozdVDA78K0",
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
                    VideoUrl = "https://www.youtube.com/watch?v=op9kVnSso6Q",
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
                    VideoUrl = "https://www.youtube.com/watch?v=eGo4IYlbE5g",
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
                    VideoUrl = "https://www.youtube.com/watch?v=FWJR5Ve8bnQ",
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
                    VideoUrl = "https://www.youtube.com/watch?v=ultWZbUMPL8",
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
                    VideoUrl = "https://www.youtube.com/watch?v=QOVaHwm-Q6U",
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
                    VideoUrl = "https://www.youtube.com/watch?v=IZxyjW7MPJQ",
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
                    VideoUrl = "https://www.youtube.com/watch?v=2yjwXTZQDDI",
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
                    VideoUrl = "https://www.youtube.com/watch?v=3VcKaXpzqRo",
                    Instructions = "Hold dumbbells at sides, raise arms out to shoulder height, lower with control"
                },

                // Arms - Biceps
                new ExerciseTemplate
                {
                    Name = "Bicep Curls",
                    Description = "Isolation exercise for biceps",
                    Category = "Strength",
                    MuscleGroup = "Arms",
                    Equipment = "Dumbbell",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=ykJmrZ5v0Oo",
                    Instructions = "Hold dumbbells, curl up toward shoulders, lower with control"
                },
                new ExerciseTemplate
                {
                    Name = "Hammer Curls",
                    Description = "Targets biceps and brachialis with neutral grip",
                    Category = "Strength",
                    MuscleGroup = "Arms",
                    Equipment = "Dumbbell",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=zC3nLlEvin4",
                    Instructions = "Hold dumbbells with palms facing each other, curl up keeping wrists neutral, lower with control"
                },
                new ExerciseTemplate
                {
                    Name = "Preacher Curls",
                    Description = "Isolation bicep exercise using preacher bench",
                    Category = "Strength",
                    MuscleGroup = "Arms",
                    Equipment = "Barbell",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=fIWP-FRFNU0",
                    Instructions = "Rest arms on preacher bench pad, curl bar up toward shoulders, lower slowly"
                },
                new ExerciseTemplate
                {
                    Name = "Concentration Curls",
                    Description = "Single-arm bicep isolation for peak contraction",
                    Category = "Strength",
                    MuscleGroup = "Arms",
                    Equipment = "Dumbbell",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=0AUGkch3tzc",
                    Instructions = "Sit with elbow braced against inner thigh, curl dumbbell up, squeeze at top"
                },
                new ExerciseTemplate
                {
                    Name = "Barbell Curls",
                    Description = "Classic bicep builder with barbell",
                    Category = "Strength",
                    MuscleGroup = "Arms",
                    Equipment = "Barbell",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=LY1V6UbRHFM",
                    Instructions = "Stand with barbell, curl up toward shoulders keeping elbows stationary, lower with control"
                },
                new ExerciseTemplate
                {
                    Name = "Incline Dumbbell Curls",
                    Description = "Bicep curls on incline bench for full stretch",
                    Category = "Strength",
                    MuscleGroup = "Arms",
                    Equipment = "Dumbbell",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=soxrZlIl35U",
                    Instructions = "Lie back on incline bench, let arms hang, curl dumbbells up, lower slowly"
                },
                new ExerciseTemplate
                {
                    Name = "Cable Curls",
                    Description = "Constant tension bicep exercise using cable machine",
                    Category = "Strength",
                    MuscleGroup = "Arms",
                    Equipment = "Cable Machine",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=NFzTWp2qpiE",
                    Instructions = "Stand facing cable machine, curl handle up toward shoulders, lower with control"
                },
                new ExerciseTemplate
                {
                    Name = "EZ Bar Curls",
                    Description = "Bicep curls with EZ bar for wrist comfort",
                    Category = "Strength",
                    MuscleGroup = "Arms",
                    Equipment = "EZ Bar",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=zG2xJ0Q5QtI",
                    Instructions = "Grip EZ bar at angled portions, curl up toward shoulders, lower with control"
                },

                // Arms - Triceps
                new ExerciseTemplate
                {
                    Name = "Tricep Dips",
                    Description = "Bodyweight exercise for triceps",
                    Category = "Strength",
                    MuscleGroup = "Arms",
                    Equipment = "Parallel Bars",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=2z8JmcrW-As",
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
                    VideoUrl = "https://www.youtube.com/watch?v=ASdvN_XEl_c",
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
                    VideoUrl = "https://www.youtube.com/watch?v=Xyd_fa5zoEU",
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
                    VideoUrl = "https://www.youtube.com/watch?v=brFHyOtTwH4",
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
                    VideoUrl = "https://www.youtube.com/watch?v=FJmRQ5iTXKE",
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
                    VideoUrl = "https://www.youtube.com/watch?v=auBLPXO8Fww",
                    Instructions = "Squat down, kick feet back to plank, do push-up, jump feet forward, jump up"
                },

                // Additional Chest Exercises
                new ExerciseTemplate
                {
                    Name = "Incline Dumbbell Press",
                    Description = "Upper chest builder",
                    Category = "Strength",
                    MuscleGroup = "Chest",
                    Equipment = "Dumbbell",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=8iPEnn-ltC8",
                    Instructions = "Lie on incline bench, press dumbbells up from chest level, lower with control"
                },
                new ExerciseTemplate
                {
                    Name = "Cable Crossovers",
                    Description = "Chest isolation with cables",
                    Category = "Strength",
                    MuscleGroup = "Chest",
                    Equipment = "Cable",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=f2W1xG-dATo",
                    Instructions = "Stand between cable machines, bring handles together in front of chest"
                },
                new ExerciseTemplate
                {
                    Name = "Decline Push-ups",
                    Description = "Advanced push-up variation",
                    Category = "Strength",
                    MuscleGroup = "Chest",
                    Equipment = "Bodyweight",
                    Difficulty = "Advanced",
                    VideoUrl = "https://www.youtube.com/watch?v=56PKAzP3pHE",
                    Instructions = "Feet elevated on bench, perform push-ups with increased difficulty"
                },

                // Additional Back Exercises
                new ExerciseTemplate
                {
                    Name = "Lat Pulldown",
                    Description = "Machine-based back exercise",
                    Category = "Strength",
                    MuscleGroup = "Back",
                    Equipment = "Machine",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=CAwf7n6Luuc",
                    Instructions = "Pull bar down to chest level, squeeze shoulder blades together"
                },
                new ExerciseTemplate
                {
                    Name = "Seated Cable Row",
                    Description = "Mid-back thickness builder",
                    Category = "Strength",
                    MuscleGroup = "Back",
                    Equipment = "Cable",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=m-w6d9hqcAk",
                    Instructions = "Pull cable handle to torso, squeeze back muscles, return with control"
                },
                new ExerciseTemplate
                {
                    Name = "T-Bar Row",
                    Description = "Thick back developer",
                    Category = "Strength",
                    MuscleGroup = "Back",
                    Equipment = "Barbell",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=8w6vYGbh0OI",
                    Instructions = "Straddle barbell, pull to chest with neutral grip"
                },
                new ExerciseTemplate
                {
                    Name = "Face Pulls",
                    Description = "Rear delt and upper back",
                    Category = "Strength",
                    MuscleGroup = "Back",
                    Equipment = "Cable",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=eIJS4mrcWqE",
                    Instructions = "Pull rope attachment to face level, squeeze rear delts"
                },

                // Additional Leg Exercises
                new ExerciseTemplate
                {
                    Name = "Romanian Deadlift",
                    Description = "Hamstring and glute builder",
                    Category = "Strength",
                    MuscleGroup = "Legs",
                    Equipment = "Barbell",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=3kDVVwc0YoI",
                    Instructions = "Lower bar by hinging at hips, feel stretch in hamstrings, return to standing"
                },
                new ExerciseTemplate
                {
                    Name = "Bulgarian Split Squat",
                    Description = "Single-leg quad and glute developer",
                    Category = "Strength",
                    MuscleGroup = "Legs",
                    Equipment = "Dumbbell",
                    Difficulty = "Advanced",
                    VideoUrl = "https://www.youtube.com/watch?v=2avvEHx3E-U",
                    Instructions = "Rear foot elevated, lower front leg until deep squat, drive back up"
                },
                new ExerciseTemplate
                {
                    Name = "Leg Curl",
                    Description = "Hamstring isolation",
                    Category = "Strength",
                    MuscleGroup = "Legs",
                    Equipment = "Machine",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=Cy-6vKzZCJE",
                    Instructions = "Curl legs up toward glutes, lower with control"
                },
                new ExerciseTemplate
                {
                    Name = "Leg Extension",
                    Description = "Quad isolation",
                    Category = "Strength",
                    MuscleGroup = "Legs",
                    Equipment = "Machine",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=YyvSfVjQeL0",
                    Instructions = "Extend legs until straight, lower with control"
                },
                new ExerciseTemplate
                {
                    Name = "Calf Raises",
                    Description = "Calf muscle builder",
                    Category = "Strength",
                    MuscleGroup = "Legs",
                    Equipment = "Machine",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=AdwxjPu2YFI",
                    Instructions = "Rise up on toes, squeeze calves at top, lower slowly"
                },
                new ExerciseTemplate
                {
                    Name = "Goblet Squat",
                    Description = "Front-loaded squat variation",
                    Category = "Strength",
                    MuscleGroup = "Legs",
                    Equipment = "Dumbbell",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=VxY2ZvyeP0s",
                    Instructions = "Hold dumbbell at chest, squat deep, maintain upright torso"
                },

                // Additional Shoulder Exercises
                new ExerciseTemplate
                {
                    Name = "Arnold Press",
                    Description = "Complete shoulder developer",
                    Category = "Strength",
                    MuscleGroup = "Shoulders",
                    Equipment = "Dumbbell",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=6Z15_WdXmj8",
                    Instructions = "Start with palms facing you, press and rotate dumbbells overhead"
                },
                new ExerciseTemplate
                {
                    Name = "Front Raises",
                    Description = "Front delt isolation",
                    Category = "Strength",
                    MuscleGroup = "Shoulders",
                    Equipment = "Dumbbell",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=sAjf4GncMg0",
                    Instructions = "Raise dumbbells to shoulder height in front of body"
                },
                new ExerciseTemplate
                {
                    Name = "Rear Delt Flyes",
                    Description = "Rear shoulder isolation",
                    Category = "Strength",
                    MuscleGroup = "Shoulders",
                    Equipment = "Dumbbell",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=dD-Sf7PZZ5A",
                    Instructions = "Bent over, raise dumbbells out to sides, squeeze rear delts"
                },
                new ExerciseTemplate
                {
                    Name = "Upright Row",
                    Description = "Shoulder and trap developer",
                    Category = "Strength",
                    MuscleGroup = "Shoulders",
                    Equipment = "Barbell",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=DcDfXaQrdm0",
                    Instructions = "Pull barbell up to chin level, elbows high"
                },

                // Additional Arm Exercises
                new ExerciseTemplate
                {
                    Name = "Hammer Curls",
                    Description = "Bicep and forearm builder",
                    Category = "Strength",
                    MuscleGroup = "Arms",
                    Equipment = "Dumbbell",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=CFVQ8Foize4",
                    Instructions = "Curl dumbbells with neutral grip, keep elbows stable"
                },
                new ExerciseTemplate
                {
                    Name = "Preacher Curls",
                    Description = "Strict bicep isolation",
                    Category = "Strength",
                    MuscleGroup = "Arms",
                    Equipment = "Barbell",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=fCr5pMwrYmU",
                    Instructions = "Arms on preacher bench, curl bar up, squeeze biceps"
                },
                new ExerciseTemplate
                {
                    Name = "Tricep Pushdown",
                    Description = "Tricep isolation with cable",
                    Category = "Strength",
                    MuscleGroup = "Arms",
                    Equipment = "Cable",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=omHg8vAH0ic",
                    Instructions = "Push cable bar down until arms fully extended"
                },
                new ExerciseTemplate
                {
                    Name = "Skull Crushers",
                    Description = "Lying tricep extension",
                    Category = "Strength",
                    MuscleGroup = "Arms",
                    Equipment = "Barbell",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=_l4DQKzwXJw",
                    Instructions = "Lying on bench, lower bar to forehead, extend arms back up"
                },
                new ExerciseTemplate
                {
                    Name = "Close-Grip Bench Press",
                    Description = "Compound tricep builder",
                    Category = "Strength",
                    MuscleGroup = "Arms",
                    Equipment = "Barbell",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=tEOM1HoYESw",
                    Instructions = "Narrow grip on bar, press focusing on triceps"
                },
                new ExerciseTemplate
                {
                    Name = "Concentration Curls",
                    Description = "Isolated bicep peak builder",
                    Category = "Strength",
                    MuscleGroup = "Arms",
                    Equipment = "Dumbbell",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=i3gePZ5ufTw",
                    Instructions = "Seated, curl dumbbell with full focus on bicep contraction"
                },

                // Additional Core Exercises
                new ExerciseTemplate
                {
                    Name = "Russian Twists",
                    Description = "Oblique and core rotation",
                    Category = "Core",
                    MuscleGroup = "Abs",
                    Equipment = "Bodyweight",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=Y2akXNeDPaE",
                    Instructions = "Seated, lean back, rotate torso side to side"
                },
                new ExerciseTemplate
                {
                    Name = "Hanging Leg Raises",
                    Description = "Advanced lower ab exercise",
                    Category = "Core",
                    MuscleGroup = "Abs",
                    Equipment = "Pull-up Bar",
                    Difficulty = "Advanced",
                    VideoUrl = "https://www.youtube.com/watch?v=VR_hWJ2rCIU",
                    Instructions = "Hang from bar, raise legs to 90 degrees, lower with control"
                },
                new ExerciseTemplate
                {
                    Name = "Cable Crunches",
                    Description = "Weighted ab exercise",
                    Category = "Core",
                    MuscleGroup = "Abs",
                    Equipment = "Cable",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=rXIqwU4aZxI",
                    Instructions = "Kneel at cable machine, crunch down bringing elbows to knees"
                },
                new ExerciseTemplate
                {
                    Name = "Side Plank",
                    Description = "Oblique isometric hold",
                    Category = "Core",
                    MuscleGroup = "Abs",
                    Equipment = "Bodyweight",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=okNdaE9V3SE",
                    Instructions = "Hold body sideways on forearm, maintain straight line"
                },
                new ExerciseTemplate
                {
                    Name = "Mountain Climbers",
                    Description = "Dynamic core and cardio",
                    Category = "Core",
                    MuscleGroup = "Abs",
                    Equipment = "Bodyweight",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=nmwgirgXLYM",
                    Instructions = "Plank position, alternate bringing knees to chest rapidly"
                },
                new ExerciseTemplate
                {
                    Name = "Ab Wheel Rollout",
                    Description = "Advanced core stability",
                    Category = "Core",
                    MuscleGroup = "Abs",
                    Equipment = "Ab Wheel",
                    Difficulty = "Advanced",
                    VideoUrl = "https://www.youtube.com/watch?v=wcUvzXpIV3I",
                    Instructions = "Roll wheel forward extending body, pull back to start"
                },

                // Additional Cardio Exercises
                new ExerciseTemplate
                {
                    Name = "Cycling",
                    Description = "Low-impact cardio",
                    Category = "Cardio",
                    MuscleGroup = "Full Body",
                    Equipment = "Bike",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=GKhT2VGXe5A",
                    Instructions = "Maintain steady pace on stationary or regular bike"
                },
                new ExerciseTemplate
                {
                    Name = "Rowing Machine",
                    Description = "Full-body cardio and strength",
                    Category = "Cardio",
                    MuscleGroup = "Full Body",
                    Equipment = "Machine",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=qyNjPBqiKfg",
                    Instructions = "Pull handle to chest, push with legs, repeat in rhythm"
                },
                new ExerciseTemplate
                {
                    Name = "Battle Ropes",
                    Description = "High-intensity upper body cardio",
                    Category = "Cardio",
                    MuscleGroup = "Full Body",
                    Equipment = "Battle Ropes",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=R3xyWBgkKoI",
                    Instructions = "Alternate or simultaneous waves with heavy ropes"
                },
                new ExerciseTemplate
                {
                    Name = "Box Jumps",
                    Description = "Explosive lower body power",
                    Category = "Cardio",
                    MuscleGroup = "Legs",
                    Equipment = "Box",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=Km8uZRGXVCU",
                    Instructions = "Jump onto elevated box, step down, repeat"
                },
                new ExerciseTemplate
                {
                    Name = "High Knees",
                    Description = "Running in place with high knee drive",
                    Category = "Cardio",
                    MuscleGroup = "Full Body",
                    Equipment = "None",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=uLXJL6bVCl0",
                    Instructions = "Run in place bringing knees to hip level"
                },

                // Advanced/Athletic Movements
                new ExerciseTemplate
                {
                    Name = "Muscle-ups",
                    Description = "Advanced pull-up to dip transition",
                    Category = "Strength",
                    MuscleGroup = "Full Body",
                    Equipment = "Pull-up Bar",
                    Difficulty = "Advanced",
                    VideoUrl = "https://www.youtube.com/watch?v=pT2o2hP_ZLE",
                    Instructions = "Pull up explosively, transition to dip position, press to top"
                },
                new ExerciseTemplate
                {
                    Name = "Pistol Squats",
                    Description = "Single-leg bodyweight squat",
                    Category = "Strength",
                    MuscleGroup = "Legs",
                    Equipment = "Bodyweight",
                    Difficulty = "Advanced",
                    VideoUrl = "https://www.youtube.com/watch?v=OMx1jF6dYOg",
                    Instructions = "Squat on one leg, other leg extended forward, stand back up"
                },
                new ExerciseTemplate
                {
                    Name = "Handstand Push-ups",
                    Description = "Inverted shoulder press",
                    Category = "Strength",
                    MuscleGroup = "Shoulders",
                    Equipment = "Bodyweight",
                    Difficulty = "Advanced",
                    VideoUrl = "https://www.youtube.com/watch?v=EHVrk-9kPKM",
                    Instructions = "Handstand against wall, lower head to ground, press back up"
                },
                new ExerciseTemplate
                {
                    Name = "Clean and Jerk",
                    Description = "Olympic weightlifting movement",
                    Category = "Strength",
                    MuscleGroup = "Full Body",
                    Equipment = "Barbell",
                    Difficulty = "Advanced",
                    VideoUrl = "https://www.youtube.com/watch?v=eHnuHIOu8zU",
                    Instructions = "Pull bar to shoulders, then jerk overhead in one fluid motion"
                },

                // Additional Chest Exercises
                new ExerciseTemplate
                {
                    Name = "Dumbbell Bench Press",
                    Description = "Dumbbell variation of bench press for better range of motion",
                    Category = "Strength",
                    MuscleGroup = "Chest",
                    Equipment = "Dumbbell",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=VmB1G1K7v94",
                    Instructions = "Lie on bench, press dumbbells up from chest level, lower with control"
                },
                new ExerciseTemplate
                {
                    Name = "Chest Dips",
                    Description = "Bodyweight chest builder with forward lean",
                    Category = "Strength",
                    MuscleGroup = "Chest",
                    Equipment = "Parallel Bars",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=dX_nSOOJIsE",
                    Instructions = "Lean forward on parallel bars, lower body, push back up focusing on chest"
                },
                new ExerciseTemplate
                {
                    Name = "Landmine Press",
                    Description = "Angled pressing movement for upper chest",
                    Category = "Strength",
                    MuscleGroup = "Chest",
                    Equipment = "Barbell",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=Oy5LxkPJVdc",
                    Instructions = "Press barbell from chest at an angle using landmine attachment"
                },
                new ExerciseTemplate
                {
                    Name = "Pec Deck",
                    Description = "Machine isolation for chest",
                    Category = "Strength",
                    MuscleGroup = "Chest",
                    Equipment = "Machine",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=Z57CtFmRMxA",
                    Instructions = "Sit in machine, bring pads together in front of chest, squeeze pecs"
                },

                // Additional Back Exercises
                new ExerciseTemplate
                {
                    Name = "Single Arm Dumbbell Row",
                    Description = "Unilateral back exercise for muscle balance",
                    Category = "Strength",
                    MuscleGroup = "Back",
                    Equipment = "Dumbbell",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=pYcpY20QaE8",
                    Instructions = "One hand on bench, row dumbbell to hip, squeeze back"
                },
                new ExerciseTemplate
                {
                    Name = "Chin-ups",
                    Description = "Underhand grip pull-up for back and biceps",
                    Category = "Strength",
                    MuscleGroup = "Back",
                    Equipment = "Pull-up Bar",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=brhRXlOhsAM",
                    Instructions = "Hang with underhand grip, pull chin over bar, lower with control"
                },
                new ExerciseTemplate
                {
                    Name = "Straight Arm Pulldown",
                    Description = "Lat isolation exercise",
                    Category = "Strength",
                    MuscleGroup = "Back",
                    Equipment = "Cable",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=lueEJGjTuPQ",
                    Instructions = "Keep arms straight, pull bar down to thighs, squeeze lats"
                },
                new ExerciseTemplate
                {
                    Name = "Meadows Row",
                    Description = "Landmine row variation for back thickness",
                    Category = "Strength",
                    MuscleGroup = "Back",
                    Equipment = "Barbell",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=_EHH9G0mWQs",
                    Instructions = "Staggered stance, row landmine to hip with one arm"
                },
                new ExerciseTemplate
                {
                    Name = "Inverted Row",
                    Description = "Bodyweight horizontal pulling movement",
                    Category = "Strength",
                    MuscleGroup = "Back",
                    Equipment = "Barbell",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=dYF-c8Gf5eg",
                    Instructions = "Hang under bar, pull chest to bar, lower with control"
                },

                // Traps Exercises
                new ExerciseTemplate
                {
                    Name = "Barbell Shrugs",
                    Description = "Primary trap builder",
                    Category = "Strength",
                    MuscleGroup = "Traps",
                    Equipment = "Barbell",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=NAqCVe2mwzM",
                    Instructions = "Hold barbell at thighs, shrug shoulders up toward ears, hold and lower"
                },
                new ExerciseTemplate
                {
                    Name = "Dumbbell Shrugs",
                    Description = "Dumbbell variation for trap development",
                    Category = "Strength",
                    MuscleGroup = "Traps",
                    Equipment = "Dumbbell",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=cJRVVxmytaM",
                    Instructions = "Hold dumbbells at sides, shrug shoulders up, squeeze at top"
                },
                new ExerciseTemplate
                {
                    Name = "Farmer's Walk",
                    Description = "Loaded carry for traps, grip, and core",
                    Category = "Strength",
                    MuscleGroup = "Traps",
                    Equipment = "Dumbbell",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=Fkzk_RqlYig",
                    Instructions = "Hold heavy weights at sides, walk with upright posture"
                },
                new ExerciseTemplate
                {
                    Name = "Rack Pulls",
                    Description = "Partial deadlift for upper back and traps",
                    Category = "Strength",
                    MuscleGroup = "Traps",
                    Equipment = "Barbell",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=bTp2Ny7Y6g8",
                    Instructions = "Pull barbell from rack at knee height, lockout and squeeze traps"
                },

                // Forearms Exercises
                new ExerciseTemplate
                {
                    Name = "Wrist Curls",
                    Description = "Forearm flexor builder",
                    Category = "Strength",
                    MuscleGroup = "Forearms",
                    Equipment = "Barbell",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=naDgR4sLTbg",
                    Instructions = "Rest forearms on bench, curl wrists up, lower slowly"
                },
                new ExerciseTemplate
                {
                    Name = "Reverse Wrist Curls",
                    Description = "Forearm extensor builder",
                    Category = "Strength",
                    MuscleGroup = "Forearms",
                    Equipment = "Barbell",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=FW8qcRS9weA",
                    Instructions = "Rest forearms on bench palms down, extend wrists up"
                },
                new ExerciseTemplate
                {
                    Name = "Dead Hang",
                    Description = "Grip strength and shoulder mobility",
                    Category = "Strength",
                    MuscleGroup = "Forearms",
                    Equipment = "Pull-up Bar",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=mFVDlBcSITI",
                    Instructions = "Hang from bar with straight arms, hold as long as possible"
                },
                new ExerciseTemplate
                {
                    Name = "Plate Pinch",
                    Description = "Grip strength exercise",
                    Category = "Strength",
                    MuscleGroup = "Forearms",
                    Equipment = "Plate",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=FWxG6LxPxjU",
                    Instructions = "Pinch two plates together with fingers, hold for time"
                },

                // Glutes Exercises
                new ExerciseTemplate
                {
                    Name = "Hip Thrust",
                    Description = "Primary glute builder",
                    Category = "Strength",
                    MuscleGroup = "Glutes",
                    Equipment = "Barbell",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=SEdqd1n0cvg",
                    Instructions = "Back against bench, thrust hips up with barbell on lap, squeeze glutes"
                },
                new ExerciseTemplate
                {
                    Name = "Glute Bridge",
                    Description = "Bodyweight glute activation",
                    Category = "Strength",
                    MuscleGroup = "Glutes",
                    Equipment = "Bodyweight",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=OUgsJ8-Vi0E",
                    Instructions = "Lie on back, feet flat, lift hips by squeezing glutes"
                },
                new ExerciseTemplate
                {
                    Name = "Cable Pull Through",
                    Description = "Hip hinge for glutes and hamstrings",
                    Category = "Strength",
                    MuscleGroup = "Glutes",
                    Equipment = "Cable",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=-aBvQxCs8BY",
                    Instructions = "Face away from cable, hinge at hips, drive forward squeezing glutes"
                },
                new ExerciseTemplate
                {
                    Name = "Kickbacks",
                    Description = "Glute isolation exercise",
                    Category = "Strength",
                    MuscleGroup = "Glutes",
                    Equipment = "Cable",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=xVbDkloLFlA",
                    Instructions = "Kick leg back against cable resistance, squeeze glute at top"
                },
                new ExerciseTemplate
                {
                    Name = "Sumo Deadlift",
                    Description = "Wide stance deadlift for glutes and inner thighs",
                    Category = "Strength",
                    MuscleGroup = "Glutes",
                    Equipment = "Barbell",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=dVPi4-xOrSs",
                    Instructions = "Wide stance, grip inside legs, lift by driving hips forward"
                },

                // Lower Back Exercises
                new ExerciseTemplate
                {
                    Name = "Back Extensions",
                    Description = "Lower back strengthening",
                    Category = "Strength",
                    MuscleGroup = "Lower Back",
                    Equipment = "Machine",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=ph3pddpKzzw",
                    Instructions = "Lock legs in machine, hinge at hips, extend back up"
                },
                new ExerciseTemplate
                {
                    Name = "Good Mornings",
                    Description = "Hip hinge for posterior chain",
                    Category = "Strength",
                    MuscleGroup = "Lower Back",
                    Equipment = "Barbell",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=YA-h3n9L4YU",
                    Instructions = "Bar on back, hinge at hips keeping back straight, return upright"
                },
                new ExerciseTemplate
                {
                    Name = "Superman",
                    Description = "Bodyweight lower back exercise",
                    Category = "Strength",
                    MuscleGroup = "Lower Back",
                    Equipment = "Bodyweight",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=z6PJMT2y8GQ",
                    Instructions = "Lie face down, lift arms and legs off ground, hold and lower"
                },
                new ExerciseTemplate
                {
                    Name = "Bird Dog",
                    Description = "Core stability and lower back exercise",
                    Category = "Core",
                    MuscleGroup = "Lower Back",
                    Equipment = "Bodyweight",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=wiFNA3sqjCA",
                    Instructions = "On hands and knees, extend opposite arm and leg, alternate"
                },
                new ExerciseTemplate
                {
                    Name = "Reverse Hypers",
                    Description = "Lower back and glute exercise",
                    Category = "Strength",
                    MuscleGroup = "Lower Back",
                    Equipment = "Machine",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=3d9_W--eUcI",
                    Instructions = "Lie face down on machine, lift legs up using lower back and glutes"
                },

                // Additional Shoulder Exercises
                new ExerciseTemplate
                {
                    Name = "Dumbbell Shoulder Press",
                    Description = "Seated or standing shoulder press with dumbbells",
                    Category = "Strength",
                    MuscleGroup = "Shoulders",
                    Equipment = "Dumbbell",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=qEwKCR5JCog",
                    Instructions = "Press dumbbells overhead from shoulder height, lower with control"
                },
                new ExerciseTemplate
                {
                    Name = "Cable Lateral Raises",
                    Description = "Constant tension lateral raise",
                    Category = "Strength",
                    MuscleGroup = "Shoulders",
                    Equipment = "Cable",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=_bTHfk1qNjY",
                    Instructions = "Raise arm out to side against cable resistance"
                },
                new ExerciseTemplate
                {
                    Name = "Push Press",
                    Description = "Explosive overhead press with leg drive",
                    Category = "Strength",
                    MuscleGroup = "Shoulders",
                    Equipment = "Barbell",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=iaBVSJm78ko",
                    Instructions = "Dip knees slightly, drive bar overhead using leg momentum"
                },
                new ExerciseTemplate
                {
                    Name = "Lu Raises",
                    Description = "Front raise to lateral raise combination",
                    Category = "Strength",
                    MuscleGroup = "Shoulders",
                    Equipment = "Dumbbell",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=XnpEW0uNwi4",
                    Instructions = "Raise dumbbells to front, then out to sides, lower slowly"
                },

                // Additional Arms Exercises
                new ExerciseTemplate
                {
                    Name = "EZ Bar Curl",
                    Description = "Comfortable grip bicep curl",
                    Category = "Strength",
                    MuscleGroup = "Arms",
                    Equipment = "EZ Bar",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=zG2xJ0Q5QtI",
                    Instructions = "Curl EZ bar up, keep elbows stationary, lower with control"
                },
                new ExerciseTemplate
                {
                    Name = "Cable Curls",
                    Description = "Constant tension bicep exercise",
                    Category = "Strength",
                    MuscleGroup = "Arms",
                    Equipment = "Cable",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=NFzTWp2qpiE",
                    Instructions = "Curl cable bar toward shoulders, squeeze biceps"
                },
                new ExerciseTemplate
                {
                    Name = "Overhead Tricep Extension",
                    Description = "Tricep long head emphasis",
                    Category = "Strength",
                    MuscleGroup = "Arms",
                    Equipment = "Dumbbell",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=YbX7Wd8jQ-Q",
                    Instructions = "Hold dumbbell overhead, lower behind head, extend back up"
                },
                new ExerciseTemplate
                {
                    Name = "Diamond Push-ups",
                    Description = "Tricep-focused push-up variation",
                    Category = "Strength",
                    MuscleGroup = "Arms",
                    Equipment = "Bodyweight",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=J0DnG1_S92I",
                    Instructions = "Hands close together forming diamond shape, perform push-ups"
                },
                new ExerciseTemplate
                {
                    Name = "Incline Dumbbell Curls",
                    Description = "Stretched position bicep curls",
                    Category = "Strength",
                    MuscleGroup = "Arms",
                    Equipment = "Dumbbell",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=soxrZlIl35U",
                    Instructions = "Lie on incline bench, curl dumbbells with arms hanging"
                },

                // Additional Leg Exercises
                new ExerciseTemplate
                {
                    Name = "Hack Squat",
                    Description = "Machine squat variation",
                    Category = "Strength",
                    MuscleGroup = "Legs",
                    Equipment = "Machine",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=0tn5K9NlCfo",
                    Instructions = "Position in hack squat machine, squat down and press up"
                },
                new ExerciseTemplate
                {
                    Name = "Front Squat",
                    Description = "Quad-dominant squat variation",
                    Category = "Strength",
                    MuscleGroup = "Legs",
                    Equipment = "Barbell",
                    Difficulty = "Advanced",
                    VideoUrl = "https://www.youtube.com/watch?v=VfBOBhwXbro",
                    Instructions = "Bar racked on front delts, squat keeping torso upright"
                },
                new ExerciseTemplate
                {
                    Name = "Step-ups",
                    Description = "Unilateral leg exercise",
                    Category = "Strength",
                    MuscleGroup = "Legs",
                    Equipment = "Dumbbell",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=WCFCdxzFBa4",
                    Instructions = "Step up onto box driving through front leg, step down"
                },
                new ExerciseTemplate
                {
                    Name = "Sissy Squat",
                    Description = "Quad isolation exercise",
                    Category = "Strength",
                    MuscleGroup = "Legs",
                    Equipment = "Bodyweight",
                    Difficulty = "Advanced",
                    VideoUrl = "https://www.youtube.com/watch?v=LXtOlgfpEws",
                    Instructions = "Lean back, bend knees while keeping hips extended, return upright"
                },
                new ExerciseTemplate
                {
                    Name = "Nordic Curls",
                    Description = "Eccentric hamstring exercise",
                    Category = "Strength",
                    MuscleGroup = "Legs",
                    Equipment = "Bodyweight",
                    Difficulty = "Advanced",
                    VideoUrl = "https://www.youtube.com/watch?v=FLAdN8Gkw9o",
                    Instructions = "Kneel, lower body forward under control using hamstrings"
                },
                new ExerciseTemplate
                {
                    Name = "Seated Calf Raises",
                    Description = "Soleus-focused calf exercise",
                    Category = "Strength",
                    MuscleGroup = "Legs",
                    Equipment = "Machine",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=Yh5TXz22VDM",
                    Instructions = "Sit in machine, raise heels by contracting calves"
                },

                // Additional Core Exercises
                new ExerciseTemplate
                {
                    Name = "Dead Bug",
                    Description = "Core stability exercise",
                    Category = "Core",
                    MuscleGroup = "Abs",
                    Equipment = "Bodyweight",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=g_BYB0R-4Ws",
                    Instructions = "Lie on back, extend opposite arm and leg while keeping core braced"
                },
                new ExerciseTemplate
                {
                    Name = "Pallof Press",
                    Description = "Anti-rotation core exercise",
                    Category = "Core",
                    MuscleGroup = "Abs",
                    Equipment = "Cable",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=AH_QZLm_0-s",
                    Instructions = "Press cable away from body, resist rotation, hold and return"
                },
                new ExerciseTemplate
                {
                    Name = "Bicycle Crunches",
                    Description = "Rotational ab exercise",
                    Category = "Core",
                    MuscleGroup = "Abs",
                    Equipment = "Bodyweight",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=9FGilxCbdz8",
                    Instructions = "Alternate bringing elbow to opposite knee in cycling motion"
                },
                new ExerciseTemplate
                {
                    Name = "Reverse Crunches",
                    Description = "Lower ab focused exercise",
                    Category = "Core",
                    MuscleGroup = "Abs",
                    Equipment = "Bodyweight",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=hyv14e2QDq0",
                    Instructions = "Lie on back, bring knees to chest by curling hips up"
                },
                new ExerciseTemplate
                {
                    Name = "Dragon Flag",
                    Description = "Advanced core strength exercise",
                    Category = "Core",
                    MuscleGroup = "Abs",
                    Equipment = "Bench",
                    Difficulty = "Advanced",
                    VideoUrl = "https://www.youtube.com/watch?v=pvz7k5gO-DE",
                    Instructions = "Hold bench behind head, lower body keeping straight, raise back up"
                },

                // Additional Cardio/Conditioning
                new ExerciseTemplate
                {
                    Name = "Sled Push",
                    Description = "Conditioning and leg drive exercise",
                    Category = "Cardio",
                    MuscleGroup = "Full Body",
                    Equipment = "Sled",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=Zc7l5-NSs2Y",
                    Instructions = "Push weighted sled across floor using legs"
                },
                new ExerciseTemplate
                {
                    Name = "Kettlebell Swings",
                    Description = "Hip hinge cardio and power",
                    Category = "Cardio",
                    MuscleGroup = "Full Body",
                    Equipment = "Kettlebell",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=YSxHifyI6s8",
                    Instructions = "Hinge at hips, swing kettlebell using hip drive"
                },
                new ExerciseTemplate
                {
                    Name = "Wall Balls",
                    Description = "Full body conditioning with medicine ball",
                    Category = "Cardio",
                    MuscleGroup = "Full Body",
                    Equipment = "Medicine Ball",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=fpUD0mcFp_0",
                    Instructions = "Squat with ball, throw against wall, catch and repeat"
                },
                new ExerciseTemplate
                {
                    Name = "Assault Bike",
                    Description = "High-intensity full body cardio",
                    Category = "Cardio",
                    MuscleGroup = "Full Body",
                    Equipment = "Bike",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=L1dwDXFr-kc",
                    Instructions = "Pedal while pushing and pulling handles for max effort"
                },
                new ExerciseTemplate
                {
                    Name = "Stair Climber",
                    Description = "Low impact cardio machine",
                    Category = "Cardio",
                    MuscleGroup = "Legs",
                    Equipment = "Machine",
                    Difficulty = "Beginner",
                    VideoUrl = "https://www.youtube.com/watch?v=FAQlNzCLF-Q",
                    Instructions = "Step continuously on rotating stairs at steady pace"
                },
                new ExerciseTemplate
                {
                    Name = "Thrusters",
                    Description = "Front squat to overhead press combination",
                    Category = "Strength",
                    MuscleGroup = "Full Body",
                    Equipment = "Barbell",
                    Difficulty = "Intermediate",
                    VideoUrl = "https://www.youtube.com/watch?v=oDdkytliOqE",
                    Instructions = "Front squat then drive bar overhead in one fluid motion"
                },
                new ExerciseTemplate
                {
                    Name = "Snatch",
                    Description = "Olympic lift from floor to overhead",
                    Category = "Strength",
                    MuscleGroup = "Full Body",
                    Equipment = "Barbell",
                    Difficulty = "Advanced",
                    VideoUrl = "https://www.youtube.com/watch?v=tuOiNeTgPOM",
                    Instructions = "Pull bar from floor to overhead in one explosive movement"
                }
            };
        }

        private static void AddMissingTemplates(TrainingContext context, List<ExerciseTemplate> templates)
        {
            var existingNames = context.ExerciseTemplates
                .Select(t => t.Name.ToLower())
                .ToHashSet();

            var missingTemplates = templates
                .Where(t => !existingNames.Contains(t.Name.ToLower()))
                .ToList();

            if (missingTemplates.Any())
            {
                context.ExerciseTemplates.AddRange(missingTemplates);
                context.SaveChanges();
                Console.WriteLine($"Added {missingTemplates.Count} missing exercise templates");
            }
        }

        private static void UpdateExerciseVideos(TrainingContext context)
        {
            // Dictionary of exercise names to video URLs
            var exerciseVideos = new Dictionary<string, string>
            {
                // Chest
                { "Bench Press", "https://www.youtube.com/watch?v=gRVjAtPip0Y" },
                { "Push-ups", "https://www.youtube.com/watch?v=IODxDxX7oi4" },
                { "Dumbbell Flyes", "https://www.youtube.com/watch?v=eozdVDA78K0" },
                { "Incline Dumbbell Press", "https://www.youtube.com/watch?v=8iPEnn-ltC8" },
                { "Cable Crossovers", "https://www.youtube.com/watch?v=f2W1xG-dATo" },
                { "Decline Push-ups", "https://www.youtube.com/watch?v=56PKAzP3pHE" },
                // Back
                { "Deadlift", "https://www.youtube.com/watch?v=op9kVnSso6Q" },
                { "Pull-ups", "https://www.youtube.com/watch?v=eGo4IYlbE5g" },
                { "Bent-Over Row", "https://www.youtube.com/watch?v=FWJR5Ve8bnQ" },
                { "Lat Pulldown", "https://www.youtube.com/watch?v=CAwf7n6Luuc" },
                { "Seated Cable Row", "https://www.youtube.com/watch?v=m-w6d9hqcAk" },
                { "T-Bar Row", "https://www.youtube.com/watch?v=8w6vYGbh0OI" },
                { "Face Pulls", "https://www.youtube.com/watch?v=eIJS4mrcWqE" },
                // Legs
                { "Squat", "https://www.youtube.com/watch?v=ultWZbUMPL8" },
                { "Lunges", "https://www.youtube.com/watch?v=QOVaHwm-Q6U" },
                { "Leg Press", "https://www.youtube.com/watch?v=IZxyjW7MPJQ" },
                { "Romanian Deadlift", "https://www.youtube.com/watch?v=3kDVVwc0YoI" },
                { "Bulgarian Split Squat", "https://www.youtube.com/watch?v=2avvEHx3E-U" },
                { "Leg Curl", "https://www.youtube.com/watch?v=Cy-6vKzZCJE" },
                { "Leg Extension", "https://www.youtube.com/watch?v=YyvSfVjQeL0" },
                { "Calf Raises", "https://www.youtube.com/watch?v=AdwxjPu2YFI" },
                { "Goblet Squat", "https://www.youtube.com/watch?v=VxY2ZvyeP0s" },
                // Shoulders
                { "Overhead Press", "https://www.youtube.com/watch?v=2yjwXTZQDDI" },
                { "Lateral Raises", "https://www.youtube.com/watch?v=3VcKaXpzqRo" },
                { "Arnold Press", "https://www.youtube.com/watch?v=6Z15_WdXmj8" },
                { "Front Raises", "https://www.youtube.com/watch?v=sAjf4GncMg0" },
                { "Rear Delt Flyes", "https://www.youtube.com/watch?v=dD-Sf7PZZ5A" },
                { "Upright Row", "https://www.youtube.com/watch?v=DcDfXaQrdm0" },
                // Arms
                { "Bicep Curls", "https://www.youtube.com/watch?v=ykJmrZ5v0Oo" },
                { "Tricep Dips", "https://www.youtube.com/watch?v=2z8JmcrW-As" },
                { "Hammer Curls", "https://www.youtube.com/watch?v=CFVQ8Foize4" },
                { "Preacher Curls", "https://www.youtube.com/watch?v=fCr5pMwrYmU" },
                { "Tricep Pushdown", "https://www.youtube.com/watch?v=omHg8vAH0ic" },
                { "Skull Crushers", "https://www.youtube.com/watch?v=_l4DQKzwXJw" },
                { "Close-Grip Bench Press", "https://www.youtube.com/watch?v=tEOM1HoYESw" },
                { "Concentration Curls", "https://www.youtube.com/watch?v=i3gePZ5ufTw" },
                // Core
                { "Plank", "https://www.youtube.com/watch?v=ASdvN_XEl_c" },
                { "Crunches", "https://www.youtube.com/watch?v=Xyd_fa5zoEU" },
                { "Russian Twists", "https://www.youtube.com/watch?v=Y2akXNeDPaE" },
                { "Hanging Leg Raises", "https://www.youtube.com/watch?v=VR_hWJ2rCIU" },
                { "Cable Crunches", "https://www.youtube.com/watch?v=rXIqwU4aZxI" },
                { "Side Plank", "https://www.youtube.com/watch?v=okNdaE9V3SE" },
                { "Mountain Climbers", "https://www.youtube.com/watch?v=nmwgirgXLYM" },
                { "Ab Wheel Rollout", "https://www.youtube.com/watch?v=wcUvzXpIV3I" },
                // Cardio
                { "Running", "https://www.youtube.com/watch?v=brFHyOtTwH4" },
                { "Jump Rope", "https://www.youtube.com/watch?v=FJmRQ5iTXKE" },
                { "Burpees", "https://www.youtube.com/watch?v=auBLPXO8Fww" },
                { "Cycling", "https://www.youtube.com/watch?v=GKhT2VGXe5A" },
                { "Rowing Machine", "https://www.youtube.com/watch?v=qyNjPBqiKfg" },
                { "Battle Ropes", "https://www.youtube.com/watch?v=R3xyWBgkKoI" },
                { "Box Jumps", "https://www.youtube.com/watch?v=Km8uZRGXVCU" },
                { "High Knees", "https://www.youtube.com/watch?v=uLXJL6bVCl0" },
                // Advanced
                { "Muscle-ups", "https://www.youtube.com/watch?v=pT2o2hP_ZLE" },
                { "Pistol Squats", "https://www.youtube.com/watch?v=OMx1jF6dYOg" },
                { "Handstand Push-ups", "https://www.youtube.com/watch?v=EHVrk-9kPKM" },
                { "Clean and Jerk", "https://www.youtube.com/watch?v=eHnuHIOu8zU" }
            };

            // Update existing exercises with video URLs
            var exercisesToUpdate = context.ExerciseTemplates
                .Where(e => e.VideoUrl == null || e.VideoUrl == "")
                .ToList();

            int updatedCount = 0;
            foreach (var exercise in exercisesToUpdate)
            {
                if (exerciseVideos.TryGetValue(exercise.Name, out string? videoUrl))
                {
                    exercise.VideoUrl = videoUrl;
                    updatedCount++;
                }
            }

            if (updatedCount > 0)
            {
                context.SaveChanges();
                Console.WriteLine($"Updated {updatedCount} exercises with video URLs");
            }
        }
    }
}
