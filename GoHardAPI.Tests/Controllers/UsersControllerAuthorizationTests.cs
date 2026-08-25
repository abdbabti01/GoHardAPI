using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Asp.Versioning;
using GoHardAPI.Controllers;
using GoHardAPI.Data;
using GoHardAPI.Models;
using GoHardAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Full-pipeline tests for UsersController that exercise the real ASP.NET Core
    /// authentication/authorization middleware (direct controller instantiation, used
    /// elsewhere in this test project, bypasses [Authorize] entirely and cannot prove 401s).
    /// A minimal test host is used instead of the production Program.cs because startup
    /// there requires a live SQL Server/PostgreSQL connection and runs migrations.
    /// </summary>
    public class UsersControllerAuthorizationTests : IAsyncLifetime
    {
        private const string TestSecret = "ThisIsAVeryLongSecretKeyForTestingPurposesOnly123456";
        private const string TestIssuer = "GoHardAPI";
        private const string TestAudience = "GoHardApp";

        private IHost _host = null!;
        private HttpClient _client = null!;
        private string _dbName = null!;
        private AuthService _authService = null!;

        public async Task InitializeAsync()
        {
            _dbName = Guid.NewGuid().ToString();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "JwtSettings:Secret", TestSecret },
                    { "JwtSettings:Issuer", TestIssuer },
                    { "JwtSettings:Audience", TestAudience },
                    { "JwtSettings:ExpirationHours", "24" }
                })
                .Build();

            _authService = new AuthService(configuration);

            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {
                        services.AddDbContext<TrainingContext>(options =>
                            options.UseInMemoryDatabase(_dbName));

                        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                            .AddJwtBearer(options =>
                            {
                                options.TokenValidationParameters = new TokenValidationParameters
                                {
                                    ValidateIssuer = true,
                                    ValidateAudience = true,
                                    ValidateLifetime = true,
                                    ValidateIssuerSigningKey = true,
                                    ValidIssuer = TestIssuer,
                                    ValidAudience = TestAudience,
                                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret))
                                };
                            });
                        services.AddAuthorization();

                        services.AddApiVersioning(options =>
                        {
                            options.DefaultApiVersion = new ApiVersion(1, 0);
                            options.AssumeDefaultVersionWhenUnspecified = true;
                            options.ApiVersionReader = new UrlSegmentApiVersionReader();
                        });

                        services.AddControllers()
                            .AddApplicationPart(typeof(UsersController).Assembly);
                    });
                    webHost.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAuthentication();
                        app.UseAuthorization();
                        app.UseEndpoints(endpoints => endpoints.MapControllers());
                    });
                });

            _host = await hostBuilder.StartAsync();
            _client = _host.GetTestServer().CreateClient();
        }

        public async Task DisposeAsync()
        {
            _client.Dispose();
            await _host.StopAsync();
            _host.Dispose();
        }

        private TrainingContext GetContext()
        {
            var options = new DbContextOptionsBuilder<TrainingContext>()
                .UseInMemoryDatabase(_dbName)
                .Options;
            return new TrainingContext(options);
        }

        private async Task<User> SeedUser(int id, string username)
        {
            using var context = GetContext();
            var user = new User
            {
                Id = id,
                Name = $"Name {id}",
                Username = username,
                Email = $"{username}@example.com",
                PasswordHash = "super-secret-bcrypt-hash",
                FcmToken = $"fcm-token-{id}"
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        }

        private string TokenFor(User user) => _authService.GenerateJwtToken(user);

        private HttpRequestMessage Authorized(HttpMethod method, string url, User user)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor(user));
            return request;
        }

        // --- Requirement 8a: anonymous requests receive 401 ---

        [Theory]
        [InlineData("GET", "/api/v1/users/search?username=al")]
        [InlineData("GET", "/api/v1/users/1/public-profile")]
        [InlineData("POST", "/api/v1/users/fcm-token")]
        [InlineData("DELETE", "/api/v1/users/fcm-token")]
        public async Task AnonymousRequest_ToProtectedEndpoint_Returns401(string method, string url)
        {
            var request = new HttpRequestMessage(new HttpMethod(method), url);
            if (method is "POST" or "DELETE")
            {
                request.Content = JsonContent.Create(new { token = "x" });
            }

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // --- Requirement 1: removed generic CRUD endpoints no longer exist ---

        [Theory]
        [InlineData("GET", "/api/v1/users")]
        [InlineData("GET", "/api/v1/users/1")]
        [InlineData("POST", "/api/v1/users")]
        [InlineData("PUT", "/api/v1/users/1")]
        [InlineData("DELETE", "/api/v1/users/1")]
        public async Task RemovedGenericCrudEndpoints_NoLongerRouteAnywhere(string method, string url)
        {
            var request = new HttpRequestMessage(new HttpMethod(method), url);
            if (method is "POST" or "PUT")
            {
                request.Content = JsonContent.Create(new { });
            }

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // --- Requirement 8b/8d: search returns only UserSearchResultDto fields, no password hash ---

        [Fact]
        public async Task SearchUsers_Authenticated_ReturnsOnlyDtoFields_NoPasswordHash()
        {
            var me = await SeedUser(1, "alice");
            await SeedUser(2, "alicia");

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/users/search?username=ali", me));
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            AssertNoSensitiveFields(body);

            using var doc = JsonDocument.Parse(body);
            var result = Assert.Single(doc.RootElement.EnumerateArray());
            var propertyNames = result.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            Assert.Equal(new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "userId", "username", "name", "profilePhotoUrl"
            }, propertyNames);
        }

        // --- Requirement 8c/8d: public profile returns only PublicProfileDto fields, no password hash ---

        [Fact]
        public async Task GetPublicProfile_Authenticated_ReturnsOnlyDtoFields_NoPasswordHash()
        {
            var me = await SeedUser(1, "alice");
            await SeedUser(2, "bob");

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/users/2/public-profile", me));
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            AssertNoSensitiveFields(body);

            using var doc = JsonDocument.Parse(body);
            var propertyNames = doc.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            Assert.Equal(new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "userId", "username", "name", "profilePhotoUrl", "bio", "experienceLevel",
                "memberSince", "isFriend", "sharedWorkoutsCount", "totalWorkoutsCount"
            }, propertyNames);
        }

        private static void AssertNoSensitiveFields(string json)
        {
            var lower = json.ToLowerInvariant();
            Assert.DoesNotContain("passwordhash", lower);
            Assert.DoesNotContain("fcmtoken", lower);
            Assert.DoesNotContain("email", lower);
            Assert.DoesNotContain("dateofbirth", lower);
            Assert.DoesNotContain("height", lower);
            Assert.DoesNotContain("weight", lower);
        }

        // --- Requirement 8e: FCM registration modifies only the authenticated user ---

        [Fact]
        public async Task RegisterFcmToken_Authenticated_OnlyModifiesAuthenticatedUser()
        {
            var me = await SeedUser(1, "alice");
            await SeedUser(2, "bob");

            var request = Authorized(HttpMethod.Post, "/api/v1/users/fcm-token", me);
            request.Content = JsonContent.Create(new { token = "brand-new-device-token" });

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var context = GetContext();
            var user1 = await context.Users.FindAsync(1);
            var user2 = await context.Users.FindAsync(2);

            Assert.Equal("brand-new-device-token", user1!.FcmToken);
            Assert.Equal("fcm-token-2", user2!.FcmToken);
        }
    }
}
