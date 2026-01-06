USE TrainingAppDb;
GO

-- Insert sample workout templates for testing
PRINT 'Inserting sample workout templates...';

INSERT INTO WorkoutTemplates (
    CreatedByUserId, Name, Description, ExercisesJson,
    RecurrencePattern, DaysOfWeek, IntervalDays,
    EstimatedDuration, Category, IsActive, UsageCount, CreatedAt
)
VALUES
-- Daily morning routine
(1, 'Morning Strength', 'Daily strength building routine',
'[{"name":"Push-ups","sets":3,"reps":15},{"name":"Squats","sets":3,"reps":20},{"name":"Plank","duration":60}]',
'daily', NULL, NULL, 30, 'Strength', 1, 5, GETUTCDATE()),

-- Weekly chest day
(1, 'Chest Day', 'Heavy chest workout for Mondays and Thursdays',
'[{"name":"Bench Press","sets":5,"reps":5},{"name":"Incline Press","sets":4,"reps":8},{"name":"Dumbbell Flyes","sets":3,"reps":12}]',
'weekly', '1,4', NULL, 60, 'Strength', 1, 12, DATEADD(day, -7, GETUTCDATE())),

-- Weekly leg day
(1, 'Leg Day', 'Leg workout for Tuesdays and Fridays',
'[{"name":"Squats","sets":5,"reps":5},{"name":"Lunges","sets":3,"reps":12},{"name":"Leg Press","sets":4,"reps":10}]',
'weekly', '2,5', NULL, 50, 'Strength', 1, 8, DATEADD(day, -5, GETUTCDATE())),

-- Weekend cardio
(1, 'Weekend Cardio', 'Saturday and Sunday cardio sessions',
'[{"name":"Running","duration":1800},{"name":"Jump Rope","duration":300},{"name":"Burpees","sets":3,"reps":15}]',
'weekly', '6,7', NULL, 45, 'Cardio', 1, 3, DATEADD(day, -3, GETUTCDATE())),

-- Every 3 days HIIT
(1, 'HIIT Blast', 'High intensity interval training every 3 days',
'[{"name":"Burpees","sets":5,"reps":20},{"name":"Box Jumps","sets":4,"reps":15},{"name":"Mountain Climbers","sets":3,"reps":30}]',
'custom', NULL, 3, 25, 'HIIT', 1, 7, DATEADD(day, -2, GETUTCDATE())),

-- Inactive template
(1, 'Old Routine', 'Previously used routine (inactive)',
'[{"name":"Various exercises"}]',
'daily', NULL, NULL, 40, 'Strength', 0, 15, DATEADD(day, -30, GETUTCDATE()));

GO

PRINT '';
PRINT '✅ Sample templates inserted!';
PRINT '';

-- Show what was inserted
SELECT
    Id, Name, Category, RecurrencePattern,
    EstimatedDuration, IsActive, UsageCount,
    FORMAT(CreatedAt, 'yyyy-MM-dd') as CreatedDate
FROM WorkoutTemplates
ORDER BY CreatedAt DESC;

PRINT '';
SELECT 'Total templates: ' + CAST(COUNT(*) AS VARCHAR) FROM WorkoutTemplates;
GO
