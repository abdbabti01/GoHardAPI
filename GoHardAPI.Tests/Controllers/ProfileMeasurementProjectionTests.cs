using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GoHardAPI.Controllers;
using GoHardAPI.Data;
using GoHardAPI.DTOs;
using GoHardAPI.Models;
using GoHardAPI.Repositories;
using GoHardAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// A user's current Height/Weight/BodyFatPercentage (and derived BMI) are
    /// DERIVED ON READ from Body Metrics history by
    /// <see cref="CurrentMeasurementsService"/> - there is no persisted summary.
    /// The only persisted <c>User</c> measurement write is legacy-scalar
    /// retirement, committed in the SAME transaction as the row that first covers
    /// a field. Real SQLite so <c>RecordedAt</c> ordering and the <c>Id</c>
    /// tie-break run in the database. Overlapping-writes evidence is in
    /// <see cref="ProfileMeasurementConcurrencyPostgresTests"/>.
    /// </summary>
    public sealed class ProfileMeasurementProjectionTests : IDisposable
    {
        private readonly SqliteConnection _conn;

        public ProfileMeasurementProjectionTests()
        {
            _conn = new SqliteConnection("DataSource=:memory:");
            _conn.Open();
            using var ctx = NewContext();
            ctx.Database.EnsureCreated();
        }

        public void Dispose() => _conn.Dispose();

        private TrainingContext NewContext(IInterceptor? interceptor = null)
        {
            var b = new DbContextOptionsBuilder<TrainingContext>().UseSqlite(_conn);
            if (interceptor is not null) b.AddInterceptors(interceptor);
            return new TrainingContext(b.Options);
        }

        private async Task SeedUser(int id = 1, double? weight = null, double? height = null, double? bodyFat = null)
        {
            await using var ctx = NewContext();
            ctx.Users.Add(new User
            {
                Id = id,
                Name = $"User {id}",
                Username = $"user{id}",
                Email = $"user{id}@example.com",
                PasswordHash = "hash",
                DateCreated = DateTime.UtcNow,
                UnitPreference = "Metric",
                Weight = weight,
                Height = height,
                BodyFatPercentage = bodyFat,
                BMI = CurrentMeasurementsService.ComputeBmi(height, weight),
            });
            await ctx.SaveChangesAsync();
        }

        private static BodyMetricsController NewBodyMetrics(TrainingContext ctx, int userId)
        {
            var controller = new BodyMetricsController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth"))
                }
            };
            return controller;
        }

        private async Task<int> CreateMetric(int userId, DateTime recordedAt,
            decimal? weight = null, decimal? height = null, decimal? bodyFat = null)
        {
            await using var ctx = NewContext();
            var result = await NewBodyMetrics(ctx, userId).CreateBodyMetric(new BodyMetric
            {
                RecordedAt = recordedAt,
                Weight = weight,
                Height = height,
                BodyFatPercentage = bodyFat,
            });
            var created = (BodyMetric)Assert.IsType<CreatedAtActionResult>(result.Result).Value!;
            return created.Id;
        }

        /// <summary>
        /// Insert a Body Metrics row directly, WITHOUT the controller's
        /// legacy-scalar retirement - simulates pre-change production data where
        /// an old <c>PUT /profile</c> set <c>User.X</c> AND created an "Updated
        /// from profile" row for the same value.
        /// </summary>
        private async Task<int> InsertRawMetric(int userId, DateTime recordedAt, decimal? weight = null, decimal? height = null)
        {
            await using var ctx = NewContext();
            var m = new BodyMetric
            {
                UserId = userId,
                RecordedAt = recordedAt,
                CreatedAt = DateTime.UtcNow,
                Weight = weight,
                Height = height,
            };
            ctx.BodyMetrics.Add(m);
            await ctx.SaveChangesAsync();
            return m.Id;
        }

        /// <summary>Raw persisted User row (legacy scalars, unfiltered by the service).</summary>
        private async Task<User> RawUser(int id = 1)
        {
            await using var ctx = NewContext();
            return await ctx.Users.AsNoTracking().FirstAsync(u => u.Id == id);
        }

        /// <summary>Current (derived-on-read) measurements, the value every consumer sees.</summary>
        private async Task<CurrentMeasurements> Current(int id = 1)
        {
            await using var ctx = NewContext();
            var user = await ctx.Users.AsNoTracking().FirstAsync(u => u.Id == id);
            return await new CurrentMeasurementsService(ctx).GetForUserAsync(user);
        }

        // ---- per-field selection --------------------------------------------------

        [Fact]
        public async Task current_weight_is_the_newest_recorded_non_null_row()
        {
            await SeedUser();
            await CreateMetric(1, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), weight: 80m);
            await CreateMetric(1, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), weight: 77m);

            Assert.Equal(77, (await Current()).WeightKg);
        }

        [Fact]
        public async Task a_weight_only_row_moves_only_weight_height_stays_from_an_older_row()
        {
            await SeedUser();
            await CreateMetric(1, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), height: 180m);
            await CreateMetric(1, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), weight: 82m);

            var cur = await Current();
            Assert.Equal(82, cur.WeightKg);
            Assert.Equal(180, cur.HeightCm);
            Assert.Equal(CurrentMeasurementsService.ComputeBmi(180, 82), cur.Bmi);
        }

        [Fact]
        public async Task a_backdated_row_does_not_win()
        {
            await SeedUser();
            await CreateMetric(1, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), weight: 77m);
            await CreateMetric(1, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), weight: 90m); // backdated

            Assert.Equal(77, (await Current()).WeightKg);
        }

        [Fact]
        public async Task same_date_rows_break_the_tie_by_newer_id()
        {
            await SeedUser();
            var sameDay = new DateTime(2026, 5, 5, 0, 0, 0, DateTimeKind.Utc);
            await CreateMetric(1, sameDay, weight: 70m);
            await CreateMetric(1, sameDay, weight: 71m); // higher Id -> wins

            Assert.Equal(71, (await Current()).WeightKg);
        }

        [Fact]
        public async Task body_fat_is_resolved_independently_like_weight_and_height()
        {
            await SeedUser();
            await CreateMetric(1, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), bodyFat: 22m);
            await CreateMetric(1, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), bodyFat: 18.5m);
            await CreateMetric(1, new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), weight: 79m); // no bodyFat

            Assert.Equal(18.5, (await Current()).BodyFatPercentage);
        }

        // ---- legacy scalar retirement & provenance ------------------------------

        [Fact]
        public async Task the_first_row_that_covers_a_field_retires_that_legacy_scalar_only()
        {
            await SeedUser(weight: 100, height: 175, bodyFat: 25);

            await CreateMetric(1, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), weight: 85m);

            var raw = await RawUser();
            Assert.Null(raw.Weight);            // retired - a row now covers weight
            Assert.Equal(175, raw.Height);      // untouched - no row covers height
            Assert.Equal(25, raw.BodyFatPercentage);
            Assert.Null(raw.BMI);               // vestigial column cleared

            var cur = await Current();
            Assert.Equal(85, cur.WeightKg);     // history
            Assert.Equal(175, cur.HeightCm);    // provably-legacy fallback
            Assert.Equal(25, cur.BodyFatPercentage);
        }

        [Fact]
        public async Task a_legacy_value_with_no_history_for_that_field_is_preserved()
        {
            await SeedUser(height: 178);
            Assert.Equal(178, (await Current()).HeightCm);

            // Log unrelated fields; height is still never covered by a row.
            await CreateMetric(1, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), weight: 80m, bodyFat: 20m);

            Assert.Equal(178, (await Current()).HeightCm);
            Assert.Equal(178, (await RawUser()).Height); // never retired
        }

        [Fact]
        public async Task deleting_the_last_row_that_covers_a_field_yields_null_not_the_removed_value()
        {
            await SeedUser(weight: 100); // legacy
            var id = await CreateMetric(1, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), weight: 85m);
            Assert.Equal(85, (await Current()).WeightKg);
            Assert.Null((await RawUser()).Weight); // retired on create

            await using (var ctx = NewContext())
                Assert.IsType<NoContentResult>(await NewBodyMetrics(ctx, 1).DeleteBodyMetric(id));

            // The removed 85 is NOT retained, and the legacy 100 is NOT
            // resurrected (it was retired when weight first became covered).
            Assert.Null((await Current()).WeightKg);
        }

        [Fact]
        public async Task deleting_the_latest_row_falls_back_to_an_older_surviving_row()
        {
            await SeedUser();
            await CreateMetric(1, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), weight: 80m);
            var latest = await CreateMetric(1, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), weight: 75m);
            Assert.Equal(75, (await Current()).WeightKg);

            await using (var ctx = NewContext())
                Assert.IsType<NoContentResult>(await NewBodyMetrics(ctx, 1).DeleteBodyMetric(latest));

            Assert.Equal(80, (await Current()).WeightKg); // surviving older row
        }

        [Fact]
        public async Task editing_a_row_to_clear_a_field_falls_back_to_an_older_row_and_never_un_retires()
        {
            await SeedUser(weight: 100); // legacy
            await CreateMetric(1, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), weight: 85m);
            var latest = await CreateMetric(1, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), weight: 80m);
            Assert.Equal(80, (await Current()).WeightKg);

            await using (var ctx = NewContext())
            {
                var edit = new BodyMetric
                {
                    Id = latest,
                    RecordedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    Weight = null, // cleared
                };
                Assert.IsType<NoContentResult>(await NewBodyMetrics(ctx, 1).UpdateBodyMetric(latest, edit));
            }

            Assert.Equal(85, (await Current()).WeightKg); // older row, NOT the legacy 100
            Assert.Null((await RawUser()).Weight);        // still retired
        }

        [Fact]
        public async Task deleting_a_pre_change_row_that_matches_a_still_present_legacy_scalar_retires_it()
        {
            // Pre-change data: User.Weight = 75 AND a matching history row, but
            // no retirement ever ran (old code path).
            await SeedUser(weight: 75);
            var id = await InsertRawMetric(1, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), weight: 75m);
            Assert.Equal(75, (await Current()).WeightKg);   // from history
            Assert.Equal(75, (await RawUser()).Weight);     // still present, un-retired

            await using (var ctx = NewContext())
                Assert.IsType<NoContentResult>(await NewBodyMetrics(ctx, 1).DeleteBodyMetric(id));

            // The removed value is NOT kept: the ambiguous legacy scalar is
            // retired because the deleted row was the last one covering weight.
            Assert.Null((await Current()).WeightKg);
            Assert.Null((await RawUser()).Weight);
        }

        [Fact]
        public async Task deleting_a_row_does_not_touch_a_legacy_scalar_for_a_field_that_row_never_covered()
        {
            // Genuine legacy height, no history ever covers it.
            await SeedUser(height: 182);
            var id = await CreateMetric(1, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), weight: 80m);
            Assert.Equal(182, (await Current()).HeightCm);

            await using (var ctx = NewContext())
                Assert.IsType<NoContentResult>(await NewBodyMetrics(ctx, 1).DeleteBodyMetric(id));

            Assert.Equal(182, (await Current()).HeightCm);   // preserved
            Assert.Equal(182, (await RawUser()).Height);
        }

        [Fact]
        public async Task deleting_one_of_several_covering_rows_still_yields_a_surviving_row_and_eagerly_retires_the_scalar()
        {
            await SeedUser(weight: 60); // legacy, un-retired (raw inserts below)
            await InsertRawMetric(1, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), weight: 70m);
            var newer = await InsertRawMetric(1, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), weight: 72m);
            Assert.Equal(72, (await Current()).WeightKg);

            await using (var ctx = NewContext())
                Assert.IsType<NoContentResult>(await NewBodyMetrics(ctx, 1).DeleteBodyMetric(newer));

            Assert.Equal(70, (await Current()).WeightKg);  // surviving older row wins
            Assert.Null((await RawUser()).Weight);         // legacy scalar retired unconditionally (harmless: shadowed)
        }

        [Fact]
        public async Task a_zero_or_negative_history_value_is_skipped_never_surfaced()
        {
            await SeedUser(weight: 72); // genuine legacy, no covering row yet
            await InsertRawMetric(1, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), weight: 80m);
            await InsertRawMetric(1, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), weight: 0m); // bad entry

            // The 0 row is skipped; the older 80 row is the current weight.
            Assert.Equal(80, (await Current()).WeightKg);
        }

        [Fact]
        public async Task a_zero_value_does_not_retire_the_legacy_scalar_via_the_controller()
        {
            await SeedUser(height: 175); // genuine legacy
            await using (var ctx = NewContext())
            {
                var r = await NewBodyMetrics(ctx, 1).CreateBodyMetric(new BodyMetric
                {
                    RecordedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    Height = 0m, // not a usable measurement
                });
                Assert.IsType<CreatedAtActionResult>(r.Result);
            }

            Assert.Equal(175, (await RawUser()).Height);   // not retired by a 0 value
            Assert.Equal(175, (await Current()).HeightCm);  // still the legacy value
        }

        [Fact]
        public async Task editing_a_pre_change_rows_only_weight_away_retires_the_still_present_legacy_scalar()
        {
            // Pre-change data: User.Weight = 90 AND a matching un-retired row.
            await SeedUser(weight: 90);
            var id = await InsertRawMetric(1, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), weight: 90m);
            Assert.Equal(90, (await RawUser()).Weight);

            await using (var ctx = NewContext())
            {
                var edit = new BodyMetric
                {
                    Id = id,
                    RecordedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    Weight = null, // clear the row's only weight
                };
                Assert.IsType<NoContentResult>(await NewBodyMetrics(ctx, 1).UpdateBodyMetric(id, edit));
            }

            // The row covered weight BEFORE the edit, so the ambiguous legacy
            // scalar is retired even though the post-edit row is weight-less.
            Assert.Null((await RawUser()).Weight);
            Assert.Null((await Current()).WeightKg);
        }

        [Fact]
        public async Task editing_a_row_to_add_a_value_retires_the_legacy_scalar()
        {
            await SeedUser(height: 170); // legacy, never covered
            var id = await CreateMetric(1, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), weight: 80m);
            Assert.Equal(170, (await Current()).HeightCm); // legacy still

            await using (var ctx = NewContext())
            {
                var edit = new BodyMetric
                {
                    Id = id,
                    RecordedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    Weight = 80m,
                    Height = 172m, // now the row covers height
                };
                Assert.IsType<NoContentResult>(await NewBodyMetrics(ctx, 1).UpdateBodyMetric(id, edit));
            }

            Assert.Equal(172, (await Current()).HeightCm);
            Assert.Null((await RawUser()).Height); // retired by the edit

            await using (var ctx = NewContext())
                Assert.IsType<NoContentResult>(await NewBodyMetrics(ctx, 1).DeleteBodyMetric(id));

            Assert.Null((await Current()).HeightCm); // 170 is NOT resurrected
        }

        // ---- atomicity ---------------------------------------------------------

        [Fact]
        public async Task create_persists_the_row_and_the_retirement_in_a_single_save()
        {
            await SeedUser(weight: 100);
            var counter = new CountSavesInterceptor();

            await using (var ctx = NewContext(counter))
            {
                await NewBodyMetrics(ctx, 1).CreateBodyMetric(new BodyMetric
                {
                    RecordedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    Weight = 85m,
                });
            }

            // One SaveChanges => the row + the legacy-scalar retirement share EF's
            // implicit transaction: both commit or neither does.
            Assert.Equal(1, counter.Count);
            Assert.Null((await RawUser()).Weight);
            Assert.Equal(85, (await Current()).WeightKg);
        }

        [Fact]
        public async Task a_failed_save_persists_neither_the_row_nor_the_retirement()
        {
            await SeedUser(weight: 100);

            await using (var ctx = NewContext(new ThrowOnSaveInterceptor()))
            {
                await Assert.ThrowsAnyAsync<Exception>(() =>
                    NewBodyMetrics(ctx, 1).CreateBodyMetric(new BodyMetric
                    {
                        RecordedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                        Weight = 85m,
                    }));
            }

            await using var verify = NewContext();
            Assert.Empty(verify.BodyMetrics);               // row not persisted
            Assert.Equal(100, (await RawUser()).Weight);    // retirement not persisted
            Assert.Equal(100, (await Current()).WeightKg);  // consistent
        }

        // ---- consumers agree -------------------------------------------------

        [Fact]
        public async Task profile_response_reflects_the_derived_current_values()
        {
            await SeedUser();
            await CreateMetric(1, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), weight: 81m, height: 176m);

            await using var ctx = NewContext();
            var body = Assert.IsType<ProfileResponse>(
                Assert.IsType<OkObjectResult>((await NewProfile(ctx, 1).GetProfile()).Result).Value);

            Assert.Equal(81, body.Weight);
            Assert.Equal(176, body.Height);
            Assert.Equal(CurrentMeasurementsService.ComputeBmi(176, 81), body.BMI);
        }

        [Fact]
        public async Task profile_and_a_second_reader_agree_on_a_same_day_two_row_case()
        {
            await SeedUser();
            var sameDay = new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc);
            await CreateMetric(1, sameDay, weight: 90m);
            await CreateMetric(1, sameDay, weight: 92m); // newer Id -> the current value

            await using var ctx = NewContext();
            var body = Assert.IsType<ProfileResponse>(
                Assert.IsType<OkObjectResult>((await NewProfile(ctx, 1).GetProfile()).Result).Value);

            var user = await ctx.Users.AsNoTracking().FirstAsync(u => u.Id == 1);
            var second = await new CurrentMeasurementsService(ctx).GetForUserAsync(user);

            Assert.Equal(92, body.Weight);
            Assert.Equal(92, second.WeightKg); // no divergent latest-row pick
        }

        // ---- isolation -----------------------------------------------------

        [Fact]
        public async Task another_users_data_and_unrelated_fields_are_untouched()
        {
            await SeedUser(1, weight: 50);
            await SeedUser(2, weight: 60);

            await CreateMetric(1, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), weight: 70m);

            Assert.Equal(70, (await Current(1)).WeightKg);
            Assert.Equal(60, (await Current(2)).WeightKg);   // user 2 derived independently
            Assert.Equal(60, (await RawUser(2)).Weight);     // user 2 legacy scalar untouched

            var u1 = await RawUser(1);
            Assert.Equal("User 1", u1.Name);
            Assert.Equal("user1@example.com", u1.Email);
            Assert.Equal("Metric", u1.UnitPreference);
        }

        private static ProfileController NewProfile(TrainingContext ctx, int userId)
        {
            var controller = new ProfileController(
                ctx,
                new FileUploadService(new StubEnv()),
                new UserRepository(ctx),
                new CurrentMeasurementsService(ctx));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth"))
                }
            };
            return controller;
        }

        private sealed class CountSavesInterceptor : SaveChangesInterceptor
        {
            public int Count { get; private set; }

            public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
                DbContextEventData eventData, InterceptionResult<int> result,
                CancellationToken cancellationToken = default)
            {
                Count++;
                return base.SavingChangesAsync(eventData, result, cancellationToken);
            }
        }

        private sealed class ThrowOnSaveInterceptor : SaveChangesInterceptor
        {
            public override InterceptionResult<int> SavingChanges(
                DbContextEventData eventData, InterceptionResult<int> result)
                => throw new InvalidOperationException("boom");

            public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
                DbContextEventData eventData, InterceptionResult<int> result,
                CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("boom");
        }

        private sealed class StubEnv : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
        {
            public string WebRootPath { get; set; } = System.IO.Path.GetTempPath();
            public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; }
                = new Microsoft.Extensions.FileProviders.NullFileProvider();
            public string ApplicationName { get; set; } = "Tests";
            public string ContentRootPath { get; set; } = System.IO.Path.GetTempPath();
            public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
                = new Microsoft.Extensions.FileProviders.NullFileProvider();
            public string EnvironmentName { get; set; } = "Test";
        }
    }
}
