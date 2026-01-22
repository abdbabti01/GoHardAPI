using GoHardAPI.Controllers;
using GoHardAPI.Data;
using GoHardAPI.DTOs;
using GoHardAPI.Models;
using GoHardAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly AuthService _authService;
        private readonly IConfiguration _configuration;

        public AuthControllerTests()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                {"JwtSettings:Secret", "ThisIsAVeryLongSecretKeyForTestingPurposesOnly123456"},
                {"JwtSettings:Issuer", "GoHardAPI"},
                {"JwtSettings:Audience", "GoHardApp"},
                {"JwtSettings:ExpirationHours", "24"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _authService = new AuthService(_configuration);
        }

        private TrainingContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<TrainingContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new TrainingContext(options);
        }

        [Fact]
        public async Task Signup_NewUser_ReturnsOkWithToken()
        {
            // Arrange
            var context = GetInMemoryContext();
            var controller = new AuthController(context, _authService);
            var request = new SignupRequest("John Doe", "john@example.com", "Password123!");

            // Act
            var result = await controller.Signup(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<AuthResponse>(okResult.Value);
            Assert.NotNull(response.Token);
            Assert.Equal("John Doe", response.Name);
            Assert.Equal("john@example.com", response.Email);
        }

        [Fact]
        public async Task Signup_DuplicateEmail_ReturnsBadRequest()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.Users.Add(new User
            {
                Name = "Existing User",
                Email = "existing@example.com",
                PasswordHash = _authService.HashPassword("Password123!"),
                PasswordSalt = string.Empty
            });
            await context.SaveChangesAsync();

            var controller = new AuthController(context, _authService);
            var request = new SignupRequest("New User", "existing@example.com", "Password123!");

            // Act
            var result = await controller.Signup(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Signup_CreatesUserInDatabase()
        {
            // Arrange
            var context = GetInMemoryContext();
            var controller = new AuthController(context, _authService);
            var request = new SignupRequest("Jane Doe", "jane@example.com", "Password123!");

            // Act
            await controller.Signup(request);

            // Assert
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "jane@example.com");
            Assert.NotNull(user);
            Assert.Equal("Jane Doe", user.Name);
            Assert.True(user.IsActive);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var context = GetInMemoryContext();
            var passwordHash = _authService.HashPassword("Password123!");
            context.Users.Add(new User
            {
                Name = "Test User",
                Email = "test@example.com",
                PasswordHash = passwordHash,
                PasswordSalt = string.Empty,
                IsActive = true
            });
            await context.SaveChangesAsync();

            var controller = new AuthController(context, _authService);
            var request = new LoginRequest("test@example.com", "Password123!");

            // Act
            var result = await controller.Login(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<AuthResponse>(okResult.Value);
            Assert.NotNull(response.Token);
            Assert.Equal("Test User", response.Name);
            Assert.Equal("test@example.com", response.Email);
        }

        [Fact]
        public async Task Login_InvalidEmail_ReturnsUnauthorized()
        {
            // Arrange
            var context = GetInMemoryContext();
            var controller = new AuthController(context, _authService);
            var request = new LoginRequest("nonexistent@example.com", "Password123!");

            // Act
            var result = await controller.Login(request);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }

        [Fact]
        public async Task Login_InvalidPassword_ReturnsUnauthorized()
        {
            // Arrange
            var context = GetInMemoryContext();
            var passwordHash = _authService.HashPassword("Password123!");
            context.Users.Add(new User
            {
                Name = "Test User",
                Email = "test@example.com",
                PasswordHash = passwordHash,
                PasswordSalt = string.Empty,
                IsActive = true
            });
            await context.SaveChangesAsync();

            var controller = new AuthController(context, _authService);
            var request = new LoginRequest("test@example.com", "WrongPassword!");

            // Act
            var result = await controller.Login(request);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }

        [Fact]
        public async Task Login_InactiveUser_ReturnsUnauthorized()
        {
            // Arrange
            var context = GetInMemoryContext();
            var passwordHash = _authService.HashPassword("Password123!");
            context.Users.Add(new User
            {
                Name = "Inactive User",
                Email = "inactive@example.com",
                PasswordHash = passwordHash,
                PasswordSalt = string.Empty,
                IsActive = false
            });
            await context.SaveChangesAsync();

            var controller = new AuthController(context, _authService);
            var request = new LoginRequest("inactive@example.com", "Password123!");

            // Act
            var result = await controller.Login(request);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }

        [Fact]
        public async Task Login_UpdatesLastLoginDate()
        {
            // Arrange
            var context = GetInMemoryContext();
            var passwordHash = _authService.HashPassword("Password123!");
            var user = new User
            {
                Name = "Test User",
                Email = "test@example.com",
                PasswordHash = passwordHash,
                PasswordSalt = string.Empty,
                IsActive = true,
                LastLoginDate = null
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = new AuthController(context, _authService);
            var request = new LoginRequest("test@example.com", "Password123!");

            // Act
            await controller.Login(request);

            // Assert
            var updatedUser = await context.Users.FirstAsync(u => u.Email == "test@example.com");
            Assert.NotNull(updatedUser.LastLoginDate);
            Assert.True(updatedUser.LastLoginDate > DateTime.UtcNow.AddMinutes(-1));
        }
    }
}
