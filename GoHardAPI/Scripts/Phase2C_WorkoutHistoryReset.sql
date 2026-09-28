-- =====================================================================================
-- Phase 2C: workout-history reset (canonical-kg lifted weight)  -- PostgreSQL only
--
-- MANUAL OPERATION. Never run by the API, a migration, or a hosted service.
-- Read Phase2C_WorkoutHistoryReset.md (runbook) BEFORE running any part of this file.
--
-- Three sections, run one at a time in this order, in the same psql session:
--   PREVIEW  read-only counts + FK inventory (BEGIN READ ONLY ... ROLLBACK)
--   RESET    the single destructive transaction; refuses to run unless the session has
--            SET gohard.confirm_reset = 'ERASE-WORKOUT-HISTORY';
--   VERIFY   read-only; every must_be_zero row must be 0 and every preserved row must
--            equal the PREVIEW's value.
--
-- Erases:   Sessions -> Exercises -> ExerciseSets; progress_analysis ChatConversations
--           (+ ChatMessages); SharedWorkouts (+ SharedWorkoutLikes, SharedWorkoutSaves).
-- Resets:   ProgramWorkouts completion/skip markers; Programs CurrentWeek/CurrentDay and
--           IsCompleted/CompletedAt; completed programs -> active.
-- Preserves everything else (SessionCreateOperations stay as tombstones, SessionId NULL).
-- =====================================================================================

-- PREVIEW
BEGIN READ ONLY;

