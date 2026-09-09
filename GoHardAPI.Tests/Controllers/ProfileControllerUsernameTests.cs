using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Contract tests for username on <c>GET /profile</c> and
    /// <c>PUT /profile</c>. Backed by real SQLite (<c>EnsureCreated</c> builds the
    /// model's <c>IX_Users_Username</c> unique index) so the pre-check, the
    /// no-op-on-same-value path, and "no synthetic BodyMetric row" are all
    /// exercised against a relational store. Real concurrent-claim racing is in
    /// <see cref="ProfileUsernameConcurrencyPostgresTests"/>.
    /// </summary>
    public sealed class ProfileControllerUsernameTests : IDisposable
    {
        private readonly SqliteConnection _conn;

        public ProfileControllerUsernameTests()
        {
            _conn = new SqliteConnection("DataSource=:memory:");
            _conn.Open();
            using var ctx = NewContext();
            ctx.Database.EnsureCreated();
        }

        public void Dispose() => _conn.Dispose();

        private TrainingContext NewContext() =>
            new(new DbContextOptionsBuilder<TrainingContext>().UseSqlite(_conn).Options);

        private static ProfileController NewController(TrainingContext ctx, int userId)
        {
            var controller = new ProfileController(
                ctx,
                TestProfilePhotoStorage.UnusedService(),
                new UserRepository(ctx),
                new CurrentMeasurementsService(ctx),
                TestScopeFactory.Unused());

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) },
                        "TestAuth"))
                }
            };
            return controller;
        }

        private async Task<User> SeedUser(int id, string username)
        {
            await using var ctx = NewContext();
            var user = new User
            {
                Id = id,
                Name = $"User {id}",
                Username = username,
                Email = $"user{id}@example.com",
                PasswordHash = "hash",
                DateCreated = DateTime.UtcNow,
                UnitPreference = "Metric",
            };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();
            return user;
        }

        private static UpdateProfileRequest Update(string? username = null, string? name = null) =>
            new(
                Name: name, Username: username, Bio: null, DateOfBirth: null, Gender: null,
                Height: null, Weight: null, TargetWeight: null, BodyFatPercentage: null,
                ExperienceLevel: null, PrimaryGoal: null, Goals: null, UnitPreference: null,
                ThemePreference: null, FavoriteExercises: null);

        [Fact]
        public async Task GetProfile_includes_the_current_username()
        {
            await SeedUser(1, "alice");
            await using var ctx = NewContext();

            var result = await NewController(ctx, 1).GetProfile();

            var body = Assert.IsType<ProfileResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
            Assert.Equal("alice", body.Username);
        }

        [Fact]
        public async Task UpdateProfile_with_no_username_leaves_the_current_one_untouched()
        {
            await SeedUser(1, "alice");
            await using var ctx = NewContext();

            var result = await NewController(ctx, 1).UpdateProfile(Update(name: "Alice Renamed"));

            var body = Assert.IsType<ProfileResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
            Assert.Equal("alice", body.Username);
            Assert.Equal("Alice Renamed", body.Name);

            await using var verify = NewContext();
            Assert.Equal("alice", (await verify.Users.FindAsync(1))!.Username);
        }

        [Fact]
        public async Task UpdateProfile_resubmitting_the_same_username_succeeds_and_changes_nothing()
        {
            await SeedUser(1, "alice");
            await using var ctx = NewContext();

            var result = await NewController(ctx, 1).UpdateProfile(Update(username: "alice"));

            var body = Assert.IsType<ProfileResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
            Assert.Equal("alice", body.Username);
        }

        [Fact]
        public async Task UpdateProfile_to_a_free_username_persists_and_a_later_GET_agrees()
        {
            await SeedUser(1, "alice");
            await using (var ctx = NewContext())
            {
                var result = await NewController(ctx, 1).UpdateProfile(Update(username: "alice_2"));
                var body = Assert.IsType<ProfileResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
                Assert.Equal("alice_2", body.Username);
            }

            await using var getCtx = NewContext();
            var getResult = await NewController(getCtx, 1).GetProfile();
            var getBody = Assert.IsType<ProfileResponse>(Assert.IsType<OkObjectResult>(getResult.Result).Value);
            Assert.Equal("alice_2", getBody.Username);
        }

        [Fact]
        public async Task UpdateProfile_cannot_claim_a_username_owned_by_another_account()
        {
            await SeedUser(1, "alice");
            await SeedUser(2, "bob");
            await using var ctx = NewContext();

            var result = await NewController(ctx, 1).UpdateProfile(Update(username: "bob"));

            Assert.IsType<ConflictObjectResult>(result.Result);

            await using var verify = NewContext();
            Assert.Equal("alice", (await verify.Users.FindAsync(1))!.Username);
            Assert.Equal("bob", (await verify.Users.FindAsync(2))!.Username);
        }

        [Fact]
        public async Task UpdateProfile_never_inserts_a_body_metric_row_even_when_measurements_are_sent()
        {
            await SeedUser(1, "alice");
            await using var ctx = NewContext();

            var request = new UpdateProfileRequest(
                Name: "New Name", Username: null, Bio: "hi", DateOfBirth: null, Gender: null,
                Height: 181, Weight: 84, TargetWeight: 78, BodyFatPercentage: 17,
                ExperienceLevel: null, PrimaryGoal: null, Goals: null, UnitPreference: null,
                ThemePreference: null, FavoriteExercises: null);

            await NewController(ctx, 1).UpdateProfile(request);

            await using var verify = NewContext();
            Assert.Empty(verify.BodyMetrics);
        }

        [Fact]
        public async Task UpdateProfile_ignores_height_weight_bodyfat_from_the_request_body()
        {
            await SeedUser(1, "alice");
            await using var ctx = NewContext();

            var request = new UpdateProfileRequest(
                Name: null, Username: null, Bio: null, DateOfBirth: null, Gender: null,
                Height: 999, Weight: 999, TargetWeight: 70, BodyFatPercentage: 99,
                ExperienceLevel: null, PrimaryGoal: null, Goals: null, UnitPreference: null,
                ThemePreference: null, FavoriteExercises: null);

            var result = await NewController(ctx, 1).UpdateProfile(request);
            var body = Assert.IsType<ProfileResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);

            Assert.Null(body.Height);
            Assert.Null(body.Weight);
            Assert.Null(body.BodyFatPercentage);
            // TargetWeight is a goal, not a measurement - it stays profile-editable.
            Assert.Equal(70, body.TargetWeight);

            await using var verify = NewContext();
            var user = await verify.Users.FindAsync(1);
            Assert.Null(user!.Height);
            Assert.Null(user.Weight);
            Assert.Null(user.BodyFatPercentage);
        }

        [Theory]
        [InlineData(null, true)]                 // omitted - valid, preserved
        [InlineData("bob_01", true)]             // letters/digits/underscore
        [InlineData("ABC", true)]
        [InlineData("", false)]                  // signup rejects "" via [Required]; we via [MinLength(1)]
        [InlineData("   ", false)]               // whitespace fails the charset regex
        [InlineData("has space", false)]
        [InlineData("dash-not-ok", false)]
        [InlineData("emoji😀", false)]
        [InlineData("waytoolongusernameover30chars_x", false)] // 31 chars
        public void UpdateProfileRequest_username_is_at_least_as_strict_as_signup(
            string? username, bool expectedValid)
        {
            // MVC binds/validates positional records off the constructor
            // parameters; evaluate those exact ValidationAttributes here.
            var attrs = ParameterValidationAttributes(typeof(UpdateProfileRequest), "Username");
            var signupAttrs = ParameterValidationAttributes(typeof(SignupRequest), "Username");

            // Every signup rule (except [Required], which becomes "null = leave
            // unchanged") must also be enforced here...
            var signupRuleTypes = signupAttrs.Where(a => a is not RequiredAttribute)
                                             .Select(a => a.GetType()).ToHashSet();
            Assert.Subset(attrs.Select(a => a.GetType()).ToHashSet(), signupRuleTypes);

            // ...with the same argument values, not just the same attribute types.
            Assert.Equal(
                Pattern(signupAttrs.OfType<RegularExpressionAttribute>().Single()),
                Pattern(attrs.OfType<RegularExpressionAttribute>().Single()));
            Assert.Equal(
                signupAttrs.OfType<MaxLengthAttribute>().Single().Length,
                attrs.OfType<MaxLengthAttribute>().Single().Length);

            // ...plus an explicit non-empty floor that signup gets for free from
            // [Required] + the "+" quantifier.
            Assert.Contains(attrs, a => a is MinLengthAttribute { Length: >= 1 });

            var actualValid = attrs.All(a => a.IsValid(username));
            Assert.Equal(expectedValid, actualValid);
        }

        private static string Pattern(RegularExpressionAttribute a) => a.Pattern;

        [Fact]
        public async Task UpdateProfile_lets_a_user_recase_their_own_username()
        {
            await SeedUser(1, "alice");
            await using var ctx = NewContext();

            var result = await NewController(ctx, 1).UpdateProfile(Update(username: "Alice"));

            var body = Assert.IsType<ProfileResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
            Assert.Equal("Alice", body.Username);
        }

        [Fact]
        public async Task UsernameExistsAsync_excludes_the_callers_own_row()
        {
            // Proves the "taken" check ignores the caller - the mechanism that
            // lets a case-only self-rename succeed on a case-INSENSITIVE
            // collation (SQL Server), independent of this test's SQLite store.
            await SeedUser(1, "alice");
            await SeedUser(2, "bob");
            await using var ctx = NewContext();
            var repo = new UserRepository(ctx);

            Assert.False(await repo.UsernameExistsAsync("alice", excludeUserId: 1));
            Assert.True(await repo.UsernameExistsAsync("alice", excludeUserId: 2));
            Assert.True(await repo.UsernameExistsAsync("bob", excludeUserId: 1));
            Assert.False(await repo.UsernameExistsAsync("carol", excludeUserId: 1));
            // Back-compat: no excludeUserId behaves exactly as before.
            Assert.True(await repo.UsernameExistsAsync("alice"));
        }

        private static ValidationAttribute[] ParameterValidationAttributes(Type recordType, string paramName)
        {
            var parameter = recordType.GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .First()
                .GetParameters()
                .Single(p => p.Name == paramName);
            return parameter.GetCustomAttributes(typeof(ValidationAttribute), inherit: true)
                .Cast<ValidationAttribute>()
                .ToArray();
        }
    }
}
