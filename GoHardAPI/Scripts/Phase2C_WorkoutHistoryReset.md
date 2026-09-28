# Phase 2C — workout-history reset runbook (manual, never automatic)

Script: `GoHardAPI/Scripts/Phase2C_WorkoutHistoryReset.sql` (PostgreSQL only).
Tested only against a disposable Testcontainers database
(`GoHardAPI.Tests/Scripts/Phase2CWorkoutHistoryResetPostgresTests.cs`). Nothing in the API,
its migrations or hosted services runs this script; a test enforces that.

The owner decided that existing lifted-weight history is disposable: old builds stored
numbers typed under an "lbs" label in the same `Weight` field now defined as canonical kg,
so that history cannot be converted reliably.

## What it does

| Action | Rows |
| --- | --- |
| **Erase** | `Sessions`, `Exercises`, `ExerciseSets` (all users) |
| **Erase** | `ChatConversations` with `Type = 'progress_analysis'` and their `ChatMessages` |
| **Erase** | `SharedWorkouts`, `SharedWorkoutLikes`, `SharedWorkoutSaves` (there is no comments table; `SharedWorkouts.CommentCount` is a column) |
| **Detach** | `SessionCreateOperations.SessionId` → `NULL` (rows kept as tombstones, so a legacy client's retried create gets 410 instead of recreating a session) |
| **Reset** | `ProgramWorkouts`: `IsCompleted = false`, `CompletedAt = NULL`, `IsSkipped = false`, `SkippedAt = NULL` |
| **Reset** | `Programs` with `Status = 'completed'`, or `Status = 'active' AND IsCompleted` (the `/advance` overflow path, which never changes `Status`): `Status = 'active'`, `IsActive = true` |
| **Reset** | all `Programs`: `CurrentWeek = 1`, `CurrentDay = 1`, `IsCompleted = false`, `CompletedAt = NULL` |
| **Preserve** | users, `Goals` (incl. `CurrentValue`), `GoalProgressHistory`, `Programs`/`ProgramWorkouts` rows (schedule, `StartDate`, `ExercisesJson`, `ProgramWorkouts.CompletionNotes` (user-written text), `SourceConversationId`), draft/archived/deleted programs' `Status` and `IsActive`, `ExerciseTemplates`, `WorkoutTemplates`, every nutrition table, `BodyMetrics`, `RunSessions`, every non-`progress_analysis` chat, friends/DMs |

## End-to-end rollout

Spec: `docs/superpowers/specs/2026-09-27-canonical-kg-lifted-weight-design.md` §9.
Env vars: `LiftedWeight__RequireCanonicalClient`, `LiftedWeight__CanonicalHistory`
(both default `false`; effective guard = either `true`). Contract endpoint:
`GET api/v1/liftedweightcontract` → `{ canonicalHistory }`.

**Phase 0 — API deploy, both flags false.**
Deploy the API build carrying the guard and the `liftedweightcontract` endpoint
with both env vars unset/`false`. Behaviour is unchanged for every existing
client. Smoke check: `GET api/v1/liftedweightcontract` returns
`{ "canonicalHistory": false }`; a `POST exercisesets` **without** the
`X-Lifted-Weight-Unit` header is still accepted (200/201, not 400). This phase
can ship independently and sit indefinitely — nothing downstream depends on a
deadline here.

**Phase 1 — build, test, submit the canonical app; hold for manual release.**
Build and test the canonical app (the build that sends `X-Lifted-Weight-Unit: kg`,
snapshots local ids on first launch, and withholds workout uploads — Sync­Service
phases and the repositories' direct uploads — until the server reports
`canonicalHistory: true`). Submit it for store review *ahead of* cutover; this is
safe pre-reset because the app never assumes canonical history is live, it just
waits.

Distribution mechanism (`.github/workflows/build-flutter-mobile.yml`): the
`build-android-aab`/`build-ios-signed` jobs produce a signed AAB/IPA for store
submission (`build-ios-signed` needs a manual `workflow_dispatch` with
`export_method: app-store`); `build-android-apk`/`build-ios` (unsigned) produce
sideload/ad-hoc artifacts for pre-release testers. If distributed via App Store /
Play:

- **App Store:** submit for review, then set the release to **"Manually release
  this version"** so approval does not auto-publish it. It sits approved-but-held
  until Phase 2 completes.
- **Play Store:** use **managed publishing** (or a staged rollout halted at 0%)
  so a completed review does not go live automatically.

Testers on TestFlight or a sideloaded unsigned build are safe to install before
cutover: the upload gate means they cannot write ambiguous history. Their
workouts logged before cutover accumulate locally in kg and stay device-only
until their next online sync after cutover, when the app purges legacy local
rows and uploads the canonical ones (spec §7).

**Phase 2 — maintenance window (production reset).**
Do not schedule this until the Phase 1 build is approved and held for manual
release. Follow this runbook's Preconditions → Backup → Exact order → Rollback
sections below unchanged — do not duplicate those steps here. Exact order step 7
(`LiftedWeight__CanonicalHistory=true`) and step 8 (release the app build) are
this phase's tail; they are a separate, explicit operator action from the API
deploy and the app build submission, never automatic.

**Phase 3 — release and monitoring.**
After the maintenance window's VERIFY step passes and `CanonicalHistory=true` is
confirmed live, release the held app build (manual release / resume managed
publishing). Watch:

- Rate of `400 LIFTED_WEIGHT_UNIT_REQUIRED` responses (expected: legacy builds
  still in the field; a spike means the guard flipped correctly).
- `GET api/v1/liftedweightcontract` continuing to report `canonicalHistory: true`.
- First canonical uploads arriving post-purge (sessions/exercises/sets created
  with the `X-Lifted-Weight-Unit` header, timestamped after the release).

### Guarantees and why

- **(A) No legacy client can write ambiguous values after reset:**
  `RequireCanonicalClient=true` is set as Preconditions #1, *before* the
  Backup and every Exact order step (PREVIEW, RESET, VERIFY) — the guard is live
  before any data changes, so no window exists where old clients can write
  post-reset set data. (Unguarded legacy writes that carry no displayed unit —
  empty sessions/exercises, program-workout completion, shared workouts/templates
  — are accepted limitations; see spec §10.)
- **(B) Compatible users are not unnecessarily locked out:** Phase 1 builds and
  ships the canonical app *ahead of* cutover, but it only snapshots and holds
  uploads — it never blocks local logging. Holding the store release manually
  (not gating Phase 1 on Phase 2) means app-review/propagation delay costs
  nothing; the app is simply ready whenever Phase 2 runs.
- **AI never told history is canonical before verification:** `CanonicalHistory`
  is set only at Exact order step 7, after VERIFY (step 6) has passed — never
  earlier, never automatically.

### What if the app review is delayed

Keep Phase 2 unscheduled until the store release is approved and held. Nothing
breaks in the meantime: both env vars stay `false` (Phase 0's steady state),
legacy and canonical builds alike write and sync normally, and the canonical
build's testers keep accumulating device-only history safely under the upload
gate. There is no time pressure from the app-store side — the maintenance
window is triggered by release approval, not the reverse.

### Progress columns (source: `Models/Program.cs`, `Controllers/ProgramsController.cs`)

| Table | Column | Written by |
| --- | --- | --- |
| `ProgramWorkouts` | `IsCompleted`, `CompletedAt`, `CompletionNotes` | `PUT workouts/{id}/complete` (`CompletionNotes` is preserved by the reset) |
| `ProgramWorkouts` | `IsSkipped`, `SkippedAt` | `PUT workouts/{id}/skip` / `unskip` |
| `Programs` | `CurrentWeek`, `CurrentDay` | `PUT {id}/advance` |
| `Programs` | `IsCompleted`, `CompletedAt`, `IsActive` | `PUT {id}/complete`; `PUT {id}/advance` past the last week |
| `Programs` | `Status` (`draft`/`active`/`completed`/`archived`/`deleted`) | `complete` → `completed`; `archive`/`unarchive`; `DELETE` → `deleted`; activation `draft` → `active` |

### Foreign keys into affected tables

Sources: `Migrations/TrainingContextModelSnapshot.cs`, `Data/TrainingContext.cs`, the
migrations' raw SQL (`AddProgramStatusAndSourceConversation`,
`AddGoalArchiveAndMealPlanSourceIdentity`, `SessionCreateOperationSql.cs`) and the
startup schema patching in `Program.cs` (`RunStartupDatabaseBootstrap`, which re-creates
`FK_Sessions_Programs_ProgramId`, `FK_Sessions_ProgramWorkouts_ProgramWorkoutId`,
`FK_Programs_Goals_GoalId`, `FK_ProgramWorkouts_Programs_ProgramId` as `ON DELETE CASCADE`,
and creates only Users-referencing FKs otherwise).

| Referencing table.column | → Referenced | ON DELETE | Effect on this reset |
| --- | --- | --- | --- |
| `Exercises.SessionId` | `Sessions` | CASCADE | children deleted explicitly first |
| `ExerciseSets.ExerciseId` | `Exercises` | CASCADE | children deleted explicitly first |
| `SessionCreateOperations.SessionId` | `Sessions` | SET NULL | nulled explicitly first; rows kept |
| `ChatMessages.ConversationId` | `ChatConversations` | CASCADE | progress_analysis messages deleted explicitly first |
| `FoodItems.SourcePlanConversationId` | `ChatConversations` | SET NULL | would silently detach meal-plan provenance → RESET refuses if any points at a progress_analysis conversation (app only sets it from `meal_plan`/`combined_plan`) |
| `Programs.SourceConversationId` | `ChatConversations` | NO ACTION | would block the delete → RESET refuses up front if any points at a progress_analysis conversation (app only sets it from `workout_plan`) |
| `SharedWorkoutLikes.SharedWorkoutId` | `SharedWorkouts` | CASCADE | deleted explicitly first |
| `SharedWorkoutSaves.SharedWorkoutId` | `SharedWorkouts` | CASCADE | deleted explicitly first |
| `Sessions.ProgramId` | `Programs` | CASCADE | none (programs are updated, never deleted) |
| `Sessions.ProgramWorkoutId` | `ProgramWorkouts` | CASCADE | none (workouts are updated, never deleted) |
| `ProgramWorkouts.ProgramId` | `Programs` | CASCADE | none |

`SharedWorkouts.OriginalId` is a plain integer (no FK). No table references `ExerciseSets`,
`ChatMessages`, `SharedWorkoutLikes` or `SharedWorkoutSaves`.

The PREVIEW section prints the live FK inventory from `pg_constraint`. **If production's
list differs from this table (extra constraint, different ON DELETE), stop and escalate.**

## Preconditions

1. The API build with the lifted-weight guard is deployed, and
   `LiftedWeight__RequireCanonicalClient=true` is set and live. Verify: a `POST` set write
   **without** the `X-Lifted-Weight-Unit: kg` header returns **400**. (From here on no
   legacy build can write a set.)
2. `LiftedWeight__CanonicalHistory` is still `false` (the AI must not describe history as kg
   until VERIFY has passed).
3. Maintenance window announced; you have a psql session to the production database with a
   role that can `LOCK`/`DELETE`/`UPDATE` these tables.
4. The canonical app build is approved in the stores and held for manual release.

## Backup (mandatory, before anything else)

```sh
pg_dump --format=custom --no-owner --file=gohard-pre-phase2c-$(date -u +%Y%m%dT%H%M%SZ).dump "$PROD_URL"
pg_restore --list gohard-pre-phase2c-*.dump | head   # sanity check the archive is readable
```

Also take a Railway backup/snapshot of the Postgres volume from the Railway dashboard and
note its timestamp. Do not continue without both.

## Exact order

1. `psql -X -v ON_ERROR_STOP=1 "$PROD_URL"` — one session for the whole procedure. `-X`
   skips `~/.psqlrc`; then run `\set ON_ERROR_ROLLBACK off` and `\set AUTOCOMMIT on` and
   check with `\echo :ON_ERROR_ROLLBACK` that it prints `off`. (The RESET work is a single
   `DO` block, so even with `ON_ERROR_ROLLBACK` left on, a failed guard cannot let the
   deletes run; the settings are defence in depth.)
2. Paste the **PREVIEW** section (from the `-- PREVIEW` line to the first `ROLLBACK;`).
   - Save the output.
   - Both `blocker:` rows must be `0`. Otherwise stop and escalate (do not edit data by hand).
   - Compare the FK inventory with the table above. Any difference: stop.
3. `SET gohard.confirm_reset = 'ERASE-WORKOUT-HISTORY';`
4. Paste the **RESET** section (from `-- RESET` to its `COMMIT;`).
   - All guards, locks, deletes and updates run as one `DO` statement: if it errors (missing
     confirmation, blocker, lock timeout, anything) none of its work is applied, whatever
     the client's `ON_ERROR_ROLLBACK` setting. Then type `ROLLBACK;` (the `COMMIT;` that
     follows a failed statement also only rolls back). Verify nothing changed by
     re-running PREVIEW. Lock timeouts can be retried; anything else: stop.
   - Paste only the section; to be extra careful, remove the final `COMMIT;`, inspect with
     ad-hoc `SELECT`s inside the transaction, then type `COMMIT;` (or `ROLLBACK;`).
