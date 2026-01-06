USE TrainingAppDb;
GO

DELETE FROM SharedWorkoutLikes;
DELETE FROM SharedWorkoutSaves;
DELETE FROM SharedWorkouts;
GO

INSERT INTO SharedWorkouts (OriginalId, Type, SharedByUserId, WorkoutName, Description, ExercisesJson, Duration, Category, Difficulty, LikeCount, SaveCount, CommentCount, SharedAt)
VALUES
(1, 'session', 1, 'Beast Mode Chest Day', 'Heavy chest workout for maximum gains',
'[{"name":"Bench Press","sets":5,"reps":5},{"name":"Incline Press","sets":4,"reps":8}]',
60, 'Strength', 'Advanced', 15, 8, GETUTCDATE()),

(2, 'session', 1, 'HIIT Fat Burner', 'High intensity interval training',
'[{"name":"Burpees","sets":5,"reps":20},{"name":"Box Jumps","sets":4,"reps":15}]',
30, 'HIIT', 'Intermediate', 32, 18, DATEADD(day, -1, GETUTCDATE())),

(3, 'session', 1, 'Beginner Full Body', 'Perfect starter workout for beginners',
'[{"name":"Squats","sets":3,"reps":15},{"name":"Push-ups","sets":3,"reps":10}]',
25, 'Strength', 'Beginner', 45, 25, DATEADD(day, -2, GETUTCDATE())),

(4, 'session', 1, 'Morning Cardio', '5K running plan',
'[{"name":"Warm-up","duration":300},{"name":"Run","duration":1200}]',
40, 'Cardio', 'Beginner', 28, 15, DATEADD(day, -3, GETUTCDATE()));
GO

INSERT INTO SharedWorkoutLikes (SharedWorkoutId, UserId) VALUES (2, 1), (3, 1);
INSERT INTO SharedWorkoutSaves (SharedWorkoutId, UserId) VALUES (3, 1), (4, 1);
GO

PRINT 'Sample data inserted successfully!';
SELECT 'SharedWorkouts' as TableName, COUNT(*) as Count FROM SharedWorkouts
UNION ALL SELECT 'Likes', COUNT(*) FROM SharedWorkoutLikes
UNION ALL SELECT 'Saves', COUNT(*) FROM SharedWorkoutSaves;
GO
