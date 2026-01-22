using GoHardAPI.Models;
using GoHardAPI.Services;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace GoHardAPI.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly AuthService _authService;
        private readonly IConfiguration _configuration;

        public AuthServiceTests()
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

        [Fact]
        public void HashPassword_ReturnsNonEmptyHash()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var hash = _authService.HashPassword(password);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
            Assert.NotEqual(password, hash);
        }

        [Fact]
        public void HashPassword_DifferentPasswordsProduceDifferentHashes()
        {
            // Arrange
            var password1 = "Password1";
            var password2 = "Password2";

            // Act
            var hash1 = _authService.HashPassword(password1);
            var hash2 = _authService.HashPassword(password2);

            // Assert
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void HashPassword_SamePasswordProducesDifferentHashes_DueToSalt()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var hash1 = _authService.HashPassword(password);
            var hash2 = _authService.HashPassword(password);

            // Assert
            Assert.NotEqual(hash1, hash2); // BCrypt uses random salt each time
        }

        [Fact]
        public void VerifyPassword_CorrectPassword_ReturnsTrue()
        {
            // Arrange
            var password = "TestPassword123!";
            var hash = _authService.HashPassword(password);

            // Act
            var result = _authService.VerifyPassword(password, hash);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void VerifyPassword_IncorrectPassword_ReturnsFalse()
        {
            // Arrange
            var password = "TestPassword123!";
            var wrongPassword = "WrongPassword!";
            var hash = _authService.HashPassword(password);

            // Act
            var result = _authService.VerifyPassword(wrongPassword, hash);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void VerifyPassword_EmptyPassword_ReturnsFalse()
        {
            // Arrange
            var password = "TestPassword123!";
            var hash = _authService.HashPassword(password);

            // Act
            var result = _authService.VerifyPassword("", hash);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GenerateJwtToken_ReturnsValidToken()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Name = "Test User",
                Email = "test@example.com"
            };

            // Act
            var token = _authService.GenerateJwtToken(user);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public void GenerateJwtToken_TokenContainsCorrectClaims()
        {
            // Arrange
            var user = new User
            {
                Id = 42,
                Name = "John Doe",
                Email = "john@example.com"
            };

            // Act
            var token = _authService.GenerateJwtToken(user);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Assert
            Assert.Equal("42", jwtToken.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            Assert.Equal("John Doe", jwtToken.Claims.First(c => c.Type == ClaimTypes.Name).Value);
            Assert.Equal("john@example.com", jwtToken.Claims.First(c => c.Type == ClaimTypes.Email).Value);
            Assert.NotNull(jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti));
        }

        [Fact]
        public void GenerateJwtToken_TokenHasCorrectIssuerAndAudience()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Name = "Test User",
                Email = "test@example.com"
            };

            // Act
            var token = _authService.GenerateJwtToken(user);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Assert
            Assert.Equal("GoHardAPI", jwtToken.Issuer);
            Assert.Contains("GoHardApp", jwtToken.Audiences);
        }

        [Fact]
        public void GenerateJwtToken_TokenExpiresInFuture()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Name = "Test User",
                Email = "test@example.com"
            };

            // Act
            var token = _authService.GenerateJwtToken(user);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Assert
            Assert.True(jwtToken.ValidTo > DateTime.UtcNow);
        }

        [Fact]
        public void GenerateJwtToken_DifferentUsersGetDifferentTokens()
        {
            // Arrange
            var user1 = new User { Id = 1, Name = "User 1", Email = "user1@example.com" };
            var user2 = new User { Id = 2, Name = "User 2", Email = "user2@example.com" };

            // Act
            var token1 = _authService.GenerateJwtToken(user1);
            var token2 = _authService.GenerateJwtToken(user2);

            // Assert
            Assert.NotEqual(token1, token2);
        }
    }
}
