using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoHardAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddNutritionGoalEffectiveDate : Migration
    {
        private const string SqlServer = "Microsoft.EntityFrameworkCore.SqlServer";
        private const string Npgsql = "Npgsql.EntityFrameworkCore.PostgreSQL";

        // Raw, provider-branched SQL rather than the scaffolded `AddColumn<T>(type: ...)`
        // calls, which came back with literal SQL Server types ("datetime2"/"bit") that
        // are used verbatim by whichever provider actually runs the migration - see
        // `AddGoalArchiveAndMealPlanSourceIdentity` for the full rationale.
        //
        // EffectiveDate backfill: existing rows predate this column, and - before this
        // phase's fix - two endpoints (NutritionGoalsController.UpdateNutritionGoal and
        // ChatController's meal-plan-apply) mutated a row's macro fields IN PLACE,
        // stamping UpdatedAt each time. That means CreatedAt alone is NOT reliable
        // evidence of when a row's CURRENT values started applying: a row created months
        // ago whose values were later overwritten in place has values that are only known
        // to have applied since that LAST overwrite, not since CreatedAt - backfilling
        // EffectiveDate = CreatedAt for such a row would fabricate a history that was
        // never actually recorded (the pre-overwrite values are genuinely gone; we must
        // not pretend the post-overwrite values applied that far back).
        //
        // So the backfill uses COALESCE(UpdatedAt, CreatedAt): a row that was NEVER
        // touched by ANY old code path (UpdatedAt IS NULL) gets its real CreatedAt as
        // EffectiveDate - the only evidence that exists. A row that WAS touched
        // (UpdatedAt IS NOT NULL, for ANY reason - see below) gets UpdatedAt as
        // EffectiveDate instead, and the interval between CreatedAt and UpdatedAt
        // resolves as unknown (no row covers it) rather than being backfilled with the
        // current, possibly-different values. This never changes any row's stored
        // values - "preserve the current target" at whatever this migration's cutover
        // finds it to be - it only refuses to over-claim how far back those values are
        // evidenced to have applied. Since EF migrations record themselves in
        // __EFMigrationsHistory and never re-run, and the guard below is
        // `WHERE EffectiveDate IS NULL`, repeated deployment cannot re-execute this
        // backfill or move an already-established EffectiveDate/DeletedAt - see
        // NutritionGoalEffectiveDateMigrationPostgresTests
        // .a_second_migrate_call_after_the_migration_has_run_changes_nothing.
        //
        // A blind "trust UpdatedAt unless it looks like a sibling's creation caused
        // it" correlation heuristic was tried and REMOVED - it does not prove
        // historical values and can fabricate them. Concrete counterexample: goal A
        // is created Jan 1 at 2000 kcal, genuinely edited in place Feb 1 to 1800 kcal
        // (the old UpdateNutritionGoal endpoint - A.UpdatedAt becomes Feb 1), then
        // goal B is created Mar 1, and the old DeactivateOtherGoals helper
        // OVERWRITES A.UpdatedAt again to Mar 1 purely because B was created (never
        // touching A's macro fields). UpdatedAt is a single mutable column with no
        // audit trail: by the time the migration runs, A.UpdatedAt = Mar 1 is the
        // ONLY surviving timestamp - the Feb 1 edit's evidence is gone. A "sibling
        // created near UpdatedAt => not real edit evidence => fall back to
        // CreatedAt" correlation would then backfill A.EffectiveDate = Jan 1 with
        // A's CURRENT (1800 kcal, the Feb-1 value) fields - which asserts that 1800
        // kcal applied throughout January, when 2000 actually did. That is exactly
        // the fabrication this migration exists to prevent, and no timing window
        // fixes it: the correlation cannot tell "only ever deactivated" apart from
        // "edited, then later deactivated (overwriting the edit's timestamp)" -
        // both leave the identical fingerprint once UpdatedAt is overwritten a
        // second time. So UpdatedAt is trusted whenever it is set, full stop,
        // regardless of what caused it (a real edit, or DeactivateOtherGoals'/
        // ActivateGoal's side effect) - the conservative, always-safe reading: the
        // row's current values are evidenced to apply since AT LATEST that
        // timestamp, never earlier. This can under-claim history for a goal that
        // was only ever deactivated (never edited) - genuinely lost information is
        // reported as unknown rather than guessed, per the explicit "do not prefer
        // apparent historical completeness over accuracy" requirement - but it can
        // never over-claim it.
        //
        // The Postgres backfill explicitly does `... AT TIME ZONE 'UTC'` rather than a bare
        // cast: CreatedAt/UpdatedAt are naive "timestamp without time zone" columns that
        // always hold a UTC wall-clock value (DateTime.UtcNow, written under this app's
        // Npgsql.EnableLegacyTimestampBehavior switch - see SessionCreateOperationSql's doc
        // comment), but EffectiveDate is "timestamp with time zone". A bare
        // naive-to-timestamptz assignment is resolved using the CONNECTION's `timezone` GUC
        // at migration-run time, not necessarily UTC - `AT TIME ZONE 'UTC'` instead asserts
        // "this naive value already IS UTC wall-clock time", producing the correct instant
        // regardless of session timezone. Never replace this with a plain cast.
        //
        // Same-EffectiveDate tie-break: ResolveForDateAsync/ResolveForDateRangeAsync/
        // RecomputeActiveFlagAsync all order by EffectiveDate DESC, Id DESC - when two
        // versions share an EffectiveDate (day-granularity backfill can produce this for
        // two same-day legacy edits), the row with the HIGHER Id (the one inserted/
        // migrated later) wins deterministically. The partial unique index below only
        // enforces "at most one row flagged active" going forward for NEW writes through
        // NutritionTargetService - it says nothing about which historical version resolves
        // for a shared date; the ORDER BY is the actual tie-break authority and is
        // consistent across every read path.
        //
        // The partial unique index enforces "at most one active row per user" at the DB
        // level - a backstop against a race where two concurrent SetActiveGoalAsync calls
        // for the same user both try to leave a row active; one commits, the other fails
        // the constraint and can be retried by the caller.
        private const string SqlServerUp = @"
ALTER TABLE [NutritionGoals] ADD [EffectiveDate] datetime2 NULL;
ALTER TABLE [NutritionGoals] ADD [DeletedAt] datetime2 NULL;
UPDATE [NutritionGoals] SET [EffectiveDate] = CAST(COALESCE([UpdatedAt], [CreatedAt]) AS date) WHERE [EffectiveDate] IS NULL;
ALTER TABLE [NutritionGoals] ALTER COLUMN [EffectiveDate] datetime2 NOT NULL;
CREATE UNIQUE INDEX [IX_NutritionGoals_UserId_Active] ON [NutritionGoals] ([UserId]) WHERE [IsActive] = 1;
";

        private const string NpgsqlUp = @"
ALTER TABLE ""NutritionGoals"" ADD ""EffectiveDate"" timestamp with time zone NULL;
ALTER TABLE ""NutritionGoals"" ADD ""DeletedAt"" timestamp with time zone NULL;
UPDATE ""NutritionGoals"" SET ""EffectiveDate"" = (date_trunc('day', COALESCE(""UpdatedAt"", ""CreatedAt"")) AT TIME ZONE 'UTC') WHERE ""EffectiveDate"" IS NULL;
ALTER TABLE ""NutritionGoals"" ALTER COLUMN ""EffectiveDate"" SET NOT NULL;
CREATE UNIQUE INDEX ""IX_NutritionGoals_UserId_Active"" ON ""NutritionGoals"" (""UserId"") WHERE ""IsActive"" = TRUE;
";

        private const string SqlServerDown = @"
DROP INDEX [IX_NutritionGoals_UserId_Active] ON [NutritionGoals];
ALTER TABLE [NutritionGoals] DROP COLUMN [EffectiveDate];
ALTER TABLE [NutritionGoals] DROP COLUMN [DeletedAt];
";

        private const string NpgsqlDown = @"
DROP INDEX ""IX_NutritionGoals_UserId_Active"";
ALTER TABLE ""NutritionGoals"" DROP COLUMN ""EffectiveDate"";
ALTER TABLE ""NutritionGoals"" DROP COLUMN ""DeletedAt"";
";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(migrationBuilder.ActiveProvider switch
            {
                SqlServer => SqlServerUp,
                Npgsql => NpgsqlUp,
                _ => NpgsqlUp,
            });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(migrationBuilder.ActiveProvider switch
            {
                SqlServer => SqlServerDown,
                Npgsql => NpgsqlDown,
                _ => NpgsqlDown,
            });
        }
    }
}
