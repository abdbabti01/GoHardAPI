using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Asp.Versioning;
using GoHardAPI.Controllers;
using GoHardAPI.Data;
using GoHardAPI.Models;
using GoHardAPI.Repositories;
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
    /// Full-pipeline tests for ExerciseTemplatesController that exercise the real ASP.NET Core
    /// authentication/authorization middleware and the real repository/EF Core stack (direct
    /// controller instantiation, used elsewhere in this test project, bypasses [Authorize] and
    /// routing entirely and cannot prove 401/404s). Mirrors the UsersControllerAuthorizationTests
    /// minimal-test-host pattern; the production Program.cs is not started because it requires a
    /// live SQL Server/PostgreSQL connection and runs migrations.
    /// </summary>
    public class ExerciseTemplatesAuthorizationTests : IAsyncLifetime
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

                        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
                        services.AddScoped<IExerciseTemplateRepository, ExerciseTemplateRepository>();

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
                            .AddApplicationPart(typeof(ExerciseTemplatesController).Assembly);
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
                PasswordHash = "super-secret-bcrypt-hash"
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        }

        private async Task<ExerciseTemplate> SeedTemplate(int id, bool isCustom, int? createdByUserId, string name)
        {
            using var context = GetContext();
            var template = new ExerciseTemplate
            {
                Id = id,
                Name = name,
                Category = "Strength",
                MuscleGroup = "Chest",
                Equipment = "Barbell",
                Difficulty = "Intermediate",
                IsCustom = isCustom,
                CreatedByUserId = createdByUserId
            };
            context.ExerciseTemplates.Add(template);
            await context.SaveChangesAsync();
            return template;
        }

        private string TokenFor(User user) => _authService.GenerateJwtToken(user);

        private HttpRequestMessage Authorized(HttpMethod method, string url, User user)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor(user));
            return request;
        }

        // --- Removed admin routes no longer exist for anyone ---
        //
        // "POST /exercisetemplates/admin" expects 405, not 404: its URL shape
        // ("exercisetemplates/{single-segment}") still matches the unrelated,
        // legitimate GET api/exercisetemplates/{id} route template, so ASP.NET Core's
        // router finds a matching endpoint for the URL but not for the POST verb and
        // correctly reports 405 Method Not Allowed. No admin handler exists or runs
        // either way, and it is well established that responses in the 4xx range do
        // not risk leaking that a resource exists, this is expected ASP.NET Core
        // route-matching behavior, not a security gap.

        [Theory]
        [InlineData("POST", "/api/v1/exercisetemplates/admin", HttpStatusCode.MethodNotAllowed)]
        [InlineData("PUT", "/api/v1/exercisetemplates/admin/1", HttpStatusCode.NotFound)]
        [InlineData("DELETE", "/api/v1/exercisetemplates/admin/1", HttpStatusCode.NotFound)]
        [InlineData("POST", "/api/v1/exercisetemplates/admin/bulk", HttpStatusCode.NotFound)]
        public async Task AdminRoutes_AnonymousRequest_NoLongerRoutesToAdminHandler(string method, string url, HttpStatusCode expected)
        {
            var request = new HttpRequestMessage(new HttpMethod(method), url);
            if (method is "POST" or "PUT")
            {
                request.Content = JsonContent.Create(new { });
            }

            var response = await _client.SendAsync(request);

            Assert.Equal(expected, response.StatusCode);
        }

        [Theory]
        [InlineData("POST", "/api/v1/exercisetemplates/admin", HttpStatusCode.MethodNotAllowed)]
        [InlineData("PUT", "/api/v1/exercisetemplates/admin/1", HttpStatusCode.NotFound)]
        [InlineData("DELETE", "/api/v1/exercisetemplates/admin/1", HttpStatusCode.NotFound)]
        [InlineData("POST", "/api/v1/exercisetemplates/admin/bulk", HttpStatusCode.NotFound)]
        public async Task AdminRoutes_OrdinaryAuthenticatedUser_NoLongerRoutesToAdminHandler(string method, string url, HttpStatusCode expected)
        {
            var user = await SeedUser(1, "alice");
            var request = Authorized(new HttpMethod(method), url, user);
            if (method is "POST" or "PUT")
            {
                request.Content = JsonContent.Create(new { });
            }

            var response = await _client.SendAsync(request);

            Assert.Equal(expected, response.StatusCode);
        }

        // --- Public browsing endpoints remain accessible anonymously ---

        [Fact]
        public async Task GetExerciseTemplates_Anonymous_ReturnsOk()
        {
            await SeedTemplate(1, isCustom: false, createdByUserId: null, name: "Bench Press");

            var response = await _client.GetAsync("/api/v1/exercisetemplates");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetExerciseTemplateById_Anonymous_ReturnsOk()
        {
            await SeedTemplate(1, isCustom: false, createdByUserId: null, name: "Bench Press");

            var response = await _client.GetAsync("/api/v1/exercisetemplates/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetCategories_Anonymous_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/v1/exercisetemplates/categories");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetMuscleGroups_Anonymous_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/v1/exercisetemplates/musclegroups");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // --- Authenticated custom-template creation ---

        [Fact]
        public async Task CreateExerciseTemplate_Authenticated_CreatesOwnedCustomTemplate()
        {
            var me = await SeedUser(1, "alice");

            var request = Authorized(HttpMethod.Post, "/api/v1/exercisetemplates", me);
            request.Content = JsonContent.Create(new
            {
                name = "My Custom Curl",
                category = "Arms",
                muscleGroup = "Biceps",
                equipment = "Dumbbell",
                difficulty = "Beginner"
            });

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<ExerciseTemplate>();
            Assert.NotNull(created);
            Assert.True(created!.IsCustom);
            Assert.Equal(1, created.CreatedByUserId);
        }

        [Fact]
        public async Task CreateExerciseTemplate_ClientSuppliedIsCustomAndOwner_AreIgnored()
        {
            var me = await SeedUser(1, "alice");
            await SeedUser(2, "bob");

            var request = Authorized(HttpMethod.Post, "/api/v1/exercisetemplates", me);
            request.Content = JsonContent.Create(new
            {
                name = "Spoofed System Template",
                category = "Arms",
                isCustom = false,
                createdByUserId = 2
            });

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<ExerciseTemplate>();
            Assert.NotNull(created);
            Assert.True(created!.IsCustom, "Server must force IsCustom=true regardless of client input");
            Assert.Equal(1, created.CreatedByUserId); // caller's id, not the spoofed 2
        }

        // --- Owner-only update/delete of custom templates ---

        [Fact]
        public async Task UpdateExerciseTemplate_Owner_Succeeds()
        {
            var me = await SeedUser(1, "alice");
            await SeedTemplate(10, isCustom: true, createdByUserId: 1, name: "My Curl");

            var request = Authorized(HttpMethod.Put, "/api/v1/exercisetemplates/10", me);
            request.Content = JsonContent.Create(new ExerciseTemplate
            {
                Id = 10,
                Name = "My Curl Updated",
                IsCustom = true,
                CreatedByUserId = 1
            });

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var context = GetContext();
            var updated = await context.ExerciseTemplates.FindAsync(10);
            Assert.Equal("My Curl Updated", updated!.Name);
        }

        [Fact]
        public async Task DeleteExerciseTemplate_Owner_Succeeds()
        {
            var me = await SeedUser(1, "alice");
            await SeedTemplate(11, isCustom: true, createdByUserId: 1, name: "My Curl 2");

            var request = Authorized(HttpMethod.Delete, "/api/v1/exercisetemplates/11", me);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var context = GetContext();
            var deleted = await context.ExerciseTemplates.FindAsync(11);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task UpdateExerciseTemplate_NonOwner_Returns403()
        {
            await SeedUser(1, "alice");
            var other = await SeedUser(2, "bob");
            await SeedTemplate(12, isCustom: true, createdByUserId: 1, name: "Alice's Curl");

            var request = Authorized(HttpMethod.Put, "/api/v1/exercisetemplates/12", other);
            request.Content = JsonContent.Create(new ExerciseTemplate
            {
                Id = 12,
                Name = "Hijacked",
                IsCustom = true,
                CreatedByUserId = 1
            });

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            using var context = GetContext();
            var unchanged = await context.ExerciseTemplates.FindAsync(12);
            Assert.Equal("Alice's Curl", unchanged!.Name);
        }

        [Fact]
        public async Task DeleteExerciseTemplate_NonOwner_Returns403()
        {
            await SeedUser(1, "alice");
            var other = await SeedUser(2, "bob");
            await SeedTemplate(13, isCustom: true, createdByUserId: 1, name: "Alice's Curl 2");

            var request = Authorized(HttpMethod.Delete, "/api/v1/exercisetemplates/13", other);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            using var context = GetContext();
            var stillThere = await context.ExerciseTemplates.FindAsync(13);
            Assert.NotNull(stillThere);
        }

        [Fact]
        public async Task UpdateExerciseTemplate_ClientSuppliedIsCustomAndOwner_AreIgnored()
        {
            var me = await SeedUser(1, "alice");
            await SeedTemplate(14, isCustom: true, createdByUserId: 1, name: "My Curl 3");

            var request = Authorized(HttpMethod.Put, "/api/v1/exercisetemplates/14", me);
            request.Content = JsonContent.Create(new ExerciseTemplate
            {
                Id = 14,
                Name = "My Curl 3 Updated",
                IsCustom = false,       // client attempts to flip to a system template
                CreatedByUserId = 999   // client attempts to reassign ownership
            });

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var context = GetContext();
            var updated = await context.ExerciseTemplates.FindAsync(14);
            Assert.Equal("My Curl 3 Updated", updated!.Name);
            Assert.True(updated.IsCustom, "Server must not let clients flip IsCustom via update");
            Assert.Equal(1, updated.CreatedByUserId);
        }

        // --- System templates cannot be changed through normal (non-admin) endpoints ---

        [Fact]
        public async Task UpdateExerciseTemplate_SystemTemplate_Returns400()
        {
            var me = await SeedUser(1, "alice");
            await SeedTemplate(20, isCustom: false, createdByUserId: null, name: "Bench Press");

            var request = Authorized(HttpMethod.Put, "/api/v1/exercisetemplates/20", me);
            request.Content = JsonContent.Create(new ExerciseTemplate
            {
                Id = 20,
                Name = "Hijacked Bench Press",
                IsCustom = false
            });

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var context = GetContext();
            var unchanged = await context.ExerciseTemplates.FindAsync(20);
            Assert.Equal("Bench Press", unchanged!.Name);
        }

        [Fact]
        public async Task DeleteExerciseTemplate_SystemTemplate_Returns400()
        {
            var me = await SeedUser(1, "alice");
            await SeedTemplate(21, isCustom: false, createdByUserId: null, name: "Squat");

            var request = Authorized(HttpMethod.Delete, "/api/v1/exercisetemplates/21", me);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var context = GetContext();
            var stillThere = await context.ExerciseTemplates.FindAsync(21);
            Assert.NotNull(stillThere);
        }

        // --- Anonymous requests to the legitimate write endpoints require authentication ---

        [Fact]
        public async Task CreateExerciseTemplate_Anonymous_Returns401()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/exercisetemplates")
            {
                Content = JsonContent.Create(new
                {
                    name = "Anonymous Curl",
                    category = "Arms"
                })
            };

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UpdateExerciseTemplate_Anonymous_Returns401()
        {
            await SeedTemplate(30, isCustom: true, createdByUserId: 1, name: "Anonymous Target Curl");

            var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/exercisetemplates/30")
            {
                Content = JsonContent.Create(new ExerciseTemplate
                {
                    Id = 30,
                    Name = "Hijacked Without Auth",
                    IsCustom = true,
                    CreatedByUserId = 1
                })
            };

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteExerciseTemplate_Anonymous_Returns401()
        {
            await SeedTemplate(31, isCustom: true, createdByUserId: 1, name: "Anonymous Target Curl 2");

            var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/exercisetemplates/31");

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
