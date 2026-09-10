using System;
using System.Linq;
using System.Threading.Tasks;
using GoHardAPI.Controllers;
using GoHardAPI.Data;
using GoHardAPI.DTOs;
using GoHardAPI.Models;
using GoHardAPI.Repositories;
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
    /// Real-PostgreSQL evidence that two accounts racing for the SAME free
    /// username end with exactly one owner: one request gets 200, the other 409
    /// (never 500, never a shared username). The <c>IX_Users_Username</c> unique
    /// index is the guarantee; the controller's pre-check can lose the race, and
    /// <see cref="UniqueConstraintViolation"/> maps only that violation to 409.
    /// </summary>
    [Trait("Category", "PostgresIntegration")]
    [Collection(ProfilePostgresCollection.Name)]
    public sealed class ProfileUsernameConcurrencyPostgresTests
    {
        private readonly ProfilePostgresFixture _pg;

        public ProfileUsernameConcurrencyPostgresTests(ProfilePostgresFixture pg) => _pg = pg;

        private static ProfileController Controller(TrainingContext ctx, int userId)
        {
            var controller = new ProfileController(
                ctx, TestProfilePhotoStorage.UnusedService(), new UserRepository(ctx),
                new CurrentMeasurementsService(ctx), TestScopeFactory.Unused());
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

        private static UpdateProfileRequest ClaimUsername(string username) =>
            new(
                Name: null, Username: username, Bio: null, DateOfBirth: null, Gender: null,
                Height: null, Weight: null, TargetWeight: null, BodyFatPercentage: null,
                ExperienceLevel: null, PrimaryGoal: null, Goals: null, UnitPreference: null,
                ThemePreference: null, FavoriteExercises: null);

        [DockerRequiredFact]
        public async Task two_accounts_racing_for_the_same_username_end_with_exactly_one_owner()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");

            const string wanted = "raceonly";
            int userA, userB;

            await using (var seed = _pg.NewContext())
            {
                // Fresh table per test run - EnsureCreated built the schema once.
                await seed.Database.ExecuteSqlRawAsync(
                    "TRUNCATE \"Users\" RESTART IDENTITY CASCADE;");
                var a = new User { Name = "A", Username = "a_start", Email = $"a{Guid.NewGuid():N}@x.com", PasswordHash = "h", DateCreated = DateTime.UtcNow, UnitPreference = "Metric" };
                var b = new User { Name = "B", Username = "b_start", Email = $"b{Guid.NewGuid():N}@x.com", PasswordHash = "h", DateCreated = DateTime.UtcNow, UnitPreference = "Metric" };
                seed.Users.AddRange(a, b);
                await seed.SaveChangesAsync();
                userA = a.Id;
                userB = b.Id;
            }

            async Task<IActionResult> Claim(int userId)
            {
                await using var ctx = _pg.NewContext();
                var result = await Controller(ctx, userId).UpdateProfile(ClaimUsername(wanted));
                return result.Result!;
            }

            var results = await Task.WhenAll(Claim(userA), Claim(userB));

            var oks = results.OfType<OkObjectResult>().ToList();
            var conflicts = results.OfType<ConflictObjectResult>().ToList();

            Assert.Single(oks);
            Assert.Single(conflicts);
            Assert.DoesNotContain(results, r => r is ObjectResult o && o.StatusCode >= 500);

            await using var verify = _pg.NewContext();
            var owners = await verify.Users.Where(u => u.Username == wanted).ToListAsync();
            Assert.Single(owners);
        }

        [DockerRequiredFact]
        public async Task the_real_npgsql_unique_violation_is_recognised_only_for_the_username_index()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");

            await using (var seed = _pg.NewContext())
            {
                await seed.Database.ExecuteSqlRawAsync("TRUNCATE \"Users\" RESTART IDENTITY CASCADE;");
                seed.Users.Add(new User { Name = "A", Username = "dupe", Email = $"a{Guid.NewGuid():N}@x.com", PasswordHash = "h", DateCreated = DateTime.UtcNow, UnitPreference = "Metric" });
                await seed.SaveChangesAsync();
            }

            var ex = await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await using var ctx = _pg.NewContext();
                ctx.Users.Add(new User { Name = "B", Username = "dupe", Email = $"b{Guid.NewGuid():N}@x.com", PasswordHash = "h", DateCreated = DateTime.UtcNow, UnitPreference = "Metric" });
                await ctx.SaveChangesAsync();
            });

            // Positive: exact Npgsql 23505 + ConstraintName == IX_Users_Username.
            Assert.True(UniqueConstraintViolation.Matches(ex, "IX_Users_Username", "Users.Username"));
            // Negative: a different index name must not match this exception,
            // so an unrelated DbUpdateException is never reported as "username taken".
            Assert.False(UniqueConstraintViolation.Matches(ex, "IX_Users_Email"));
            Assert.False(UniqueConstraintViolation.Matches(ex, "IX_Users_Usernam")); // prefix, not equal
        }
    }
}