SELECT "check", "count" FROM (VALUES
  (1,  'erase:Sessions',                            (SELECT COUNT(*) FROM "Sessions")),
  (2,  'erase:Exercises',                           (SELECT COUNT(*) FROM "Exercises")),
  (3,  'erase:ExerciseSets',                        (SELECT COUNT(*) FROM "ExerciseSets")),
  (4,  'erase:ChatConversations(progress_analysis)',(SELECT COUNT(*) FROM "ChatConversations" WHERE "Type" = 'progress_analysis')),
  (5,  'erase:ChatMessages(progress_analysis)',     (SELECT COUNT(*) FROM "ChatMessages" m JOIN "ChatConversations" c ON c."Id" = m."ConversationId" WHERE c."Type" = 'progress_analysis')),
  (6,  'erase:SharedWorkouts',                      (SELECT COUNT(*) FROM "SharedWorkouts")),
  (7,  'erase:SharedWorkoutLikes',                  (SELECT COUNT(*) FROM "SharedWorkoutLikes")),
  (8,  'erase:SharedWorkoutSaves',                  (SELECT COUNT(*) FROM "SharedWorkoutSaves")),
  (9,  'detach:SessionCreateOperations(SessionId)', (SELECT COUNT(*) FROM "SessionCreateOperations" WHERE "SessionId" IS NOT NULL)),
  (10, 'reset:ProgramWorkouts(progress)',           (SELECT COUNT(*) FROM "ProgramWorkouts" WHERE "IsCompleted" OR "CompletedAt" IS NOT NULL OR "CompletionNotes" IS NOT NULL OR "IsSkipped" OR "SkippedAt" IS NOT NULL)),
  (11, 'reset:Programs(progress)',                  (SELECT COUNT(*) FROM "Programs" WHERE "CurrentWeek" <> 1 OR "CurrentDay" <> 1 OR "IsCompleted" OR "CompletedAt" IS NOT NULL)),
  (12, 'reset:Programs(completed->active)',         (SELECT COUNT(*) FROM "Programs" WHERE "Status" = 'completed' OR ("Status" = 'active' AND "IsCompleted"))),
  -- Blockers: must be 0 or RESET refuses (a progress_analysis delete would otherwise
  -- SET NULL a food item's provenance / be blocked by a program's NO ACTION FK).
  (13, 'blocker:Programs.SourceConversationId->progress_analysis',     (SELECT COUNT(*) FROM "Programs" p JOIN "ChatConversations" c ON c."Id" = p."SourceConversationId" WHERE c."Type" = 'progress_analysis')),
  (14, 'blocker:FoodItems.SourcePlanConversationId->progress_analysis',(SELECT COUNT(*) FROM "FoodItems" f JOIN "ChatConversations" c ON c."Id" = f."SourcePlanConversationId" WHERE c."Type" = 'progress_analysis')),
  -- Preserved: VERIFY must report the same numbers.
  (20, 'preserved:Users',                           (SELECT COUNT(*) FROM "Users")),
  (21, 'preserved:Goals',                           (SELECT COUNT(*) FROM "Goals")),
  (22, 'preserved:GoalProgressHistory',             (SELECT COUNT(*) FROM "GoalProgressHistory")),
  (23, 'preserved:Programs',                        (SELECT COUNT(*) FROM "Programs")),
  (24, 'preserved:ProgramWorkouts',                 (SELECT COUNT(*) FROM "ProgramWorkouts")),
  (25, 'preserved:SessionCreateOperations',         (SELECT COUNT(*) FROM "SessionCreateOperations")),
  (26, 'preserved:ChatConversations(other)',        (SELECT COUNT(*) FROM "ChatConversations" WHERE "Type" <> 'progress_analysis')),
  (27, 'preserved:ChatMessages(other)',             (SELECT COUNT(*) FROM "ChatMessages" m JOIN "ChatConversations" c ON c."Id" = m."ConversationId" WHERE c."Type" <> 'progress_analysis')),
  (28, 'preserved:ExerciseTemplates',               (SELECT COUNT(*) FROM "ExerciseTemplates")),
  (29, 'preserved:WorkoutTemplates',                (SELECT COUNT(*) FROM "WorkoutTemplates")),
  (30, 'preserved:BodyMetrics',                     (SELECT COUNT(*) FROM "BodyMetrics")),
  (31, 'preserved:RunSessions',                     (SELECT COUNT(*) FROM "RunSessions")),
  (32, 'preserved:MealLogs',                        (SELECT COUNT(*) FROM "MealLogs")),
  (33, 'preserved:MealEntries',                     (SELECT COUNT(*) FROM "MealEntries")),
  (34, 'preserved:FoodItems',                       (SELECT COUNT(*) FROM "FoodItems")),
  (35, 'preserved:FoodItems(with plan source)',     (SELECT COUNT(*) FROM "FoodItems" WHERE "SourcePlanConversationId" IS NOT NULL)),
  (36, 'preserved:NutritionGoals',                  (SELECT COUNT(*) FROM "NutritionGoals")),
  (37, 'preserved:MealPlans',                       (SELECT COUNT(*) FROM "MealPlans"))
) AS t(ord, "check", "count")
ORDER BY ord;

-- FK inventory: every foreign key pointing at a table this reset deletes from or updates.
-- Compare with the table in the runbook; ANY difference => stop, do not run RESET.
SELECT replace(con.conrelid::regclass::text, '"', '')  AS referencing_table,
       con.conname::text             AS constraint_name,
       replace(con.confrelid::regclass::text, '"', '') AS referenced_table,
       CASE con.confdeltype WHEN 'a' THEN 'NO ACTION' WHEN 'r' THEN 'RESTRICT' WHEN 'c' THEN 'CASCADE'
                            WHEN 'n' THEN 'SET NULL' WHEN 'd' THEN 'SET DEFAULT' END AS on_delete
FROM pg_constraint con
WHERE con.contype = 'f'
  AND con.confrelid IN ('"Sessions"'::regclass, '"Exercises"'::regclass, '"ExerciseSets"'::regclass,
                        '"ChatConversations"'::regclass, '"ChatMessages"'::regclass,
                        '"SharedWorkouts"'::regclass, '"SharedWorkoutLikes"'::regclass, '"SharedWorkoutSaves"'::regclass,
                        '"Programs"'::regclass, '"ProgramWorkouts"'::regclass)
ORDER BY 1, 2;

ROLLBACK;

-- RESET
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '120s';

