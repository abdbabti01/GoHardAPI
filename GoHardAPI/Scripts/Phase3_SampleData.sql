-- Phase 3: Insert Sample Data for Testing
USE TrainingAppDb;
GO

-- Get the first user ID (assuming user with ID 1 exists)
DECLARE @UserId INT = 1;

-- Insert Sample Workout Templates
PRINT 'Inserting sample workout templates...';

INSERT INTO WorkoutTemplates (Name, Description, ExercisesJson, RecurrencePattern, DaysOfWeek, EstimatedDuration, Category, IsActive, CreatedByUserId, IsCustom, CreatedAt)
VALUES
('Morning Push Workout', 'Upper body push exercises for morning',
'[{"name":"Push-ups","sets":3,"reps":15},{"name":"Dips","sets":3,"reps":12},{"name":"Overhead Press","sets":3,"reps":10}]',
'weekly', '1,3,5', 30, 'Strength', 1, @UserId, 1, GETUTCDATE()),

('Pull Day', 'Back and biceps focused workout',
'[{"name":"Pull-ups","sets":4,"reps":8},{"name":"Rows","sets":4,"reps":10},{"name":"Bicep Curls","sets":3,"reps":12}]',
'weekly', '2,4', 45, 'Strength', 1, @UserId, 1, GETUTCDATE()),

('Daily Cardio Blast', 'Quick cardio routine',
'[{"name":"Jump Rope","duration":300},{"name":"Burpees","sets":3,"reps":15},{"name":"Mountain Climbers","sets":3,"reps":20}]',
'daily', NULL, 20, 'Cardio', 1, @UserId, 1, GETUTCDATE()),

('Leg Day Destroyer', 'Intense lower body workout',
'[{"name":"Squats","sets":5,"reps":8},{"name":"Lunges","sets":4,"reps":12},{"name":"Leg Press","sets":4,"reps":10}]',
'weekly', '1,5', 60, 'Strength', 1, @UserId, 1, GETUTCDATE());

PRINT '✅ Created 4 workout templates';

-- Insert Sample Shared Workouts
PRINT 'Inserting sample shared workouts...';

INSERT INTO SharedWorkouts (OriginalId, Type, SharedByUserId, WorkoutName, Description, ExercisesJson, Duration, Category, Difficulty, LikeCount, SaveCount, SharedAt)
VALUES
(1, 'session', @UserId, 'Beast Mode Chest Day', 'Heavy chest workout for maximum gains',
'[{"name":"Bench Press","sets":5,"reps":5,"weight":100},{"name":"Incline Dumbbell Press","sets":4,"reps":8,"weight":35},{"name":"Cable Flyes","sets":3,"reps":12,"weight":20}]',
60, 'Strength', 'Advanced', 15, 8, GETUTCDATE()),

(2, 'session', @UserId, 'HIIT Fat Burner', 'High intensity interval training for fat loss',
'[{"name":"Burpees","sets":5,"reps":20},{"name":"Box Jumps","sets":4,"reps":15},{"name":"Kettlebell Swings","sets":4,"reps":20}]',
30, 'HIIT', 'Intermediate', 32, 18, DATEADD(day, -1, GETUTCDATE())),

(3, 'template', @UserId, 'Beginner Full Body', 'Perfect starter workout for beginners',
'[{"name":"Bodyweight Squats","sets":3,"reps":15},{"name":"Push-ups","sets":3,"reps":10},{"name":"Plank","duration":60}]',
25, 'Strength', 'Beginner', 45, 25, DATEADD(day, -2, GETUTCDATE())),

(4, 'session', @UserId, '5K Running Plan', 'Build up to running 5K',
'[{"name":"Warm-up Walk","duration":300},{"name":"Interval Running","duration":1200},{"name":"Cool-down Walk","duration":300}]',
40, 'Cardio', 'Beginner', 28, 15, DATEADD(day, -3, GETUTCDATE()));

PRINT '✅ Created 4 shared workouts';

-- Add some likes and saves for realism
PRINT 'Adding likes and saves...';

-- User 1 likes workout 2 and 3
INSERT INTO SharedWorkoutLikes (SharedWorkoutId, UserId, LikedAt)
SELECT 2, @UserId, GETUTCDATE()
UNION ALL
SELECT 3, @UserId, GETUTCDATE();

-- User 1 saves workout 3 and 4
INSERT INTO SharedWorkoutSaves (SharedWorkoutId, UserId, SavedAt)
SELECT 3, @UserId, GETUTCDATE()
UNION ALL
SELECT 4, @UserId, GETUTCDATE();

PRINT '✅ Added likes and saves';

-- Display summary
PRINT '';
PRINT '=================================================';
PRINT 'Sample data created successfully!';
PRINT '=================================================';
PRINT '';

SELECT
    'WorkoutTemplates' as TableName,
    COUNT(*) as RecordCount
FROM WorkoutTemplates
UNION ALL
SELECT 'SharedWorkouts', COUNT(*) FROM SharedWorkouts
UNION ALL
SELECT 'SharedWorkoutLikes', COUNT(*) FROM SharedWorkoutLikes
UNION ALL
SELECT 'SharedWorkoutSaves', COUNT(*) FROM SharedWorkoutSaves;

PRINT '';
PRINT 'You can now see data in the Flutter app!';
PRINT 'Refresh the Templates and Community screens.';
GO
