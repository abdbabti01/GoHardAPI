using GoHardAPI.Controllers;
using GoHardAPI.Data;
using GoHardAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    public class UsersControllerTests
    {
        private TrainingContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<TrainingContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new TrainingContext(options);
        }

        private UsersController CreateControllerWithUser(TrainingContext context, int? userId)
        {
            var controller = new UsersController(context);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Email, "test@example.com")
            };

            if (userId.HasValue)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            return controller;
        }

        private UsersController CreateControllerWithInvalidClaim(TrainingContext context, string invalidUserId)
        {
            var controller = new UsersController(context);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, invalidUserId),
                new Claim(ClaimTypes.Name, "Test User")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            return controller;
        }

        private async Task<User> CreateTestUser(TrainingContext context, int userId, string username)
        {
            var user = new User
            {
                Id = userId,
                Name = $"Name {userId}",
                Username = username,
                Email = $"user{userId}@example.com",
                PasswordHash = "super-secret-hash",
                FcmToken = $"fcm-token-{userId}"
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        }

        // --- Authorization attribute presence (Requirement 1) ---

        [Fact]
        public void UsersController_HasClassLevelAuthorizeAttribute()
        {
            var authorizeAttribute = typeof(UsersController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
                .Cast<AuthorizeAttribute>()
                .SingleOrDefault();

            Assert.NotNull(authorizeAttribute);
        }

        // --- GetCurrentUserId hardening (Requirement 7) ---

        [Fact]
        public async Task SearchUsers_MissingUserIdClaim_ReturnsUnauthorizedWithoutThrowing()
        {
            var context = GetInMemoryContext();
            var controller = CreateControllerWithUser(context, userId: null);

            var result = await controller.SearchUsers("ab");

            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        [Fact]
        public async Task SearchUsers_NonNumericUserIdClaim_ReturnsUnauthorizedWithoutThrowing()
        {
            var context = GetInMemoryContext();
            var controller = CreateControllerWithInvalidClaim(context, "not-a-number");

            var result = await controller.SearchUsers("ab");

            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        [Fact]
        public async Task GetPublicProfile_MissingUserIdClaim_ReturnsUnauthorizedWithoutThrowing()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1, "alice");
            var controller = CreateControllerWithUser(context, userId: null);

            var result = await controller.GetPublicProfile(1);

            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        [Fact]
        public async Task RegisterFcmToken_InvalidUserIdClaim_ReturnsUnauthorizedWithoutThrowing()
        {
            var context = GetInMemoryContext();
            var controller = CreateControllerWithInvalidClaim(context, "abc");

            var result = await controller.RegisterFcmToken(new FcmTokenDto { Token = "device-token" });

            Assert.IsType<UnauthorizedResult>(result);
        }

        // --- Search returns DTO-only data ---

        [Fact]
        public async Task SearchUsers_ReturnsOnlyDtoFields_ExcludesSelfAndSensitiveData()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1, "alice");
            await CreateTestUser(context, 2, "alicia");
            var controller = CreateControllerWithUser(context, userId: 1);

            var result = await controller.SearchUsers("ali");

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var users = Assert.IsAssignableFrom<IEnumerable<UserSearchResultDto>>(okResult.Value).ToList();

            Assert.Single(users);
            Assert.Equal(2, users[0].UserId);
            Assert.Equal("alicia", users[0].Username);
        }

        // --- Public profile returns DTO-only data ---

        [Fact]
        public async Task GetPublicProfile_ReturnsPublicProfileDto_HidesTotalWorkoutsWhenNotFriends()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1, "alice");
            await CreateTestUser(context, 2, "bob");
            var controller = CreateControllerWithUser(context, userId: 1);

            var result = await controller.GetPublicProfile(2);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var profile = Assert.IsType<PublicProfileDto>(okResult.Value);

            Assert.Equal(2, profile.UserId);
            Assert.Equal("bob", profile.Username);
            Assert.False(profile.IsFriend);
            Assert.Null(profile.TotalWorkoutsCount);
        }

        // --- FCM registration scoping (Requirement 8) ---

        [Fact]
        public async Task RegisterFcmToken_OnlyModifiesAuthenticatedUser()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1, "alice");
            await CreateTestUser(context, 42, "bob");
            var controller = CreateControllerWithUser(context, userId: 42);

            await controller.RegisterFcmToken(new FcmTokenDto { Token = "new-device-token" });

            var user1 = await context.Users.FindAsync(1);
            var user42 = await context.Users.FindAsync(42);

            Assert.Equal("new-device-token", user42!.FcmToken);
            Assert.Equal("fcm-token-1", user1!.FcmToken);
        }

        [Fact]
        public async Task UnregisterFcmToken_OnlyModifiesAuthenticatedUser()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1, "alice");
            await CreateTestUser(context, 42, "bob");
            var controller = CreateControllerWithUser(context, userId: 42);

            await controller.UnregisterFcmToken(new FcmTokenDto { Token = "fcm-token-42" });

            var user1 = await context.Users.FindAsync(1);
            var user42 = await context.Users.FindAsync(42);

            Assert.Null(user42!.FcmToken);
            Assert.Equal("fcm-token-1", user1!.FcmToken);
        }
    }
}