DO $$
BEGIN
  IF current_setting('gohard.confirm_reset', true) IS DISTINCT FROM 'ERASE-WORKOUT-HISTORY' THEN
    RAISE EXCEPTION 'Refusing to reset: run  SET gohard.confirm_reset = ''ERASE-WORKOUT-HISTORY'';  in this session first (see runbook)';
  END IF;
END $$;

-- Block concurrent writers for the life of this transaction (reads still allowed).
LOCK TABLE "Sessions", "Exercises", "ExerciseSets", "SessionCreateOperations",
           "ChatConversations", "ChatMessages",
           "SharedWorkouts", "SharedWorkoutLikes", "SharedWorkoutSaves",
           "Programs", "ProgramWorkouts", "FoodItems"
  IN EXCLUSIVE MODE;

DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM "Programs" p JOIN "ChatConversations" c ON c."Id" = p."SourceConversationId"
             WHERE c."Type" = 'progress_analysis')
     OR EXISTS (SELECT 1 FROM "FoodItems" f JOIN "ChatConversations" c ON c."Id" = f."SourcePlanConversationId"
                WHERE c."Type" = 'progress_analysis') THEN
    RAISE EXCEPTION 'Refusing to reset: a Program or FoodItem references a progress_analysis conversation (see PREVIEW blocker rows); escalate, do not work around';
  END IF;
END $$;

-- Workout history: children before parents, explicitly (do not rely on cascades).
DELETE FROM "ExerciseSets";
DELETE FROM "Exercises";
UPDATE "SessionCreateOperations" SET "SessionId" = NULL WHERE "SessionId" IS NOT NULL;
DELETE FROM "Sessions";

-- AI progress analyses (only this conversation type).
DELETE FROM "ChatMessages"
 WHERE "ConversationId" IN (SELECT "Id" FROM "ChatConversations" WHERE "Type" = 'progress_analysis');
DELETE FROM "ChatConversations" WHERE "Type" = 'progress_analysis';

-- Shared workouts and all their child rows.
DELETE FROM "SharedWorkoutLikes";
DELETE FROM "SharedWorkoutSaves";
DELETE FROM "SharedWorkouts";

-- Program progress (rows, schedule and ExercisesJson are kept).
UPDATE "ProgramWorkouts"
   SET "IsCompleted" = FALSE, "CompletedAt" = NULL, "CompletionNotes" = NULL,
       "IsSkipped" = FALSE, "SkippedAt" = NULL
 WHERE "IsCompleted" OR "CompletedAt" IS NOT NULL OR "CompletionNotes" IS NOT NULL
    OR "IsSkipped" OR "SkippedAt" IS NOT NULL;

-- Completed -> active. Covers PUT /complete (Status 'completed') and the /advance
-- overflow (Status stays 'active' but IsCompleted/IsActive=false). Draft, archived and
-- deleted programs keep their Status and IsActive.
UPDATE "Programs"
   SET "Status" = 'active', "IsActive" = TRUE
 WHERE "Status" = 'completed' OR ("Status" = 'active' AND "IsCompleted");

UPDATE "Programs"
   SET "CurrentWeek" = 1, "CurrentDay" = 1, "IsCompleted" = FALSE, "CompletedAt" = NULL
 WHERE "CurrentWeek" <> 1 OR "CurrentDay" <> 1 OR "IsCompleted" OR "CompletedAt" IS NOT NULL;

COMMIT;

-- VERIFY
BEGIN READ ONLY;

