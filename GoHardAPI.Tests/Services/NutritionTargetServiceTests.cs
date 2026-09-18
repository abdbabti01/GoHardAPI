using System;
using System.Threading.Tasks;
using GoHardAPI.Data;
using GoHardAPI.Models;
using GoHardAPI.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GoHardAPI.Tests.Services
{
    /// <summary>
    /// Historical nutrition targets must stay historical: a target change must never
    /// mutate an existing row's macro fields in place, and resolving a date's target
    /// must always pick the row that actually applied on that date - never "today's"
    /// row applied retroactively, and never a guessed/backfilled value for a date
    /// before any target existed. Real SQLite (not EF InMemory) so
    /// <c>Database.BeginTransactionAsync</c> exercises a real transaction.
    /// </summary>
    public class NutritionTargetServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly TrainingContext _context;
        private const int UserId = 1;

        public NutritionTargetServiceTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            _context = new TrainingContext(
                new DbContextOptionsBuilder<TrainingContext>().UseSqlite(_connection).Options);
            _context.Database.EnsureCreated();
            _context.Users.Add(new User { Id = UserId, Name = "u", Username = "u", Email = "u@x.com", PasswordHash = "h" });
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        private static NutritionGoal Fields(decimal calories, string name = "G") => new()
        {
            Name = name,
            DailyCalories = calories,
            DailyProtein = 100,
            DailyCarbohydrates = 100,
            DailyFat = 50,
        };

        [Fact]
        public async Task SetActiveGoalAsync_NeverMutatesThePreviousRowsFields()
        {
            var jan1 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var goalA = await NutritionTargetService.SetActiveGoalAsync(_context, UserId, Fields(1800, "A"), jan1);

            var mar1 = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            await NutritionTargetService.SetActiveGoalAsync(_context, UserId, Fields(2200, "B"), mar1);

            // A's own stored row is untouched - only IsActive flips.
            var storedA = await _context.NutritionGoals.AsNoTracking().FirstAsync(g => g.Id == goalA.Id);
            Assert.Equal(1800, storedA.DailyCalories);
            Assert.False(storedA.IsActive);
        }

        [Fact]
        public async Task SetActiveGoalAsync_ABackdatedCorrectionInsertedAfterANewerRow_DoesNotStealIsActive()
        {
            var jan1 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var mar1 = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            await NutritionTargetService.SetActiveGoalAsync(_context, UserId, Fields(1800, "A"), jan1);
            var goalB = await NutritionTargetService.SetActiveGoalAsync(_context, UserId, Fields(2200, "B"), mar1);

            // A backdated correction inserted AFTER B, but effective BEFORE it
            // (Feb 1) - since real "today" is well after March 1, B is still the
            // row that actually applies today. Inserting C must not blindly steal
            // IsActive just because it's the most recently-written row.
            var feb1 = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            var goalC = await NutritionTargetService.SetActiveGoalAsync(_context, UserId, Fields(1900, "C"), feb1);

            var storedB = await _context.NutritionGoals.AsNoTracking().FirstAsync(g => g.Id == goalB.Id);
            var storedC = await _context.NutritionGoals.AsNoTracking().FirstAsync(g => g.Id == goalC.Id);
            Assert.True(storedB.IsActive);
            Assert.False(storedC.IsActive);

            // ResolveForDateAsync for a date between C and B correctly picks up C -
            // it's still a real, correctly-ordered row, just not "today's".
            var midFeb = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc);
            var resolvedMidFeb = await NutritionTargetService.ResolveForDateAsync(_context, UserId, midFeb);
            Assert.Equal(goalC.Id, resolvedMidFeb!.Id);
        }

        [Fact]
        public async Task SetActiveGoalAsync_AFutureDatedGoal_DoesNotBecomeActiveBeforeItsEffectiveDate()
        {
            var jan1 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var goalA = await NutritionTargetService.SetActiveGoalAsync(_context, UserId, Fields(1800, "A"), jan1);

            var farFuture = DateTime.UtcNow.Date.AddDays(30);
            var goalFuture = await NutritionTargetService.SetActiveGoalAsync(_context, UserId, Fields(2500, "Future"), farFuture);

            var storedA = await _context.NutritionGoals.AsNoTracking().FirstAsync(g => g.Id == goalA.Id);
            var storedFuture = await _context.NutritionGoals.AsNoTracking().FirstAsync(g => g.Id == goalFuture.Id);
            Assert.True(storedA.IsActive);
            Assert.False(storedFuture.IsActive);

            // Today still resolves to A, not the future-dated row.
            var today = NutritionTargetService.NormalizeDate(DateTime.UtcNow);
            var resolvedToday = await NutritionTargetService.ResolveForDateAsync(_context, UserId, today);
            Assert.Equal(goalA.Id, resolvedToday!.Id);
        }

        [Fact]
        public async Task ResolveForDateAsync_PastDatesKeepTheOldTarget_AfterANewerOneIsSet()
        {
            var jan1 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            await NutritionTargetService.SetActiveGoalAsync(_context, UserId, Fields(1800, "A"), jan1);
            var mar1 = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            await NutritionTargetService.SetActiveGoalAsync(_context, UserId, Fields(2200, "B"), mar1);

            // A date between A and B's effective dates still resolves to A.
            var february = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc);
            var resolvedFeb = await NutritionTargetService.ResolveForDateAsync(_context, UserId, february);
            Assert.NotNull(resolvedFeb);
            Assert.Equal(1800, resolvedFeb!.DailyCalories);

            // Today (after B's effective date) resolves to B.
            var april = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            var resolvedApril = await NutritionTargetService.ResolveForDateAsync(_context, UserId, april);
            Assert.NotNull(resolvedApril);
            Assert.Equal(2200, resolvedApril!.DailyCalories);
        }

        [Fact]
        public async Task ResolveForDateAsync_ResolvesTheHistoricalTarget_RegardlessOfWhetherAMealLogExistsForThatDay()
        {
            var jan1 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            await NutritionTargetService.SetActiveGoalAsync(_context, UserId, Fields(1800, "A"), jan1);

            // No MealLog row is ever created for this user/date in this test -
            // resolution must not depend on one existing.
            var farFuture = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
            var resolved = await NutritionTargetService.ResolveForDateAsync(_context, UserId, farFuture);

            Assert.NotNull(resolved);
            Assert.Equal(1800, resolved!.DailyCalories);
            Assert.Empty(_context.MealLogs);
        }

        [Fact]
        public async Task ResolveForDateAsync_BeforeTheFirstKnownTarget_ReturnsNull_NotAGuess()
        {
            var mar1 = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            await NutritionTargetService.SetActiveGoalAsync(_context, UserId, Fields(2200, "B"), mar1);

            var beforeAnyTarget = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc);
            var resolved = await NutritionTargetService.ResolveForDateAsync(_context, UserId, beforeAnyTarget);

            Assert.Null(resolved);
        }

        [Fact]
        public async Task ResolveForDateAsync_TwoVersionsShareTheSameEffectiveDate_HigherIdWinsDeterministically()
        {
            // Two edits made on the same calendar day (e.g. the user corrected a typo
            // minutes after first setting it) - a real, reachable tie, not a synthetic
            // edge case. The partial unique IsActive index says nothing about which
            // historical version resolves for a shared date; ORDER BY EffectiveDate DESC,
            // Id DESC is the actual, documented tie-break authority (see
            // AddNutritionGoalEffectiveDate's doc comment).
            var jan1 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var goalA = await NutritionTargetService.SetActiveGoalAsync(_context, UserId, Fields(1800, "A"), jan1);
            var goalB = await NutritionTargetService.SetActiveGoalAsync(_context, UserId, Fields(1900, "B"), jan1);
            Assert.True(goalB.Id > goalA.Id);

            var resolved = await NutritionTargetService.ResolveForDateAsync(_context, UserId, jan1);
            Assert.Equal(goalB.Id, resolved!.Id);
            Assert.Equal(1900, resolved.DailyCalories);

            // IsActive follows the identical tie-break - B (higher id), not A, ends up
            // flagged active, consistent with what ResolveForDateAsync(today) returns.
            var storedA = await _context.NutritionGoals.AsNoTracking().FirstAsync(g => g.Id == goalA.Id);
            var storedB = await _context.NutritionGoals.AsNoTracking().FirstAsync(g => g.Id == goalB.Id);
            Assert.False(storedA.IsActive);
            Assert.True(storedB.IsActive);
        }

        [Fact]
        public async Task SoftDeleteAsync_RevertsTodayToWhateverAppliedBefore_WithoutErasingEitherRow()
        {
            var jan1 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var goalA = await NutritionTargetService.SetActiveGoalAsync(_context, UserId, Fields(1800, "A"), jan1);
            var goalB = await NutritionTargetService.SetActiveGoalAsync(_context, UserId, Fields(2200, "B"), jan1.AddDays(1));

            var deleted = await NutritionTargetService.SoftDeleteAsync(_context, UserId, goalB.Id);
            Assert.True(deleted);

            // Both rows still exist - nothing erased.
            Assert.Equal(2, await _context.NutritionGoals.CountAsync(g => g.UserId == UserId));

            var today = NutritionTargetService.NormalizeDate(DateTime.UtcNow);
            var current = await NutritionTargetService.ResolveForDateAsync(_context, UserId, today);
            Assert.NotNull(current);
            Assert.Equal(goalA.Id, current!.Id);

            var storedA = await _context.NutritionGoals.AsNoTracking().FirstAsync(g => g.Id == goalA.Id);
            Assert.True(storedA.IsActive);
        }

        [Fact]
        public async Task SoftDeleteAsync_ADateBeforeTheDeletion_StillSeesTheDeletedTarget()
        {
            var jan1 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var goal = await NutritionTargetService.SetActiveGoalAsync(_context, UserId, Fields(1800, "A"), jan1);

            await NutritionTargetService.SoftDeleteAsync(_context, UserId, goal.Id);

            // A historical date before "now" (the delete timestamp) is unaffected.
            var resolved = await NutritionTargetService.ResolveForDateAsync(_context, UserId, jan1);
            Assert.NotNull(resolved);
            Assert.Equal(goal.Id, resolved!.Id);
        }

        [Fact]
        public async Task SoftDeleteAsync_NoMatchingOwnedRow_ReturnsFalse()
        {
            var deleted = await NutritionTargetService.SoftDeleteAsync(_context, UserId, 9999);
            Assert.False(deleted);
        }

        [Fact]
        public async Task ResolveForDateRangeAsync_MatchesPerDateResolution_AcrossAChange()
        {
            var jan1 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var goalA = await NutritionTargetService.SetActiveGoalAsync(_context, UserId, Fields(1800, "A"), jan1);
            var jan10 = jan1.AddDays(9);
            var goalB = await NutritionTargetService.SetActiveGoalAsync(_context, UserId, Fields(2200, "B"), jan10);

            var range = await NutritionTargetService.ResolveForDateRangeAsync(_context, UserId, jan1, jan1.AddDays(20));

            // Before B existed: A.
            Assert.Equal(goalA.Id, range[jan1]!.Id);
            Assert.Equal(goalA.Id, range[jan10.AddDays(-1)]!.Id);
            // From B's effective date onward: B.
            Assert.Equal(goalB.Id, range[jan10]!.Id);
            Assert.Equal(goalB.Id, range[jan1.AddDays(20)]!.Id);
            // Before A existed at all: no target.
            var beforeA = await NutritionTargetService.ResolveForDateRangeAsync(_context, UserId, jan1.AddDays(-2), jan1.AddDays(-1));
            Assert.Null(beforeA[jan1.AddDays(-2)]);
        }

        [Fact]
        public async Task ResolveForDateRangeAsync_ADeletionOnlyAffectsDatesFromTheDeletionOnward()
        {
            var jan1 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var goalA = await NutritionTargetService.SetActiveGoalAsync(_context, UserId, Fields(1800, "A"), jan1);
            var jan10 = jan1.AddDays(9);
            var goalB = await NutritionTargetService.SetActiveGoalAsync(_context, UserId, Fields(2200, "B"), jan10);

            // Deleted "now" (far after jan1-jan21 in this test's real clock) - every date in
            // the past range below was already resolved before the deletion happened, so B
            // must still apply to all of them. Only "today" (per the other SoftDeleteAsync
            // test) reverts to A.
            await NutritionTargetService.SoftDeleteAsync(_context, UserId, goalB.Id);

            var range = await NutritionTargetService.ResolveForDateRangeAsync(_context, UserId, jan1, jan1.AddDays(20));

            Assert.Equal(goalA.Id, range[jan1]!.Id);
            Assert.Equal(goalB.Id, range[jan10]!.Id);
            Assert.Equal(goalB.Id, range[jan1.AddDays(20)]!.Id);
        }
    }
}