5. `RESET gohard.confirm_reset;`
6. Paste the **VERIFY** section. Every `must_be_zero:` row must be `0`; every `preserved:`
   row must equal the PREVIEW value. `must_be_zero:Sessions`/`Exercises` may only be
   non-zero if a legacy client created an (empty) session after COMMIT — the guard only
   covers set writes; `ExerciseSets` must be `0`. Investigate before continuing.
   If the API stays online during the window, `preserved:` counts for non-workout tables
   (users, goals, goal history, meals/food, chats, body metrics, runs, templates…) can
   legitimately drift between PREVIEW and VERIFY from normal user activity; confirm any
   difference is explainable that way. To get exact equality instead, put the API in
   maintenance/offline between PREVIEW and VERIFY. `Programs`/`ProgramWorkouts` counts
   drifting is still worth a look (the reset never inserts or deletes them).
7. Set `LiftedWeight__CanonicalHistory=true` and redeploy/restart the API. Confirm
   `GET api/v1/liftedweightcontract` reports `canonicalHistory: true`.
8. Release the canonical app build.

The RESET section refuses to run without the step-3 setting, so pasting or running the
whole file by mistake aborts the single `DO` block instead of erasing data (tested,
including with a savepoint around every statement as psql `ON_ERROR_ROLLBACK` does).