SELECT "check", "count" FROM (VALUES
  (1,  'must_be_zero:Sessions',                            (SELECT COUNT(*) FROM "Sessions")),
  (2,  'must_be_zero:Exercises',                           (SELECT COUNT(*) FROM "Exercises")),
  (3,  'must_be_zero:ExerciseSets',                        (SELECT COUNT(*) FROM "ExerciseSets")),
  (4,  'must_be_zero:ChatConversations(progress_analysis)',(SELECT COUNT(*) FROM "ChatConversations" WHERE "Type" = 'progress_analysis')),
  (5,  'must_be_zero:ChatMessages(orphaned)',              (SELECT COUNT(*) FROM "ChatMessages" m WHERE NOT EXISTS (SELECT 1 FROM "ChatConversations" c WHERE c."Id" = m."ConversationId"))),
  (6,  'must_be_zero:SharedWorkouts',                      (SELECT COUNT(*) FROM "SharedWorkouts")),
  (7,  'must_be_zero:SharedWorkoutLikes',                  (SELECT COUNT(*) FROM "SharedWorkoutLikes")),
  (8,  'must_be_zero:SharedWorkoutSaves',                  (SELECT COUNT(*) FROM "SharedWorkoutSaves")),
  (9,  'must_be_zero:SessionCreateOperations(SessionId)',  (SELECT COUNT(*) FROM "SessionCreateOperations" WHERE "SessionId" IS NOT NULL)),
  (10, 'must_be_zero:ProgramWorkouts(progress)',           (SELECT COUNT(*) FROM "ProgramWorkouts" WHERE "IsCompleted" OR "CompletedAt" IS NOT NULL OR "CompletionNotes" IS NOT NULL OR "IsSkipped" OR "SkippedAt" IS NOT NULL)),
  (11, 'must_be_zero:Programs(progress)',                  (SELECT COUNT(*) FROM "Programs" WHERE "CurrentWeek" <> 1 OR "CurrentDay" <> 1 OR "IsCompleted" OR "CompletedAt" IS NOT NULL)),
  (12, 'must_be_zero:Programs(completed)',                 (SELECT COUNT(*) FROM "Programs" WHERE "Status" = 'completed')),
  (20, 'preserved:Users',                           (SELECT COUNT(*) FROM "Users")),
  (21, 'preserved:Goals',                           (SELECT COUNT(*) FROM "Goals")),
  (22, 'preserved:GoalProgressHistory',             (SELECT COUNT(*) FROM "GoalProgressHistory")),
  (23, 'preserved:Programs',                        (SELECT COUNT(*) FROM "Programs")),
  (24, 'preserved:ProgramWorkouts',                 (SELECT COUNT(*) FROM "ProgramWorkouts")),
  (25, 'preserved:SessionCreateOperations',         (SELECT COUNT(*) FROM "SessionCreateOperations")),
  (26, 'preserved:ChatConversations(other)',        (SELECT COUNT(*) FROM "ChatConversations" WHERE "Type" <> 'progress_analysis')),
  (27, 'preserved:ChatMessages(other)',             (SELECT COUNT(*) FROM "ChatMessages" m JOIN "ChatConversations" c ON c."Id" = m."ConversationId" WHERE c."Type" <> 'progress_analysis')),
  (28, 'preserved:ExerciseTemplates',               (SELECT COUNT(*) FROM "ExerciseTemplates")),
  (29, 'preserved:WorkoutTemplates',                (SELECT COUNT(*) FROM "WorkoutTemplates")),
  (30, 'preserved:BodyMetrics',                     (SELECT COUNT(*) FROM "BodyMetrics")),
  (31, 'preserved:RunSessions',                     (SELECT COUNT(*) FROM "RunSessions")),
  (32, 'preserved:MealLogs',                        (SELECT COUNT(*) FROM "MealLogs")),
  (33, 'preserved:MealEntries',                     (SELECT COUNT(*) FROM "MealEntries")),
  (34, 'preserved:FoodItems',                       (SELECT COUNT(*) FROM "FoodItems")),
  (35, 'preserved:FoodItems(with plan source)',     (SELECT COUNT(*) FROM "FoodItems" WHERE "SourcePlanConversationId" IS NOT NULL)),
  (36, 'preserved:NutritionGoals',                  (SELECT COUNT(*) FROM "NutritionGoals")),
  (37, 'preserved:MealPlans',                       (SELECT COUNT(*) FROM "MealPlans"))
) AS t(ord, "check", "count")
ORDER BY ord;

ROLLBACK;
