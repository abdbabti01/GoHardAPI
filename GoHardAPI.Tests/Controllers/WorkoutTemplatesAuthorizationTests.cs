using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Asp.Versioning;
using GoHardAPI.Controllers;
using GoHardAPI.Converters;
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
using System.Text.Json;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Full-pipeline tests for WorkoutTemplatesController: private/public/system visibility,
    /// JWT-only ownership, over-posting prevention, usage/rating authorization, and the centralized
    /// DTO contract. Uses a real ASP.NET Core host (auth middleware + routing) over an EF Core
    /// InMemory database, mirroring SharedWorkoutsAuthorizationTests — direct controller
    /// instantiation would bypass [Authorize] and routing and could not prove 401/403/404s.
    /// Migration behavior is covered separately in WorkoutTemplatesMigrationTests.
    /// </summary>
    public class WorkoutTemplatesAuthorizationTests : IAsyncLifetime
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

                        // Mirror Program.cs's JSON configuration so contract assertions (camelCase
                        // names, UTC 'Z' timestamps) reflect what the deployed API actually emits.
                        services.AddControllers()
                            .AddApplicationPart(typeof(WorkoutTemplatesController).Assembly)
                            .AddJsonOptions(options =>
                            {
                                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                                options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
                                options.JsonSerializerOptions.Converters.Add(new NullableUtcDateTimeConverter());
                            });
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

        private async Task<WorkoutTemplate> SeedTemplate(
            int id,
            int? createdByUserId,
            bool isPublic = false,
            bool isActive = true,
            string name = "Template",
            string recurrencePattern = "daily",
            string? daysOfWeek = null,
            int? intervalDays = null,
            DateTime? lastUsedAt = null,
            int usageCount = 0,
            double? rating = null,
            int ratingCount = 0)
        {
            using var context = GetContext();
            var template = new WorkoutTemplate
            {
                Id = id,
                Name = name,
                Description = "desc",
                ExercisesJson = "[]",
                RecurrencePattern = recurrencePattern,
                DaysOfWeek = daysOfWeek,
                IntervalDays = intervalDays,
                EstimatedDuration = 30,
                Category = "Strength",
                IsActive = isActive,
                UsageCount = usageCount,
                CreatedAt = DateTime.UtcNow,
                LastUsedAt = lastUsedAt,
                CreatedByUserId = createdByUserId,
                IsCustom = createdByUserId != null,
                IsPublic = isPublic,
                Rating = rating,
                RatingCount = ratingCount
            };
            context.WorkoutTemplates.Add(template);
            await context.SaveChangesAsync();
            return template;
        }

        private async Task SeedRating(int templateId, int userId, double rating)
        {
            using var context = GetContext();
            context.WorkoutTemplateRatings.Add(new WorkoutTemplateRating
            {
                WorkoutTemplateId = templateId,
                UserId = userId,
                Rating = rating,
                RatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        private string TokenFor(User user) => _authService.GenerateJwtToken(user);

        private HttpRequestMessage Authorized(HttpMethod method, string url, User user)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor(user));
            return request;
        }

        private HttpRequestMessage AuthorizedJson(HttpMethod method, string url, User user, object body)
        {
            var request = Authorized(method, url, user);
            request.Content = JsonContent.Create(body);
            return request;
        }

        private static object ValidCreateBody(bool? isPublic = null) => new
        {
            name = "Morning Routine",
            description = "Quick morning workout",
            exercisesJson = "[{\"name\":\"Push-ups\",\"sets\":3,\"reps\":10}]",
            recurrencePattern = "daily",
            estimatedDuration = 20,
            category = "Strength",
            isActive = true,
            isPublic
        };

        // =========================================================================
        // Visibility (spec 1-11)
        // =========================================================================

        [Fact] // 1
        public async Task GetTemplate_SystemTemplate_VisibleToAnyAuthenticatedUser()
        {
            var user = await SeedUser(1, "alice");
            await SeedTemplate(100, createdByUserId: null);

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/workouttemplates/100", user));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact] // 2
        public async Task GetTemplate_Owner_SeesOwnPrivateTemplate()
        {
            var owner = await SeedUser(1, "alice");
            await SeedTemplate(101, createdByUserId: 1, isPublic: false);

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/workouttemplates/101", owner));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact] // 3
        public async Task GetTemplate_Stranger_PrivateTemplate_NotFound()
        {
            await SeedUser(1, "alice");
            var stranger = await SeedUser(2, "bob");
            await SeedTemplate(102, createdByUserId: 1, isPublic: false);

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/workouttemplates/102", stranger));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact] // 4
        public async Task GetTemplate_Stranger_PublicTemplate_ReturnsOk()
        {
            await SeedUser(1, "alice");
            var stranger = await SeedUser(2, "bob");
            await SeedTemplate(103, createdByUserId: 1, isPublic: true);

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/workouttemplates/103", stranger));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact] // 5
        public async Task GetCommunity_ContainsSystemAndPublicCustomTemplates()
        {
            var user = await SeedUser(1, "alice");
            await SeedUser(2, "bob");
            await SeedTemplate(110, createdByUserId: null, name: "System");
            await SeedTemplate(111, createdByUserId: 2, isPublic: true, name: "Public Custom");

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/workouttemplates/community", user));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var ids = (await response.Content.ReadFromJsonAsync<JsonElement>())
                .EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).ToList();
            Assert.Contains(110, ids);
            Assert.Contains(111, ids);
        }

        [Fact] // community limit is clamped to [1, 200] and never leaks private templates
        public async Task GetCommunity_LimitIsClampedAndNeverLeaksPrivateTemplates()
        {
            var caller = await SeedUser(1, "alice");
            await SeedUser(2, "bob");

            int id = 5000;
            for (int i = 0; i < 6; i++) await SeedTemplate(id++, createdByUserId: null, name: $"sys{i}");        // 6 system
            for (int i = 0; i < 4; i++) await SeedTemplate(id++, createdByUserId: 2, isPublic: true, name: $"pub{i}"); // 4 public custom
            for (int i = 0; i < 5; i++) await SeedTemplate(id++, createdByUserId: 2, isPublic: false, name: $"priv{i}"); // 5 private (must never appear)
            await SeedTemplate(id++, createdByUserId: 1, isPublic: false, name: "myPriv"); // caller's own private (must never appear)

            async Task<List<JsonElement>> Community(string qs)
            {
                var resp = await _client.SendAsync(Authorized(HttpMethod.Get, $"/api/v1/workouttemplates/community{qs}", caller));
                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
                return (await resp.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
            }

            // limit=0 clamps to 1
            Assert.Single(await Community("?limit=0"));
            // negative clamps to 1
            Assert.Single(await Community("?limit=-10"));
            // huge clamps to 200 → returns all 10 visible (6 system + 4 public), never the 6 private
            var big = await Community("?limit=100000");
            Assert.Equal(10, big.Count);
            var names = big.Select(e => e.GetProperty("name").GetString()).ToList();
            Assert.DoesNotContain(names, n => n!.StartsWith("priv"));
            Assert.DoesNotContain("myPriv", names);
            Assert.All(big, e => Assert.NotEqual(JsonValueKind.Undefined,
                e.TryGetProperty("isPublic", out var p) ? p.ValueKind : JsonValueKind.Undefined));
        }

        [Fact] // 6
        public async Task GetCommunity_ExcludesPrivateCustomTemplates()
        {
            var user = await SeedUser(1, "alice");
            await SeedUser(2, "bob");
            await SeedTemplate(112, createdByUserId: 2, isPublic: false, name: "Private Custom");
            // Even the caller's own private template must not surface in the community feed.
            await SeedTemplate(113, createdByUserId: 1, isPublic: false, name: "My Private");

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/workouttemplates/community", user));

            var ids = (await response.Content.ReadFromJsonAsync<JsonElement>())
                .EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).ToList();
            Assert.DoesNotContain(112, ids);
            Assert.DoesNotContain(113, ids);
        }

        [Fact] // 7
        public async Task GetTemplate_HiddenAndMissingIds_ReturnIdenticalResponses()
        {
            await SeedUser(1, "alice");
            var stranger = await SeedUser(2, "bob");
            await SeedTemplate(120, createdByUserId: 1, isPublic: false);

            var hidden = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/workouttemplates/120", stranger));
            var missing = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/workouttemplates/999999", stranger));

            Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
            Assert.Equal(missing.StatusCode, hidden.StatusCode);

            var hiddenBody = await hidden.Content.ReadFromJsonAsync<JsonElement>();
            var missingBody = await missing.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(missingBody.GetProperty("status").GetInt32(), hiddenBody.GetProperty("status").GetInt32());
            Assert.Equal(missingBody.GetProperty("title").GetString(), hiddenBody.GetProperty("title").GetString());
            Assert.Equal(missingBody.ToString().Length, hiddenBody.ToString().Length);
        }

        [Fact] // 8
        public async Task GetTemplates_OwnerList_DoesNotExposeAnotherUsersTemplates()
        {
            var owner = await SeedUser(1, "alice");
            await SeedUser(2, "bob");
            await SeedTemplate(130, createdByUserId: 1, name: "Mine");
            await SeedTemplate(131, createdByUserId: 2, isPublic: true, name: "Bob public");
            await SeedTemplate(132, createdByUserId: null, name: "System");

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/workouttemplates", owner));
            var ids = (await response.Content.ReadFromJsonAsync<JsonElement>())
                .EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).ToList();

            Assert.Equal(new[] { 130 }, ids);
        }

        [Fact] // 9
        public async Task GetScheduled_ContainsOnlyRequesterOwnedActiveTemplates()
        {
            var owner = await SeedUser(1, "alice");
            await SeedUser(2, "bob");
            await SeedTemplate(140, createdByUserId: 1, isActive: true, recurrencePattern: "daily", name: "Mine active");
            await SeedTemplate(141, createdByUserId: 1, isActive: false, recurrencePattern: "daily", name: "Mine inactive");
            await SeedTemplate(142, createdByUserId: 2, isActive: true, isPublic: true, recurrencePattern: "daily", name: "Bob active public");
            await SeedTemplate(143, createdByUserId: null, isActive: true, recurrencePattern: "daily", name: "System active");

            var date = new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc).ToString("o");
            var response = await _client.SendAsync(Authorized(HttpMethod.Get, $"/api/v1/workouttemplates/scheduled?date={Uri.EscapeDataString(date)}", owner));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var ids = (await response.Content.ReadFromJsonAsync<JsonElement>())
                .EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).ToList();
            Assert.Equal(new[] { 140 }, ids);
        }

        [Fact] // 10
        public async Task GetScheduled_AnotherUsersLastUsedAt_CannotAffectRequesterSchedule()
        {
            var owner = await SeedUser(1, "alice");
            await SeedUser(2, "bob");
            // Bob's custom template that "would be due" - must never appear for Alice.
            await SeedTemplate(150, createdByUserId: 2, isPublic: true, recurrencePattern: "custom",
                intervalDays: 1, lastUsedAt: DateTime.UtcNow.AddDays(-30));

            var date = DateTime.UtcNow.ToString("o");
            var response = await _client.SendAsync(Authorized(HttpMethod.Get, $"/api/v1/workouttemplates/scheduled?date={Uri.EscapeDataString(date)}", owner));

            var ids = (await response.Content.ReadFromJsonAsync<JsonElement>())
                .EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).ToList();
            Assert.DoesNotContain(150, ids);
        }

        [Fact] // 11
        public async Task GetScheduled_SystemTemplateUsageHistory_DoesNotEnterRequesterSchedule()
        {
            var owner = await SeedUser(1, "alice");
            await SeedTemplate(151, createdByUserId: null, recurrencePattern: "custom",
                intervalDays: 1, lastUsedAt: DateTime.UtcNow.AddDays(-30));

            var date = DateTime.UtcNow.ToString("o");
            var response = await _client.SendAsync(Authorized(HttpMethod.Get, $"/api/v1/workouttemplates/scheduled?date={Uri.EscapeDataString(date)}", owner));

            var ids = (await response.Content.ReadFromJsonAsync<JsonElement>())
                .EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).ToList();
            Assert.DoesNotContain(151, ids);
        }

        // =========================================================================
        // Create / update / delete (spec 12-27)
        // =========================================================================

        [Fact] // 12
        public async Task CreateTemplate_StampsJwtOwner()
        {
            var me = await SeedUser(1, "alice");
            await SeedUser(2, "bob");

            var response = await _client.SendAsync(AuthorizedJson(HttpMethod.Post, "/api/v1/workouttemplates", me, ValidCreateBody()));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, body.GetProperty("createdByUserId").GetInt32());
            Assert.True(body.GetProperty("isCustom").GetBoolean());
        }

        [Fact] // 13
        public async Task CreateTemplate_IsPrivateByDefault()
        {
            var me = await SeedUser(1, "alice");

            var response = await _client.SendAsync(AuthorizedJson(HttpMethod.Post, "/api/v1/workouttemplates", me, ValidCreateBody(isPublic: null)));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.False(body.GetProperty("isPublic").GetBoolean());
        }

        [Fact] // 14
        public async Task CreateTemplate_CanExplicitlyPublish()
        {
            var me = await SeedUser(1, "alice");

            var response = await _client.SendAsync(AuthorizedJson(HttpMethod.Post, "/api/v1/workouttemplates", me, ValidCreateBody(isPublic: true)));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(body.GetProperty("isPublic").GetBoolean());
        }

        [Fact] // 15
        public async Task CreateTemplate_ClientSuppliedOwner_IsIgnored()
        {
            var me = await SeedUser(1, "alice");
            await SeedUser(2, "bob");

            var request = Authorized(HttpMethod.Post, "/api/v1/workouttemplates", me);
            request.Content = JsonContent.Create(new
            {
                name = "Spoofed",
                exercisesJson = "[]",
                recurrencePattern = "daily",
                createdByUserId = 2, // spoof
                createdByUserName = "bob"
            });

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, body.GetProperty("createdByUserId").GetInt32());
        }

        [Fact] // 16
        public async Task CreateTemplate_CannotCreateSystemTemplate()
        {
            var me = await SeedUser(1, "alice");

            var request = Authorized(HttpMethod.Post, "/api/v1/workouttemplates", me);
            request.Content = JsonContent.Create(new
            {
                name = "Fake system",
                exercisesJson = "[]",
                recurrencePattern = "daily",
                createdByUserId = (int?)null,
                isCustom = false
            });

            var response = await _client.SendAsync(request);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(1, body.GetProperty("createdByUserId").GetInt32());
            Assert.True(body.GetProperty("isCustom").GetBoolean());
        }

        [Fact] // 17
        public async Task CreateTemplate_CannotSeedServerOwnedFields()
        {
            var me = await SeedUser(1, "alice");

            var request = Authorized(HttpMethod.Post, "/api/v1/workouttemplates", me);
            request.Content = JsonContent.Create(new
            {
                id = 555,
                name = "Seed attempt",
                exercisesJson = "[]",
                recurrencePattern = "daily",
                usageCount = 999,
                rating = 5.0,
                ratingCount = 42,
                createdAt = "2000-01-01T00:00:00Z",
                lastUsedAt = "2001-01-01T00:00:00Z"
            });

            var response = await _client.SendAsync(request);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.NotEqual(555, body.GetProperty("id").GetInt32());
            Assert.Equal(0, body.GetProperty("usageCount").GetInt32());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("rating").ValueKind);
            Assert.Equal(0, body.GetProperty("ratingCount").GetInt32());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("lastUsedAt").ValueKind);
            Assert.True(DateTime.Parse(body.GetProperty("createdAt").GetString()!).ToUniversalTime() > new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        [Fact] // 18
        public async Task UpdateTemplate_Owner_UpdatesAllowedFields()
        {
            var owner = await SeedUser(1, "alice");
            await SeedTemplate(200, createdByUserId: 1, name: "Old");

            var response = await _client.SendAsync(AuthorizedJson(HttpMethod.Put, "/api/v1/workouttemplates/200", owner, new
            {
                name = "New name",
                description = "New desc",
                exercisesJson = "[{\"x\":1}]",
                recurrencePattern = "weekly",
                daysOfWeek = "1,3,5",
                estimatedDuration = 45,
                category = "Cardio",
                isActive = false,
                isPublic = false
            }));
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var context = GetContext();
            var t = await context.WorkoutTemplates.FindAsync(200);
            Assert.Equal("New name", t!.Name);
            Assert.Equal("weekly", t.RecurrencePattern);
            Assert.Equal("1,3,5", t.DaysOfWeek);
            Assert.False(t.IsActive);
        }

        [Fact] // 19
        public async Task UpdateTemplate_CannotTransferOwnershipOrChangeServerFields()
        {
            var owner = await SeedUser(1, "alice");
            await SeedUser(2, "bob");
            await SeedTemplate(201, createdByUserId: 1, usageCount: 7, rating: 4.0, ratingCount: 3);

            var response = await _client.SendAsync(AuthorizedJson(HttpMethod.Put, "/api/v1/workouttemplates/201", owner, new
            {
                name = "Rename",
                exercisesJson = "[]",
                recurrencePattern = "daily",
                isActive = true,
                isPublic = false,
                createdByUserId = 2,
                isCustom = false,
                usageCount = 0,
                rating = 1.0,
                ratingCount = 0,
                createdAt = "2000-01-01T00:00:00Z"
            }));
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var context = GetContext();
            var t = await context.WorkoutTemplates.FindAsync(201);
            Assert.Equal(1, t!.CreatedByUserId);
            Assert.True(t.IsCustom);
            Assert.Equal(7, t.UsageCount);
            Assert.Equal(4.0, t.Rating);
            Assert.Equal(3, t.RatingCount);
        }

        [Fact] // 20
        public async Task UpdateTemplate_Stranger_PublicTemplate_Forbidden_PrivateTemplate_NotFound()
        {
            await SeedUser(1, "alice");
            var stranger = await SeedUser(2, "bob");
            await SeedTemplate(202, createdByUserId: 1, isPublic: true, name: "Public");
            await SeedTemplate(203, createdByUserId: 1, isPublic: false, name: "Private");

            var body = new { name = "hax", exercisesJson = "[]", recurrencePattern = "daily", isActive = true, isPublic = true };

            var publicResp = await _client.SendAsync(AuthorizedJson(HttpMethod.Put, "/api/v1/workouttemplates/202", stranger, body));
            var privateResp = await _client.SendAsync(AuthorizedJson(HttpMethod.Put, "/api/v1/workouttemplates/203", stranger, body));

            // Visible-but-not-owned → 403; a template hidden from the caller stays a 404, identical
            // to a missing id, so its existence is never disclosed through a write verb.
            Assert.Equal(HttpStatusCode.Forbidden, publicResp.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, privateResp.StatusCode);

            var missingResp = await _client.SendAsync(AuthorizedJson(HttpMethod.Put, "/api/v1/workouttemplates/424242", stranger, body));
            Assert.Equal(privateResp.StatusCode, missingResp.StatusCode);

            using var context = GetContext();
            Assert.Equal("Public", (await context.WorkoutTemplates.FindAsync(202))!.Name);
            Assert.Equal("Private", (await context.WorkoutTemplates.FindAsync(203))!.Name);
        }

        [Fact] // 21
        public async Task UpdateTemplate_SystemTemplate_Rejected()
        {
            var user = await SeedUser(1, "alice");
            await SeedTemplate(204, createdByUserId: null, name: "System");

            var response = await _client.SendAsync(AuthorizedJson(HttpMethod.Put, "/api/v1/workouttemplates/204", user, new
            {
                name = "hax", exercisesJson = "[]", recurrencePattern = "daily", isActive = true, isPublic = true
            }));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            using var context = GetContext();
            Assert.Equal("System", (await context.WorkoutTemplates.FindAsync(204))!.Name);
        }

        [Fact] // 22
        public async Task UpdateTemplate_Owner_CanPublishAndUnpublish()
        {
            var owner = await SeedUser(1, "alice");
            await SeedTemplate(205, createdByUserId: 1, isPublic: false);

            var body = new { name = "T", exercisesJson = "[]", recurrencePattern = "daily", isActive = true, isPublic = true };
            await _client.SendAsync(AuthorizedJson(HttpMethod.Put, "/api/v1/workouttemplates/205", owner, body));
            using (var c1 = GetContext())
                Assert.True((await c1.WorkoutTemplates.FindAsync(205))!.IsPublic);

            var body2 = new { name = "T", exercisesJson = "[]", recurrencePattern = "daily", isActive = true, isPublic = false };
            await _client.SendAsync(AuthorizedJson(HttpMethod.Put, "/api/v1/workouttemplates/205", owner, body2));
            using var c2 = GetContext();
            Assert.False((await c2.WorkoutTemplates.FindAsync(205))!.IsPublic);
        }

        [Fact] // 23
        public async Task ToggleActive_Owner_Succeeds()
        {
            var owner = await SeedUser(1, "alice");
            await SeedTemplate(210, createdByUserId: 1, isActive: true);

            var response = await _client.SendAsync(Authorized(HttpMethod.Patch, "/api/v1/workouttemplates/210/toggle-active", owner));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var context = GetContext();
            Assert.False((await context.WorkoutTemplates.FindAsync(210))!.IsActive);
        }

        [Fact] // 24
        public async Task ToggleActive_Stranger_Rejected()
        {
            await SeedUser(1, "alice");
            var stranger = await SeedUser(2, "bob");
            await SeedTemplate(211, createdByUserId: 1, isPublic: true, isActive: true);

            var response = await _client.SendAsync(Authorized(HttpMethod.Patch, "/api/v1/workouttemplates/211/toggle-active", stranger));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            using var context = GetContext();
            Assert.True((await context.WorkoutTemplates.FindAsync(211))!.IsActive);
        }

        [Fact] // 25
        public async Task DeleteTemplate_Owner_Succeeds()
        {
            var owner = await SeedUser(1, "alice");
            await SeedTemplate(220, createdByUserId: 1);

            var response = await _client.SendAsync(Authorized(HttpMethod.Delete, "/api/v1/workouttemplates/220", owner));
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var context = GetContext();
            Assert.Null(await context.WorkoutTemplates.FindAsync(220));
        }

        [Fact] // 26
        public async Task DeleteTemplate_Stranger_Rejected()
        {
            await SeedUser(1, "alice");
            var stranger = await SeedUser(2, "bob");
            await SeedTemplate(221, createdByUserId: 1, isPublic: true);

            var response = await _client.SendAsync(Authorized(HttpMethod.Delete, "/api/v1/workouttemplates/221", stranger));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            using var context = GetContext();
            Assert.NotNull(await context.WorkoutTemplates.FindAsync(221));
        }

        [Fact] // 27
        public async Task DeleteTemplate_SystemTemplate_Rejected()
        {
            var user = await SeedUser(1, "alice");
            await SeedTemplate(222, createdByUserId: null);

            var response = await _client.SendAsync(Authorized(HttpMethod.Delete, "/api/v1/workouttemplates/222", user));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            using var context = GetContext();
            Assert.NotNull(await context.WorkoutTemplates.FindAsync(222));
        }

        [Theory] // write-verb existence disclosure: a foreign PRIVATE template is 404 (== missing), a foreign PUBLIC one is 403
        [InlineData("PUT")]
        [InlineData("PATCH")]
        [InlineData("DELETE")]
        public async Task MutatingVerbs_ForeignPrivateTemplate_NotFound_ForeignPublicTemplate_Forbidden(string verb)
        {
            await SeedUser(1, "alice");
            var stranger = await SeedUser(2, "bob");
            await SeedTemplate(230, createdByUserId: 1, isPublic: false, name: "Private");
            await SeedTemplate(231, createdByUserId: 1, isPublic: true, name: "Public");

            HttpRequestMessage Req(int id)
            {
                var url = verb == "PATCH"
                    ? $"/api/v1/workouttemplates/{id}/toggle-active"
                    : $"/api/v1/workouttemplates/{id}";
                var method = verb switch { "PUT" => HttpMethod.Put, "PATCH" => HttpMethod.Patch, _ => HttpMethod.Delete };
                var r = Authorized(method, url, stranger);
                if (verb == "PUT")
                {
                    r.Content = JsonContent.Create(new { name = "x", exercisesJson = "[]", recurrencePattern = "daily", isActive = true, isPublic = false });
                }
                return r;
            }

            var privateResp = await _client.SendAsync(Req(230));
            var publicResp = await _client.SendAsync(Req(231));
            var missingResp = await _client.SendAsync(Req(999123));

            Assert.Equal(HttpStatusCode.NotFound, privateResp.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, missingResp.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, publicResp.StatusCode);

            // The foreign-private 404 must be structurally identical to the missing-id 404 (no
            // oracle): same status/title, same body length (only the per-request traceId varies).
            var priv = await privateResp.Content.ReadFromJsonAsync<JsonElement>();
            var miss = await missingResp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(miss.GetProperty("status").GetInt32(), priv.GetProperty("status").GetInt32());
            Assert.Equal(miss.GetProperty("title").GetString(), priv.GetProperty("title").GetString());
            Assert.Equal(miss.ToString().Length, priv.ToString().Length);

            using var context = GetContext();
            Assert.Equal("Private", (await context.WorkoutTemplates.FindAsync(230))!.Name);
            Assert.Equal("Public", (await context.WorkoutTemplates.FindAsync(231))!.Name);
        }

        // =========================================================================
        // Usage / rating (spec 28-38)
        // =========================================================================

        [Fact] // 28
        public async Task IncrementUsage_Owner_Succeeds()
        {
            var owner = await SeedUser(1, "alice");
            await SeedTemplate(300, createdByUserId: 1, usageCount: 4);

            var response = await _client.SendAsync(Authorized(HttpMethod.Post, "/api/v1/workouttemplates/300/increment-usage", owner));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var context = GetContext();
            Assert.Equal(5, (await context.WorkoutTemplates.FindAsync(300))!.UsageCount);
        }

        [Fact] // 29
        public async Task IncrementUsage_Stranger_PublicOrPrivateTemplate_Rejected()
        {
            await SeedUser(1, "alice");
            var stranger = await SeedUser(2, "bob");
            await SeedTemplate(301, createdByUserId: 1, isPublic: true, usageCount: 0);
            await SeedTemplate(302, createdByUserId: 1, isPublic: false, usageCount: 0);

            var publicResp = await _client.SendAsync(Authorized(HttpMethod.Post, "/api/v1/workouttemplates/301/increment-usage", stranger));
            var privateResp = await _client.SendAsync(Authorized(HttpMethod.Post, "/api/v1/workouttemplates/302/increment-usage", stranger));

            Assert.Equal(HttpStatusCode.Forbidden, publicResp.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, privateResp.StatusCode); // private stays indistinguishable from missing

            using var context = GetContext();
            Assert.Equal(0, (await context.WorkoutTemplates.FindAsync(301))!.UsageCount);
            Assert.Equal(0, (await context.WorkoutTemplates.FindAsync(302))!.UsageCount);
        }

        [Fact] // 30
        public async Task IncrementUsage_SystemTemplate_Rejected()
        {
            var user = await SeedUser(1, "alice");
            await SeedTemplate(303, createdByUserId: null, usageCount: 0);

            var response = await _client.SendAsync(Authorized(HttpMethod.Post, "/api/v1/workouttemplates/303/increment-usage", user));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            using var context = GetContext();
            Assert.Equal(0, (await context.WorkoutTemplates.FindAsync(303))!.UsageCount);
        }

        [Fact] // 31
        public async Task IncrementUsage_UsesUtc_AndChangesOnlyUsageCountAndLastUsedAt()
        {
            var owner = await SeedUser(1, "alice");
            await SeedTemplate(304, createdByUserId: 1, name: "Fixed", isActive: true, usageCount: 1, rating: 3.0, ratingCount: 2);

            var before = DateTime.UtcNow.AddSeconds(-1);
            var response = await _client.SendAsync(Authorized(HttpMethod.Post, "/api/v1/workouttemplates/304/increment-usage", owner));
            var after = DateTime.UtcNow.AddSeconds(1);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var context = GetContext();
            var t = await context.WorkoutTemplates.FindAsync(304);
            Assert.Equal(2, t!.UsageCount);
            Assert.NotNull(t.LastUsedAt);
            Assert.InRange(t.LastUsedAt!.Value, before, after);
            Assert.Equal(DateTimeKind.Utc, t.LastUsedAt.Value.Kind);
            // Untouched fields
            Assert.Equal("Fixed", t.Name);
            Assert.True(t.IsActive);
            Assert.Equal(3.0, t.Rating);
            Assert.Equal(2, t.RatingCount);
        }

        [Fact] // 32
        public async Task RateTemplate_VisiblePublicOrSystemTemplate_Succeeds()
        {
            await SeedUser(1, "alice");
            var rater = await SeedUser(2, "bob");
            await SeedTemplate(310, createdByUserId: 1, isPublic: true);
            await SeedTemplate(311, createdByUserId: null);

            var publicResp = await _client.SendAsync(AuthorizedJson(HttpMethod.Post, "/api/v1/workouttemplates/310/rate", rater, new { rating = 4.0 }));
            var systemResp = await _client.SendAsync(AuthorizedJson(HttpMethod.Post, "/api/v1/workouttemplates/311/rate", rater, new { rating = 5.0 }));

            Assert.Equal(HttpStatusCode.OK, publicResp.StatusCode);
            Assert.Equal(HttpStatusCode.OK, systemResp.StatusCode);
        }

        [Fact] // 33
        public async Task RateTemplate_HiddenPrivateTemplate_NotFound()
        {
            await SeedUser(1, "alice");
            var stranger = await SeedUser(2, "bob");
            await SeedTemplate(312, createdByUserId: 1, isPublic: false);

            var response = await _client.SendAsync(AuthorizedJson(HttpMethod.Post, "/api/v1/workouttemplates/312/rate", stranger, new { rating = 4.0 }));
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            using var context = GetContext();
            Assert.Empty(context.WorkoutTemplateRatings.Where(r => r.WorkoutTemplateId == 312));
        }

        [Fact] // 34
        public async Task RateTemplate_OwnCustomTemplate_Rejected()
        {
            var owner = await SeedUser(1, "alice");
            await SeedTemplate(313, createdByUserId: 1, isPublic: true);

            var response = await _client.SendAsync(AuthorizedJson(HttpMethod.Post, "/api/v1/workouttemplates/313/rate", owner, new { rating = 5.0 }));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var context = GetContext();
            Assert.Empty(context.WorkoutTemplateRatings.Where(r => r.WorkoutTemplateId == 313));
            Assert.Null((await context.WorkoutTemplates.FindAsync(313))!.Rating);
        }

        [Theory] // 35
        [InlineData(0.5)]
        [InlineData(0)]
        [InlineData(5.5)]
        [InlineData(6)]
        public async Task RateTemplate_OutOfRange_Rejected(double rating)
        {
            await SeedUser(1, "alice");
            var rater = await SeedUser(2, "bob");
            await SeedTemplate(314, createdByUserId: 1, isPublic: true);

            var response = await _client.SendAsync(AuthorizedJson(HttpMethod.Post, "/api/v1/workouttemplates/314/rate", rater, new { rating }));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var context = GetContext();
            Assert.Empty(context.WorkoutTemplateRatings.Where(r => r.WorkoutTemplateId == 314));
        }

        [Fact] // 36
        public async Task RateTemplate_ReRating_UpdatesExistingRowInsteadOfDuplicating()
        {
            await SeedUser(1, "alice");
            var rater = await SeedUser(2, "bob");
            await SeedTemplate(315, createdByUserId: 1, isPublic: true);

            await _client.SendAsync(AuthorizedJson(HttpMethod.Post, "/api/v1/workouttemplates/315/rate", rater, new { rating = 2.0 }));
            await _client.SendAsync(AuthorizedJson(HttpMethod.Post, "/api/v1/workouttemplates/315/rate", rater, new { rating = 4.0 }));

            using var context = GetContext();
            var rows = context.WorkoutTemplateRatings.Where(r => r.WorkoutTemplateId == 315).ToList();
            Assert.Single(rows);
            Assert.Equal(4.0, rows[0].Rating);

            var t = await context.WorkoutTemplates.FindAsync(315);
            Assert.Equal(1, t!.RatingCount);
            Assert.Equal(4.0, t.Rating);
        }

        [Fact] // 37
        public async Task RateTemplate_AggregateAndCount_RemainCorrectAcrossRaters()
        {
            await SeedUser(1, "alice");
            var bob = await SeedUser(2, "bob");
            var carol = await SeedUser(3, "carol");
            await SeedTemplate(316, createdByUserId: 1, isPublic: true);

            await _client.SendAsync(AuthorizedJson(HttpMethod.Post, "/api/v1/workouttemplates/316/rate", bob, new { rating = 2.0 }));
            await _client.SendAsync(AuthorizedJson(HttpMethod.Post, "/api/v1/workouttemplates/316/rate", carol, new { rating = 4.0 }));

            using var context = GetContext();
            var t = await context.WorkoutTemplates.FindAsync(316);
            Assert.Equal(2, t!.RatingCount);
            Assert.Equal(3.0, t.Rating!.Value, 3);

            // Carol re-rates 4 -> 5; average must become (2 + 5) / 2 = 3.5, count still 2.
            await _client.SendAsync(AuthorizedJson(HttpMethod.Post, "/api/v1/workouttemplates/316/rate", carol, new { rating = 5.0 }));
            using var context2 = GetContext();
            var t2 = await context2.WorkoutTemplates.FindAsync(316);
            Assert.Equal(2, t2!.RatingCount);
            Assert.Equal(3.5, t2.Rating!.Value, 3);
        }

        [Fact] // 38
        public async Task RateTemplate_UnauthenticatedOrInvalid_LeavesDatabaseUnchanged()
        {
            await SeedUser(1, "alice");
            await SeedTemplate(317, createdByUserId: 1, isPublic: true);

            // No Authorization header
            var unauth = await _client.PostAsJsonAsync("/api/v1/workouttemplates/317/rate", new { rating = 4.0 });
            Assert.Equal(HttpStatusCode.Unauthorized, unauth.StatusCode);

            using var context = GetContext();
            Assert.Empty(context.WorkoutTemplateRatings);
            Assert.Null((await context.WorkoutTemplates.FindAsync(317))!.Rating);
        }

        // =========================================================================
        // Contract / security (spec 39-45)
        // =========================================================================

        private static readonly string[] ExpectedDtoFields =
        {
            "id", "name", "description", "exercisesJson", "recurrencePattern", "daysOfWeek",
            "intervalDays", "estimatedDuration", "category", "isActive", "isCustom", "isPublic",
            "createdByUserId", "createdByUserName", "usageCount", "rating", "ratingCount",
            "createdAt", "lastUsedAt"
        };

        private static readonly string[] ForbiddenFieldNames =
        {
            "passwordHash", "password", "token", "fcmToken", "securityStamp", "email",
            "createdByUser", "createdByUserEmail", "ratings", "userId", "isCommunity", "updatedAt"
        };

        [Fact] // 39
        public async Task AllResponseProducingEndpoints_ShareTheSameDtoFieldSet()
        {
            var me = await SeedUser(1, "alice");
            var other = await SeedUser(2, "bob");

            var createResp = await _client.SendAsync(AuthorizedJson(HttpMethod.Post, "/api/v1/workouttemplates", me, ValidCreateBody(isPublic: true)));
            var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
            var id = created.GetProperty("id").GetInt32();

            await SeedTemplate(400, createdByUserId: null, name: "System");

            var byId = await (await _client.SendAsync(Authorized(HttpMethod.Get, $"/api/v1/workouttemplates/{id}", me))).Content.ReadFromJsonAsync<JsonElement>();
            var list = (await (await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/workouttemplates", me))).Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().First();
            var community = (await (await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/workouttemplates/community", other))).Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().First();

            var date = DateTime.UtcNow.ToString("o");
            var scheduled = (await (await _client.SendAsync(Authorized(HttpMethod.Get, $"/api/v1/workouttemplates/scheduled?date={Uri.EscapeDataString(date)}", me))).Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().First();

            var expected = ExpectedDtoFields.OrderBy(x => x).ToList();
            foreach (var doc in new[] { created, byId, list, community, scheduled })
            {
                Assert.Equal(expected, doc.EnumerateObject().Select(p => p.Name).OrderBy(x => x).ToList());
            }
        }

        [Fact] // 40
        public async Task NoEndpoint_SerializesRawUserOrSensitiveAccountFields()
        {
            var me = await SeedUser(1, "alice");
            var other = await SeedUser(2, "bob");
            await SeedTemplate(410, createdByUserId: 2, isPublic: true);
            await SeedTemplate(411, createdByUserId: null);
            await SeedTemplate(412, createdByUserId: 1);
            await SeedRating(410, 1, 3.0);

            var urls = new[]
            {
                "/api/v1/workouttemplates",
                "/api/v1/workouttemplates/community",
                "/api/v1/workouttemplates/410",
                $"/api/v1/workouttemplates/scheduled?date={Uri.EscapeDataString(DateTime.UtcNow.ToString("o"))}"
            };

            foreach (var url in urls)
            {
                var raw = await (await _client.SendAsync(Authorized(HttpMethod.Get, url, me))).Content.ReadAsStringAsync();
                Assert.DoesNotContain("super-secret-bcrypt-hash", raw);
                Assert.DoesNotContain("bob@example.com", raw);
                Assert.DoesNotContain("alice@example.com", raw);

                using var doc = JsonDocument.Parse(raw);
                var items = doc.RootElement.ValueKind == JsonValueKind.Array
                    ? doc.RootElement.EnumerateArray()
                    : new[] { doc.RootElement }.AsEnumerable();
                foreach (var item in items)
                {
                    if (item.ValueKind != JsonValueKind.Object) continue;
                    foreach (var forbidden in ForbiddenFieldNames)
                    {
                        Assert.False(item.TryGetProperty(forbidden, out _), $"{url} exposed '{forbidden}'");
                    }
                }
            }
        }

        [Fact] // 41
        public async Task CreatedByUserId_IsNullForSystemTemplates_AndSetForCustom()
        {
            var me = await SeedUser(1, "alice");
            await SeedTemplate(420, createdByUserId: null, name: "System");
            await SeedTemplate(421, createdByUserId: 1, isPublic: true, name: "Custom");

            var system = await (await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/workouttemplates/420", me))).Content.ReadFromJsonAsync<JsonElement>();
            var custom = await (await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/workouttemplates/421", me))).Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(JsonValueKind.Null, system.GetProperty("createdByUserId").ValueKind);
            Assert.Equal(JsonValueKind.Null, system.GetProperty("createdByUserName").ValueKind);
            Assert.Equal(1, custom.GetProperty("createdByUserId").GetInt32());
            Assert.Equal("Name 1", custom.GetProperty("createdByUserName").GetString());
        }

        [Fact] // 42
        public async Task IsPublic_IsPresentAndBoolean()
        {
            var me = await SeedUser(1, "alice");
            await SeedTemplate(430, createdByUserId: 1, isPublic: true);

            var body = await (await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/workouttemplates/430", me))).Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(body.TryGetProperty("isPublic", out var isPublic));
            Assert.Equal(JsonValueKind.True, isPublic.ValueKind);
        }

        [Fact] // 43
        public async Task Response_DoesNotEmitMisleadingUserIdIsCommunityOrUpdatedAtAliases()
        {
            var me = await SeedUser(1, "alice");
            await SeedTemplate(440, createdByUserId: 1, isPublic: true);

            var body = await (await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/workouttemplates/440", me))).Content.ReadFromJsonAsync<JsonElement>();

            Assert.False(body.TryGetProperty("userId", out _));
            Assert.False(body.TryGetProperty("isCommunity", out _));
            Assert.False(body.TryGetProperty("updatedAt", out _));
        }

        [Fact] // 44
        public async Task RequestDto_IgnoresServerOwnedOverposting_OnCreateAndUpdate()
        {
            var me = await SeedUser(1, "alice");
            await SeedUser(2, "bob");

            var createReq = Authorized(HttpMethod.Post, "/api/v1/workouttemplates", me);
            createReq.Content = JsonContent.Create(new
            {
                name = "T", exercisesJson = "[]", recurrencePattern = "daily",
                id = 12345, createdByUserId = 2, isCustom = false, usageCount = 50,
                rating = 5.0, ratingCount = 9, lastUsedAt = "2000-01-01T00:00:00Z", createdAt = "2000-01-01T00:00:00Z"
            });
            var created = await (await _client.SendAsync(createReq)).Content.ReadFromJsonAsync<JsonElement>();
            var id = created.GetProperty("id").GetInt32();
            Assert.NotEqual(12345, id);
            Assert.Equal(1, created.GetProperty("createdByUserId").GetInt32());
            Assert.Equal(0, created.GetProperty("usageCount").GetInt32());

            var updateReq = Authorized(HttpMethod.Put, $"/api/v1/workouttemplates/{id}", me);
            updateReq.Content = JsonContent.Create(new
            {
                name = "T2", exercisesJson = "[]", recurrencePattern = "daily", isActive = true, isPublic = false,
                createdByUserId = 2, usageCount = 77, rating = 1.0, ratingCount = 1
            });
            Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(updateReq)).StatusCode);

            using var context = GetContext();
            var t = await context.WorkoutTemplates.FindAsync(id);
            Assert.Equal(1, t!.CreatedByUserId);
            Assert.Equal(0, t.UsageCount);
            Assert.Null(t.Rating);
        }

        [Fact] // 45
        public async Task UpdateTemplate_Returns204NoContent()
        {
            var owner = await SeedUser(1, "alice");
            await SeedTemplate(450, createdByUserId: 1);

            var response = await _client.SendAsync(AuthorizedJson(HttpMethod.Put, "/api/v1/workouttemplates/450", owner, new
            {
                name = "x", exercisesJson = "[]", recurrencePattern = "daily", isActive = true, isPublic = false
            }));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(0, (await response.Content.ReadAsByteArrayAsync()).Length);
        }

        [Fact]
        public async Task EveryEndpoint_WithoutJwt_Returns401()
        {
            await SeedUser(1, "alice");
            await SeedTemplate(460, createdByUserId: 1);

            var probes = new (HttpMethod method, string url)[]
            {
                (HttpMethod.Get, "/api/v1/workouttemplates"),
                (HttpMethod.Get, "/api/v1/workouttemplates/460"),
                (HttpMethod.Get, "/api/v1/workouttemplates/community"),
                (HttpMethod.Get, $"/api/v1/workouttemplates/scheduled?date={Uri.EscapeDataString(DateTime.UtcNow.ToString("o"))}"),
                (HttpMethod.Post, "/api/v1/workouttemplates"),
                (HttpMethod.Put, "/api/v1/workouttemplates/460"),
                (HttpMethod.Patch, "/api/v1/workouttemplates/460/toggle-active"),
                (HttpMethod.Delete, "/api/v1/workouttemplates/460"),
                (HttpMethod.Post, "/api/v1/workouttemplates/460/increment-usage"),
                (HttpMethod.Post, "/api/v1/workouttemplates/460/rate"),
            };

            foreach (var (method, url) in probes)
            {
                var resp = await _client.SendAsync(new HttpRequestMessage(method, url)
                {
                    Content = method == HttpMethod.Get ? null : JsonContent.Create(new { rating = 3.0 })
                });
                Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
            }
        }

        // =========================================================================
        // Request validation (new 400 paths introduced by this PR)
        // =========================================================================

        [Fact]
        public async Task CreateTemplate_WeeklyWithoutDaysOfWeek_Returns400_AndPersistsNothing()
        {
            var me = await SeedUser(1, "alice");

            var resp = await _client.SendAsync(AuthorizedJson(HttpMethod.Post, "/api/v1/workouttemplates", me, new
            {
                name = "T", exercisesJson = "[]", recurrencePattern = "weekly"
            }));

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            using var context = GetContext();
            Assert.Empty(context.WorkoutTemplates);
        }

        [Fact]
        public async Task CreateTemplate_CustomWithoutIntervalDays_Returns400()
        {
            var me = await SeedUser(1, "alice");
            var resp = await _client.SendAsync(AuthorizedJson(HttpMethod.Post, "/api/v1/workouttemplates", me, new
            {
                name = "T", exercisesJson = "[]", recurrencePattern = "custom"
            }));
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Theory]
        [InlineData("fortnightly")]
        [InlineData("Daily")]
        [InlineData("")]
        public async Task CreateTemplate_UnknownRecurrencePattern_Returns400(string pattern)
        {
            var me = await SeedUser(1, "alice");
            var resp = await _client.SendAsync(AuthorizedJson(HttpMethod.Post, "/api/v1/workouttemplates", me, new
            {
                name = "T", exercisesJson = "[]", recurrencePattern = pattern
            }));
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(400)]
        public async Task CreateTemplate_IntervalDaysOutOfRange_Returns400(int interval)
        {
            var me = await SeedUser(1, "alice");
            var resp = await _client.SendAsync(AuthorizedJson(HttpMethod.Post, "/api/v1/workouttemplates", me, new
            {
                name = "T", exercisesJson = "[]", recurrencePattern = "custom", intervalDays = interval
            }));
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        [InlineData(5000)]
        public async Task CreateTemplate_EstimatedDurationOutOfRange_Returns400(int minutes)
        {
            var me = await SeedUser(1, "alice");
            var resp = await _client.SendAsync(AuthorizedJson(HttpMethod.Post, "/api/v1/workouttemplates", me, new
            {
                name = "T", exercisesJson = "[]", recurrencePattern = "daily", estimatedDuration = minutes
            }));
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Theory]
        [InlineData("")]
        [InlineData("1,x,3")]
        [InlineData("8")]     // out of 1-7 range (caught by ValidateRecurrence for weekly)
        public async Task CreateTemplate_MalformedDaysOfWeek_Returns400(string days)
        {
            var me = await SeedUser(1, "alice");
            var resp = await _client.SendAsync(AuthorizedJson(HttpMethod.Post, "/api/v1/workouttemplates", me, new
            {
                name = "T", exercisesJson = "[]", recurrencePattern = "weekly", daysOfWeek = days
            }));
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task CreateTemplate_NameOverMaxLength_Returns400()
        {
            var me = await SeedUser(1, "alice");
            var resp = await _client.SendAsync(AuthorizedJson(HttpMethod.Post, "/api/v1/workouttemplates", me, new
            {
                name = new string('x', 101), exercisesJson = "[]", recurrencePattern = "daily"
            }));
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task CreateTemplate_MissingRequiredFields_Returns400()
        {
            var me = await SeedUser(1, "alice");
            // no name, no exercisesJson, no recurrencePattern
            var resp = await _client.SendAsync(AuthorizedJson(HttpMethod.Post, "/api/v1/workouttemplates", me, new { description = "only this" }));
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task UpdateTemplate_WeeklyWithoutDays_Returns400_AndLeavesRowUnchanged()
        {
            var owner = await SeedUser(1, "alice");
            await SeedTemplate(470, createdByUserId: 1, name: "Original", recurrencePattern: "daily");

            var resp = await _client.SendAsync(AuthorizedJson(HttpMethod.Put, "/api/v1/workouttemplates/470", owner, new
            {
                name = "Changed", exercisesJson = "[]", recurrencePattern = "weekly", isActive = true, isPublic = false
            }));

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            using var context = GetContext();
            var t = await context.WorkoutTemplates.FindAsync(470);
            Assert.Equal("Original", t!.Name);
            Assert.Equal("daily", t.RecurrencePattern);
        }

        // =========================================================================
        // Ad-hoc response bodies + createdAt formatting + Location header
        // =========================================================================

        [Fact]
        public async Task ToggleActive_ResponseBody_IsExactlyIsActiveFlag()
        {
            var owner = await SeedUser(1, "alice");
            await SeedTemplate(480, createdByUserId: 1, isActive: true);

            var resp = await _client.SendAsync(Authorized(HttpMethod.Patch, "/api/v1/workouttemplates/480/toggle-active", owner));
            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(new[] { "isActive" }, body.EnumerateObject().Select(p => p.Name).ToArray());
            Assert.False(body.GetProperty("isActive").GetBoolean());
        }

        [Fact]
        public async Task IncrementUsage_ResponseBody_IsExactlyUsageCount()
        {
            var owner = await SeedUser(1, "alice");
            await SeedTemplate(481, createdByUserId: 1, usageCount: 2);

            var resp = await _client.SendAsync(Authorized(HttpMethod.Post, "/api/v1/workouttemplates/481/increment-usage", owner));
            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(new[] { "usageCount" }, body.EnumerateObject().Select(p => p.Name).ToArray());
            Assert.Equal(3, body.GetProperty("usageCount").GetInt32());
        }

        [Fact]
        public async Task RateTemplate_ResponseBody_IsExactlyRatingAndRatingCount()
        {
            await SeedUser(1, "alice");
            var rater = await SeedUser(2, "bob");
            await SeedTemplate(482, createdByUserId: 1, isPublic: true);

            var resp = await _client.SendAsync(AuthorizedJson(HttpMethod.Post, "/api/v1/workouttemplates/482/rate", rater, new { rating = 4.0 }));
            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(new[] { "rating", "ratingCount" }, body.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToArray());
            Assert.Equal(4.0, body.GetProperty("rating").GetDouble());
            Assert.Equal(1, body.GetProperty("ratingCount").GetInt32());
        }

        [Fact]
        public async Task CreateTemplate_SetsLocationHeaderToTheNewTemplate()
        {
            var me = await SeedUser(1, "alice");

            var resp = await _client.SendAsync(AuthorizedJson(HttpMethod.Post, "/api/v1/workouttemplates", me, ValidCreateBody()));
            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

            var created = await resp.Content.ReadFromJsonAsync<JsonElement>();
            var id = created.GetProperty("id").GetInt32();

            Assert.NotNull(resp.Headers.Location);
            var location = resp.Headers.Location!.ToString();
            Assert.Contains($"/workouttemplates/{id}", location, StringComparison.OrdinalIgnoreCase);

            // The Location URL is genuinely resolvable.
            var followed = await _client.SendAsync(Authorized(HttpMethod.Get, location, me));
            Assert.Equal(HttpStatusCode.OK, followed.StatusCode);
        }

        [Fact]
        public async Task Response_CreatedAt_IsUtcIso8601WithZ()
        {
            var me = await SeedUser(1, "alice");
            await SeedTemplate(490, createdByUserId: 1, isPublic: true, lastUsedAt: DateTime.UtcNow);

            var raw = await (await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/workouttemplates/490", me))).Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(raw);
            var createdAt = doc.RootElement.GetProperty("createdAt").GetString()!;
            var lastUsedAt = doc.RootElement.GetProperty("lastUsedAt").GetString()!;

            Assert.EndsWith("Z", createdAt);
            Assert.EndsWith("Z", lastUsedAt);
            Assert.Equal(DateTimeKind.Utc, DateTime.Parse(createdAt, null, System.Globalization.DateTimeStyles.RoundtripKind).Kind);
        }
    }
}