## Rollback

- **Before `COMMIT`:** `ROLLBACK;` — nothing changes.
- **After `COMMIT`:** the reset is **not reversible in SQL**. Restore from the backup:
  keep `LiftedWeight__CanonicalHistory=false`, stop the API, then
  `pg_restore --clean --if-exists --no-owner --dbname="$PROD_URL" gohard-pre-phase2c-<ts>.dump`
  (or restore the Railway snapshot), start the API, re-run PREVIEW to confirm the old counts.
  Any data written between COMMIT and the restore is lost.
- **Aborting before step 7:** also set `LiftedWeight__RequireCanonicalClient=false` again
  (and redeploy/restart), otherwise legacy builds' set writes stay blocked with the
  update-required error. Keep the canonical app build held.
- **Restoring from backup AFTER step 7 is not a clean rollback:** once
  `CanonicalHistory=true` is live, canonical devices complete their one-time local purge
  (terminal state `lifted_weight_contract_v1: complete`) and would then download the restored
  legacy lb-typed rows as kg. Purging them again needs a new versioned key (e.g.
  `lifted_weight_contract_v2`) and a new app release. So never set step 7 until VERIFY has
  passed, and treat a post-step-7 restore as a new incident, not a rollback.

## App and local-cache implications

- Canonical app builds hold workout uploads until the server reports
  `canonicalHistory: true`; on their next online sync they purge the old local workout
  cache and resume uploads.
- Legacy builds: set writes return the update-required error; their cached sessions no
  longer exist server-side (reads return 404/empty). A retried keyed session create hits the
  `SessionCreateOperations` tombstone and gets 410 rather than recreating the session.
- Programs keep their schedule and `StartDate` but restart at week 1 / day 1 with no
  completed or skipped workouts; completed programs reappear as active. A user can
  therefore end up with several active programs (every completed one becomes active and
  nothing enforces a single active program); the app must tolerate that.
- Goals, goal progress history, body metrics, runs, nutrition and non-progress chats are
  untouched. Previous AI progress analyses are gone (they quoted the old unit-ambiguous
  weights).
