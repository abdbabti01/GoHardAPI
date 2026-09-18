using GoHardAPI.Data;
using GoHardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GoHardAPI.Services
{
    /// <summary>
    /// Single authoritative read/write path for nutrition TARGETS (calorie/macro
    /// goals) - not actual food logging, which stays on <see cref="MealLog"/> /
    /// <see cref="NutritionProgressCalculator"/> untouched.
    ///
    /// A target change never mutates an existing row's macro fields in place -
    /// every change is a new row stamped with the calendar date (UTC midnight,
    /// same convention as <see cref="MealLog.Date"/>) it starts applying from.
    /// Historical resolution for a given date always picks the row with the
    /// greatest <see cref="NutritionGoal.EffectiveDate"/> that is `&lt;=` that
    /// date and not soft-deleted as of that date - never "today's active row"
    /// applied retroactively. This is what keeps past days' targets stable when
    /// today's target changes, and lets deleting today's target cleanly revert
    /// to whatever applied immediately before it without erasing anything.
    ///
    /// <see cref="NutritionGoal.IsActive"/> is kept in sync as a convenience for
    /// callers that only care about "today" (chat context lookups etc.) but is
    /// never consulted by <see cref="ResolveForDateAsync"/> - it is a projection
    /// of "today's" resolution, not a second source of truth.
    ///
    /// <see cref="SetActiveGoalAsync"/> wraps the deactivate-old + insert-new
    /// pair in one transaction (matching the existing transactional pattern used
    /// by <c>ProgramsController.SwapWorkouts</c>), and the migration
    /// (<c>AddNutritionGoalEffectiveDate</c>) adds a partial unique index on
    /// <c>NutritionGoals (UserId) WHERE "IsActive"</c> as a DB-level backstop so
    /// two concurrent calls for the same user can never both leave a row
    /// flagged active - one commits, the other fails the constraint and can be
    /// retried by the caller.
    /// </summary>
    public static class NutritionTargetService
    {
        /// <summary>
        /// Normalizes any DateTime to the UTC-midnight convention shared with
        /// MealLog.Date, so a target's effective date and a meal log's date are
        /// always compared on the same calendar day regardless of the caller's
        /// original Kind/offset.
        /// </summary>
        public static DateTime NormalizeDate(DateTime date) =>
            DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);

        /// <summary>
        /// Create a new active target effective from <paramref name="effectiveDate"/>
        /// (defaults to today). Deactivates every other currently-active row for
        /// this user but never edits their stored macro values - so whatever
        /// applied on past dates through those rows is untouched. Opens and
        /// commits its own transaction - for a caller that is already inside
        /// one (e.g. a retryable atomic attempt), use
        /// <see cref="ApplyActiveGoalChangeAsync"/> instead so transactions
        /// don't nest.
        /// </summary>
        public static async Task<NutritionGoal> SetActiveGoalAsync(
            TrainingContext context,
            int userId,
            NutritionGoal fields,
            DateTime? effectiveDate = null,
            CancellationToken cancellationToken = default)
        {
            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var newGoal = await ApplyActiveGoalChangeAsync(context, userId, fields, effectiveDate, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return newGoal;
        }

        /// <summary>
        /// Same effect as <see cref="SetActiveGoalAsync"/> (deactivate old,
        /// insert a new dated row) but without opening its own transaction -
        /// for callers already running inside one, such as
        /// <c>ChatController</c>'s meal-plan-apply retryable atomic attempts.
        /// The caller is responsible for committing.
        /// </summary>
        public static async Task<NutritionGoal> ApplyActiveGoalChangeAsync(
            TrainingContext context,
            int userId,
            NutritionGoal fields,
            DateTime? effectiveDate = null,
            CancellationToken cancellationToken = default)
        {
            var effective = NormalizeDate(effectiveDate ?? DateTime.UtcNow);

            var newGoal = new NutritionGoal
            {
                UserId = userId,
                Name = fields.Name,
                DailyCalories = fields.DailyCalories,
                DailyProtein = fields.DailyProtein,
                DailyCarbohydrates = fields.DailyCarbohydrates,
                DailyFat = fields.DailyFat,
                DailyFiber = fields.DailyFiber,
                DailySodium = fields.DailySodium,
                DailySugar = fields.DailySugar,
                DailyWater = fields.DailyWater,
                ProteinPercentage = fields.ProteinPercentage,
                CarbohydratesPercentage = fields.CarbohydratesPercentage,
                FatPercentage = fields.FatPercentage,
                // IsActive is provisional here - RecomputeActiveFlagAsync below is the
                // real authority. Inserting this row is not necessarily "the newest
                // change" from TODAY's point of view: a caller may supply a
                // back-dated or future-dated EffectiveDate (e.g. a correction, or "my
                // new plan starts Monday"), in which case some OTHER existing row may
                // still be the one that correctly resolves for today.
                IsActive = false,
                EffectiveDate = effective,
                CreatedAt = DateTime.UtcNow,
                Explanation = fields.Explanation,
                Bmr = fields.Bmr,
                Tdee = fields.Tdee,
                CalorieAdjustment = fields.CalorieAdjustment,
            };

            if (newGoal.ProteinPercentage.HasValue || newGoal.CarbohydratesPercentage.HasValue || newGoal.FatPercentage.HasValue)
            {
                newGoal.CalculateMacrosFromPercentages();
            }

            context.NutritionGoals.Add(newGoal);
            await context.SaveChangesAsync(cancellationToken);

            // Recompute which row (if any) is now "active" for TODAY across every
            // non-deleted row for this user - never just flip IsActive on the row we
            // happened to just insert. This is what keeps IsActive from desyncing
            // from what ResolveForDateAsync(today) actually returns when a client
            // supplies an out-of-order EffectiveDate.
            await RecomputeActiveFlagAsync(context, userId, cancellationToken);

            return newGoal;
        }

        /// <summary>
        /// Sets IsActive=true on exactly the row that <see cref="ResolveForDateAsync"/>
        /// would return for TODAY (or clears it on every row if none applies), and
        /// IsActive=false on every other row for this user. This is the single place
        /// that decides "is-active" - callers never set the flag directly, so it can
        /// never drift from what date-aware resolution actually returns.
        /// </summary>
        private static async Task RecomputeActiveFlagAsync(
            TrainingContext context,
            int userId,
            CancellationToken cancellationToken)
        {
            var today = NormalizeDate(DateTime.UtcNow);
            var candidates = await context.NutritionGoals
                .Where(g => g.UserId == userId)
                .ToListAsync(cancellationToken);

            var currentForToday = candidates
                .Where(g => g.EffectiveDate <= today && (g.DeletedAt == null || g.DeletedAt.Value.Date > today))
                .OrderByDescending(g => g.EffectiveDate)
                .ThenByDescending(g => g.Id)
                .FirstOrDefault();

            foreach (var candidate in candidates)
            {
                var shouldBeActive = currentForToday != null && candidate.Id == currentForToday.Id;
                if (candidate.IsActive != shouldBeActive)
                {
                    candidate.IsActive = shouldBeActive;
                }
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// The target in effect for one calendar date, or null if none exists
        /// yet at that point in the user's history (never backfilled with a
        /// guessed/default value).
        /// </summary>
        public static async Task<NutritionGoal?> ResolveForDateAsync(
            TrainingContext context,
            int userId,
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            var target = NormalizeDate(date);

            return await context.NutritionGoals
                .AsNoTracking()
                .Where(g => g.UserId == userId
                    && g.EffectiveDate <= target
                    && (g.DeletedAt == null || g.DeletedAt.Value.Date > target))
                .OrderByDescending(g => g.EffectiveDate)
                .ThenByDescending(g => g.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Batch form of <see cref="ResolveForDateAsync"/> for a history view
        /// rendering many days at once - one query instead of one per day.
        /// </summary>
        public static async Task<Dictionary<DateTime, NutritionGoal?>> ResolveForDateRangeAsync(
            TrainingContext context,
            int userId,
            DateTime start,
            DateTime end,
            CancellationToken cancellationToken = default)
        {
            var startDate = NormalizeDate(start);
            var endDate = NormalizeDate(end);

            var candidates = await context.NutritionGoals
                .AsNoTracking()
                .Where(g => g.UserId == userId && g.EffectiveDate <= endDate)
                .OrderBy(g => g.EffectiveDate)
                .ThenBy(g => g.Id)
                .ToListAsync(cancellationToken);

            var result = new Dictionary<DateTime, NutritionGoal?>();
            for (var day = startDate; day <= endDate; day = day.AddDays(1))
            {
                NutritionGoal? best = null;
                foreach (var candidate in candidates)
                {
                    if (candidate.EffectiveDate > day) break;
                    if (candidate.DeletedAt.HasValue && candidate.DeletedAt.Value.Date <= day) continue;
                    best = candidate;
                }
                result[day] = best;
            }
            return result;
        }

        /// <summary>
        /// Soft-delete: marks the row deleted as of now rather than removing it,
        /// so it still answers <see cref="ResolveForDateAsync"/> for any date
        /// before today. Recomputes which row (if any) is now "active" so
        /// convenience reads of <see cref="NutritionGoal.IsActive"/> revert
        /// cleanly to whatever applied immediately before the deleted one.
        /// Returns false if no matching, owned row exists.
        /// </summary>
        public static async Task<bool> SoftDeleteAsync(
            TrainingContext context,
            int userId,
            int goalId,
            CancellationToken cancellationToken = default)
        {
            var goal = await context.NutritionGoals
                .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId, cancellationToken);
            if (goal == null)
            {
                return false;
            }

            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            goal.DeletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            // Same authority as ApplyActiveGoalChangeAsync - never set IsActive by
            // hand, always recompute from what actually resolves for today now that
            // this row is deleted.
            await RecomputeActiveFlagAsync(context, userId, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
    }
}
