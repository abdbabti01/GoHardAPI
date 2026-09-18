using System;
using System.Linq;
using System.Threading.Tasks;
using GoHardAPI.Data;
using GoHardAPI.Models;
using GoHardAPI.Services;
using GoHardAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Concurrent nutrition-target changes must follow a defined, deterministic policy:
    /// at most one row per user can ever end up <c>IsActive</c>, enforced at the database
    /// level (<c>IX_NutritionGoals_UserId_Active</c>, added by
    /// <c>AddNutritionGoalEffectiveDate</c>) as a backstop to
    /// <see cref="NutritionTargetService.SetActiveGoalAsync"/>'s own deactivate-then-insert
    /// transaction. Deterministically interleaved (not a wall-clock race): writer A's
    /// transaction is held open past its insert so writer B's concurrent attempt is
    /// guaranteed to still be racing it, then A commits and B's blocked insert is the one
    /// that must observe the conflict.
    /// </summary>
    [Collection(NutritionTargetPostgresCollection.Name)]
    [Trait("Category", "PostgresIntegration")]
    public sealed class NutritionTargetConcurrencyPostgresTests
    {
        private readonly NutritionTargetPostgresFixture _pg;

        public NutritionTargetConcurrencyPostgresTests(NutritionTargetPostgresFixture pg)
        {
            _pg = pg;
        }

        private static NutritionGoal Fields(decimal calories, string name) => new()
        {
            Name = name,
            DailyCalories = calories,
            DailyProtein = 100,
            DailyCarbohydrates = 100,
            DailyFat = 50,
        };

        [DockerRequiredFact]
        public async Task TwoConcurrentSetActiveGoalCalls_ExactlyOneCommits_NeverTwoActiveRows()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");

            var userId = await _pg.SeedUserAsync();
            var jan1 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            await using (var seedCtx = _pg.NewContext())
            {
                await NutritionTargetService.SetActiveGoalAsync(seedCtx, userId, Fields(1500, "Seed"), jan1);
            }

            // Writer A: begin, deactivate the seed row, insert a new active row - but do NOT
            // commit yet, so writer B is guaranteed to still be racing it.
            await using var ctxA = _pg.NewContext();
            await using var txA = await ctxA.Database.BeginTransactionAsync();
            var activeForA = await ctxA.NutritionGoals.Where(g => g.UserId == userId && g.IsActive).ToListAsync();
            foreach (var g in activeForA) g.IsActive = false;
            var newA = new NutritionGoal
            {
                UserId = userId,
                Name = "A",
                DailyCalories = 1800,
                DailyProtein = 100,
                DailyCarbohydrates = 100,
                DailyFat = 50,
                IsActive = true,
                EffectiveDate = NutritionTargetService.NormalizeDate(DateTime.UtcNow),
                CreatedAt = DateTime.UtcNow,
            };
            ctxA.NutritionGoals.Add(newA);
            await ctxA.SaveChangesAsync(); // inserted within A's still-open transaction

            // Writer B: a fully independent call through the real service, racing A. B's
            // deactivate step still sees the ORIGINAL seed row as active (A's uncommitted
            // change is invisible to it), so B also attempts to insert its own active row -
            // colliding with A's uncommitted row at the partial unique index and blocking
            // until A's transaction resolves.
            var bTask = Task.Run(async () =>
            {
                await using var ctxB = _pg.NewContext();
                return await Record.ExceptionAsync(() =>
                    NutritionTargetService.SetActiveGoalAsync(ctxB, userId, Fields(2200, "B"), null));
            });

            // Give B time to reach and block on its INSERT before A commits. A short,
            // generous delay against a local container is the only practical way to
            // guarantee B is actually mid-flight and blocked (rather than not yet started)
            // without a purpose-built test hook inside the production service.
            await Task.Delay(300);

            await txA.CommitAsync();

            var bException = await bTask;

            Assert.NotNull(bException);
            var dbEx = Assert.IsAssignableFrom<DbUpdateException>(bException);
            var pgEx = Assert.IsType<PostgresException>(dbEx.InnerException);
            Assert.Equal("23505", pgEx.SqlState);

            // Exactly one active row for this user, and it is A's - B's failed attempt left
            // no trace (its whole transaction rolled back).
            await using var verifyCtx = _pg.NewContext();
            var activeRows = await verifyCtx.NutritionGoals
                .Where(g => g.UserId == userId && g.IsActive)
                .ToListAsync();
            Assert.Single(activeRows);
            Assert.Equal("A", activeRows[0].Name);

            var bRowExists = await verifyCtx.NutritionGoals.AnyAsync(g => g.UserId == userId && g.Name == "B");
            Assert.False(bRowExists);
        }
    }
}
