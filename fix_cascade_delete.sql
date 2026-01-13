-- Check current foreign key constraints
SELECT
    tc.table_name,
    tc.constraint_name,
    tc.constraint_type,
    rc.update_rule,
    rc.delete_rule,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
JOIN information_schema.referential_constraints AS rc
    ON rc.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY'
    AND tc.table_name IN ('Sessions', 'Programs', 'ProgramWorkouts', 'Exercises', 'ExerciseSets')
ORDER BY tc.table_name, tc.constraint_name;

-- Fix Session -> Program cascade delete
ALTER TABLE "Sessions"
DROP CONSTRAINT IF EXISTS "FK_Sessions_Programs_ProgramId";

ALTER TABLE "Sessions"
ADD CONSTRAINT "FK_Sessions_Programs_ProgramId"
FOREIGN KEY ("ProgramId")
REFERENCES "Programs"("Id")
ON DELETE CASCADE;

-- Fix Session -> ProgramWorkout cascade delete
ALTER TABLE "Sessions"
DROP CONSTRAINT IF EXISTS "FK_Sessions_ProgramWorkouts_ProgramWorkoutId";

ALTER TABLE "Sessions"
ADD CONSTRAINT "FK_Sessions_ProgramWorkouts_ProgramWorkoutId"
FOREIGN KEY ("ProgramWorkoutId")
REFERENCES "ProgramWorkouts"("Id")
ON DELETE CASCADE;

-- Verify the fixes
SELECT
    tc.table_name,
    tc.constraint_name,
    rc.delete_rule,
    ccu.table_name AS foreign_table_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
JOIN information_schema.referential_constraints AS rc
    ON rc.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY'
    AND tc.table_name = 'Sessions'
    AND tc.constraint_name LIKE '%Program%'
ORDER BY tc.constraint_name;
