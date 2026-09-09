using System;
using System.Linq;
using System.Threading.Tasks;
using GoHardAPI.Controllers;
using GoHardAPI.Data;
using GoHardAPI.Models;
using GoHardAPI.Services;
using GoHardAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Real-PostgreSQL evidence that genuinely overlapping Body-Metrics writes
    /// for ONE user converge on the correct current measurements. Each request
    /// runs on its own <see cref="TrainingContext"/> / connection against the
    /// same database, so they interleave under real transactions - not a
    /// single-connection simulation. There is no persisted summary: current
    /// values are derived on read, and the only <c>User</c> write is the
    /// monotonic, idempotent legacy-scalar retirement.
    /// </summary>
    [Trait("Category", "PostgresIntegration")]
    [Collection(ProfilePostgresCollection.Name)]
    public sealed class ProfileMeasurementConcurrencyPostgresTests
    {
        private readonly ProfilePostgresFixture _pg;

        public ProfileMeasurementConcurrencyPostgresTests(ProfilePostgresFixture pg) => _pg = pg;

        private static BodyMetricsController BodyMetrics(TrainingContext ctx, int userId)
        {
            var c = new BodyMetricsController(ctx);
            c.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth"))
                }
            };
            return c;
        }

        private async Task<int> SeedUser(double? legacyWeight)
        {
            await using var ctx = _pg.NewContext();
            await ctx.Database.ExecuteSqlRawAsync("TRUNCATE \"Users\" RESTART IDENTITY CASCADE;");
            var u = new User
            {
                Name = "U",
                Username = $"u{Guid.NewGuid():N}"[..20],
                Email = $"u{Guid.NewGuid():N}@x.com",
                PasswordHash = "h",
                DateCreated = DateTime.UtcNow,
                UnitPreference = "Metric",
                Weight = legacyWeight,
            };
            ctx.Users.Add(u);
            await ctx.SaveChangesAsync();
            return u.Id;
        }

        private async Task Create(int userId, DateTime recordedAt, decimal weight)
        {
            await using var ctx = _pg.NewContext();
            await BodyMetrics(ctx, userId).CreateBodyMetric(new BodyMetric
            {
                RecordedAt = recordedAt,
                Weight = weight,
            });
        }

        [DockerRequiredFact]
        public async Task overlapping_creates_for_one_user_converge_on_the_newest_row_and_retire_the_legacy_scalar()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser(legacyWeight: 100);

            var jan = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var feb = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

            // Started together, interleaving on independent connections.
            await Task.WhenAll(
                Create(userId, jan, 80m),
                Create(userId, feb, 85m));

            await using var verify = _pg.NewContext();
            Assert.Equal(2, await verify.BodyMetrics.CountAsync(m => m.UserId == userId)); // no lost write

            var user = await verify.Users.AsNoTracking().FirstAsync(u => u.Id == userId);
            Assert.Null(user.Weight); // retired by both creates (idempotent -> converges)

            var current = await new CurrentMeasurementsService(verify).GetForUserAsync(user);
            Assert.Equal(85, current.WeightKg); // February row - the actual newest
        }

        [DockerRequiredFact]
        public async Task an_overlapping_create_and_delete_leave_current_weight_consistent_with_the_surviving_rows()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser(legacyWeight: null);

            var jan = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var feb = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            await Create(userId, jan, 70m);

            int febId;
            await using (var ctx = _pg.NewContext())
            {
                var r = await BodyMetrics(ctx, userId).CreateBodyMetric(new BodyMetric { RecordedAt = feb, Weight = 75m });
                febId = ((BodyMetric)Assert.IsType<CreatedAtActionResult>(r.Result).Value!).Id;
            }

            // Delete the Feb row while another Jan-ish create lands concurrently.
            var mar = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            async Task DeleteFeb()
            {
                await using var ctx = _pg.NewContext();
                await BodyMetrics(ctx, userId).DeleteBodyMetric(febId);
            }
            await Task.WhenAll(DeleteFeb(), Create(userId, mar, 72m));

            await using var verify = _pg.NewContext();
            var user = await verify.Users.AsNoTracking().FirstAsync(u => u.Id == userId);
            var current = await new CurrentMeasurementsService(verify).GetForUserAsync(user);

            var survivingWeights = await verify.BodyMetrics
                .Where(m => m.UserId == userId && m.Weight != null)
                .OrderByDescending(m => m.RecordedAt).ThenByDescending(m => m.Id)
                .Select(m => m.Weight)
                .ToListAsync();

            // Whatever survived, the derived current weight is exactly the newest
            // surviving row's - never the deleted 75, never an invented value.
            Assert.Equal((double?)survivingWeights.FirstOrDefault(), current.WeightKg);
            Assert.DoesNotContain(75d, new[] { current.WeightKg ?? double.NaN });
        }

        [DockerRequiredFact]
        public async Task overlapping_deletes_of_the_last_two_covering_rows_end_with_current_weight_null()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            // Pre-change shape: legacy User.Weight set AND two matching rows, none retired.
            var userId = await SeedUser(legacyWeight: 88);
            int r1, r2;
            await using (var ctx = _pg.NewContext())
            {
                var a = new BodyMetric { UserId = userId, RecordedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), CreatedAt = DateTime.UtcNow, Weight = 88m };
                var b = new BodyMetric { UserId = userId, RecordedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), CreatedAt = DateTime.UtcNow, Weight = 88m };
                ctx.BodyMetrics.AddRange(a, b);
                await ctx.SaveChangesAsync();
                r1 = a.Id; r2 = b.Id;
            }

            async Task Del(int id)
            {
                await using var ctx = _pg.NewContext();
                await BodyMetrics(ctx, userId).DeleteBodyMetric(id);
            }
            await Task.WhenAll(Del(r1), Del(r2));

            await using var verify = _pg.NewContext();
            Assert.Equal(0, await verify.BodyMetrics.CountAsync(m => m.UserId == userId));
            var user = await verify.Users.AsNoTracking().FirstAsync(u => u.Id == userId);

            // Unconditional retire-on-delete: no read-then-write race can leave
            // the removed value behind.
            Assert.Null(user.Weight);
            Assert.Null((await new CurrentMeasurementsService(verify).GetForUserAsync(user)).WeightKg);
        }

        [DockerRequiredFact]
        public async Task a_partial_write_failure_rolls_back_both_the_row_and_the_retirement()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser(legacyWeight: 100);

            // Notes is character varying(500) on PostgreSQL; 600 chars -> the row
            // INSERT fails mid-SaveChanges, AFTER the User retirement UPDATE has
            // been staged. EF's implicit transaction must roll back BOTH.
            await using (var ctx = _pg.NewContext())
            {
                await Assert.ThrowsAnyAsync<Exception>(() =>
                    BodyMetrics(ctx, userId).CreateBodyMetric(new BodyMetric
                    {
                        RecordedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                        Weight = 85m,
                        Notes = new string('x', 600),
                    }));
            }

            await using var verify = _pg.NewContext();
            Assert.Equal(0, await verify.BodyMetrics.CountAsync(m => m.UserId == userId));
            var user = await verify.Users.AsNoTracking().FirstAsync(u => u.Id == userId);
            Assert.Equal(100, user.Weight); // retirement rolled back with the failed row
        }

        [DockerRequiredFact]
        public async Task an_update_that_clears_a_field_racing_a_create_that_fills_it_stays_consistent()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser(legacyWeight: null);

            var jan = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var feb = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            int janId;
            await using (var ctx = _pg.NewContext())
            {
                var r = await BodyMetrics(ctx, userId).CreateBodyMetric(new BodyMetric { RecordedAt = jan, Weight = 70m });
                janId = ((BodyMetric)Assert.IsType<CreatedAtActionResult>(r.Result).Value!).Id;
            }

            async Task ClearJan()
            {
                await using var ctx = _pg.NewContext();
                await BodyMetrics(ctx, userId).UpdateBodyMetric(janId,
                    new BodyMetric { Id = janId, RecordedAt = jan, Weight = null });
            }
            await Task.WhenAll(ClearJan(), Create(userId, feb, 75m));

            await using var verify = _pg.NewContext();
            var user = await verify.Users.AsNoTracking().FirstAsync(u => u.Id == userId);
            var current = await new CurrentMeasurementsService(verify).GetForUserAsync(user);

            var newestSurviving = await verify.BodyMetrics
                .Where(m => m.UserId == userId && m.Weight != null)
                .OrderByDescending(m => m.RecordedAt).ThenByDescending(m => m.Id)
                .Select(m => m.Weight)
                .FirstOrDefaultAsync();

            // Derived weight is exactly the newest surviving non-null row (75 if
            // the Feb create is present), or null if none - never a phantom.
            Assert.Equal((double?)newestSurviving, current.WeightKg);
        }
    }
}
