using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Full-pipeline tests for the nutrition dashboard/progress endpoints proving that
    /// consumed/planned totals are derived live from MealLog/MealEntry data through the
    /// shared NutritionProgressCalculator, instead of the persisted NutritionProgress
    /// aggregate that could silently drift from actual meal data (nutrition audit blocking
    /// finding #1). Mirrors the ExerciseTemplatesAuthorizationTests minimal-test-host pattern
    /// so requests go through real routing, [Authorize] JWT auth, and real JSON serialization.
    /// </summary>
    public class NutritionProgressCalculationTests : IAsyncLifetime
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
                            .AddApplicationPart(typeof(NutritionGoalsController).Assembly)
                            .AddJsonOptions(options =>
                            {
                                // Mirror Program.cs JSON configuration so these tests prove the
                                // real wire contract, not just default serializer behavior.
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

        private async Task<NutritionGoal> SeedActiveGoal(int userId, decimal dailyCalories = 2000)
        {
            using var context = GetContext();
            var goal = new NutritionGoal
            {
                UserId = userId,
                Name = "Test Goal",
                DailyCalories = dailyCalories,
                DailyProtein = 150,
                DailyCarbohydrates = 200,
                DailyFat = 65,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.NutritionGoals.Add(goal);
            await context.SaveChangesAsync();
            return goal;
        }

        private static DateTime Utc(int year, int month, int day) =>
            DateTime.SpecifyKind(new DateTime(year, month, day), DateTimeKind.Utc);

        private async Task<MealLog> SeedMealLog(int userId, DateTime date, decimal waterIntake, params MealEntry[] entries)
        {
            using var context = GetContext();
            var mealLog = new MealLog
            {
                UserId = userId,
                Date = date,
                WaterIntake = waterIntake,
                CreatedAt = DateTime.UtcNow
            };
            foreach (var entry in entries)
            {
                entry.CreatedAt = DateTime.UtcNow;
                mealLog.MealEntries.Add(entry);
            }
            context.MealLogs.Add(mealLog);
            await context.SaveChangesAsync();
            return mealLog;
        }

        private Task<MealLog> SeedMealLog(int userId, DateTime date, params MealEntry[] entries) =>
            SeedMealLog(userId, date, waterIntake: 0, entries);

        private static MealEntry Entry(string mealType, bool isConsumed, decimal calories, decimal protein = 0, decimal carbs = 0, decimal fat = 0, decimal fiber = 0) =>
            new MealEntry
            {
                MealType = mealType,
                IsConsumed = isConsumed,
                TotalCalories = calories,
                TotalProtein = protein,
                TotalCarbohydrates = carbs,
                TotalFat = fat,
                TotalFiber = fiber
            };

        private async Task SeedStaleNutritionProgress(int userId, DateTime date, int? nutritionGoalId, decimal plannedCalories, decimal consumedCalories)
        {
            using var context = GetContext();
            context.NutritionProgresses.Add(new NutritionProgress
            {
                UserId = userId,
                Date = date,
                NutritionGoalId = nutritionGoalId,
                PlannedCalories = plannedCalories,
                ConsumedCalories = consumedCalories,
                CreatedAt = DateTime.UtcNow.AddDays(-3)
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

        private async Task<JsonElement> GetJson(string url, User user)
        {
            var response = await _client.SendAsync(Authorized(HttpMethod.Get, url, user));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(body).RootElement;
        }

        private static string DashboardUrl(DateTime date) => $"/api/v1/nutritiongoals/dashboard?date={date:yyyy-MM-dd}";
        private static string ProgressByDateUrl(DateTime date) => $"/api/v1/nutritiongoals/progress/date/{date:yyyy-MM-dd}";
        private const string TodayProgressUrl = "/api/v1/nutritiongoals/progress/today";

        // 1. No MealLog -> zero consumed and planned.
        [Fact]
        public async Task Dashboard_NoMealLog_ReturnsZeroConsumedAndPlanned()
        {
            var user = await SeedUser(1, "alice");
            var date = Utc(2026, 3, 10);

            var json = await GetJson(DashboardUrl(date), user);
            var progress = json.GetProperty("progress");

            Assert.Equal(0, progress.GetProperty("consumedCalories").GetDecimal());
            Assert.Equal(0, progress.GetProperty("plannedCalories").GetDecimal());
        }

        // 2. Empty MealLog (entries exist but all totals are zero) -> zero consumed and planned.
        [Fact]
        public async Task Dashboard_EmptyMealLog_ReturnsZeroConsumedAndPlanned()
        {
            var user = await SeedUser(1, "alice");
            var date = Utc(2026, 3, 10);
            await SeedMealLog(user.Id, date,
                Entry(MealTypes.Breakfast, isConsumed: false, calories: 0),
                Entry(MealTypes.Lunch, isConsumed: false, calories: 0));

            var json = await GetJson(DashboardUrl(date), user);
            var progress = json.GetProperty("progress");

            Assert.Equal(0, progress.GetProperty("consumedCalories").GetDecimal());
            Assert.Equal(0, progress.GetProperty("plannedCalories").GetDecimal());
        }

        // 3. Unconsumed entry contributes to planned but not consumed.
        [Fact]
        public async Task Dashboard_UnconsumedEntry_ContributesToPlannedNotConsumed()
        {
            var user = await SeedUser(1, "alice");
            var date = Utc(2026, 3, 10);
            await SeedMealLog(user.Id, date,
                Entry(MealTypes.Breakfast, isConsumed: false, calories: 400, protein: 20, carbs: 40, fat: 10));

            var json = await GetJson(DashboardUrl(date), user);
            var progress = json.GetProperty("progress");

            Assert.Equal(400, progress.GetProperty("plannedCalories").GetDecimal());
            Assert.Equal(0, progress.GetProperty("consumedCalories").GetDecimal());
        }

        // 4. Consumed entry contributes to both planned and consumed.
        [Fact]
        public async Task Dashboard_ConsumedEntry_ContributesToPlannedAndConsumed()
        {
            var user = await SeedUser(1, "alice");
            var date = Utc(2026, 3, 10);
            await SeedMealLog(user.Id, date,
                Entry(MealTypes.Breakfast, isConsumed: true, calories: 500, protein: 30, carbs: 50, fat: 15));

            var json = await GetJson(DashboardUrl(date), user);
            var progress = json.GetProperty("progress");

            Assert.Equal(500, progress.GetProperty("plannedCalories").GetDecimal());
            Assert.Equal(500, progress.GetProperty("consumedCalories").GetDecimal());
            Assert.Equal(30, progress.GetProperty("consumedProtein").GetDecimal());
            Assert.Equal(50, progress.GetProperty("consumedCarbohydrates").GetDecimal());
            Assert.Equal(15, progress.GetProperty("consumedFat").GetDecimal());
        }

        // 5. Mixed consumed/unconsumed entries calculate correctly.
        [Fact]
        public async Task Dashboard_MixedEntries_CalculatesCorrectly()
        {
            var user = await SeedUser(1, "alice");
            var date = Utc(2026, 3, 10);
            await SeedMealLog(user.Id, date,
                Entry(MealTypes.Breakfast, isConsumed: true, calories: 300),
                Entry(MealTypes.Lunch, isConsumed: true, calories: 600),
                Entry(MealTypes.Dinner, isConsumed: false, calories: 700),
                Entry(MealTypes.Snack, isConsumed: false, calories: 200));

            var json = await GetJson(DashboardUrl(date), user);
            var progress = json.GetProperty("progress");

            Assert.Equal(900, progress.GetProperty("consumedCalories").GetDecimal());
            Assert.Equal(1800, progress.GetProperty("plannedCalories").GetDecimal());
        }

        // 6. Editing the stored total of a consumed entry is reflected immediately (proves
        // totals are calculated live, not read from a stale persisted aggregate).
        [Fact]
        public async Task Dashboard_EditingConsumedEntryTotal_ReflectedImmediately()
        {
            var user = await SeedUser(1, "alice");
            var date = Utc(2026, 3, 10);
            var mealLog = await SeedMealLog(user.Id, date,
                Entry(MealTypes.Breakfast, isConsumed: true, calories: 500));

            var before = await GetJson(DashboardUrl(date), user);
            Assert.Equal(500, before.GetProperty("progress").GetProperty("consumedCalories").GetDecimal());

            using (var context = GetContext())
            {
                var entry = await context.MealEntries.FirstAsync(e => e.MealLogId == mealLog.Id);
                entry.TotalCalories = 850;
                await context.SaveChangesAsync();
            }

            var after = await GetJson(DashboardUrl(date), user);
            Assert.Equal(850, after.GetProperty("progress").GetProperty("consumedCalories").GetDecimal());
            Assert.Equal(850, after.GetProperty("progress").GetProperty("plannedCalories").GetDecimal());
        }

        // 7. Deleting/removing an entry is reflected immediately.
        [Fact]
        public async Task Dashboard_DeletingEntry_ReflectedImmediately()
        {
            var user = await SeedUser(1, "alice");
            var date = Utc(2026, 3, 10);
            var mealLog = await SeedMealLog(user.Id, date,
                Entry(MealTypes.Breakfast, isConsumed: true, calories: 400),
                Entry(MealTypes.Lunch, isConsumed: true, calories: 600));

            var before = await GetJson(DashboardUrl(date), user);
            Assert.Equal(1000, before.GetProperty("progress").GetProperty("consumedCalories").GetDecimal());

            using (var context = GetContext())
            {
                var lunch = await context.MealEntries.FirstAsync(e => e.MealLogId == mealLog.Id && e.MealType == MealTypes.Lunch);
                context.MealEntries.Remove(lunch);
                await context.SaveChangesAsync();
            }

            var after = await GetJson(DashboardUrl(date), user);
            Assert.Equal(400, after.GetProperty("progress").GetProperty("consumedCalories").GetDecimal());
        }

        // 8. Values above the goal remain mathematically correct (not clamped).
        [Fact]
        public async Task Dashboard_ConsumedAboveGoal_IsNotClamped()
        {
            var user = await SeedUser(1, "alice");
            await SeedActiveGoal(user.Id, dailyCalories: 1500);
            var date = Utc(2026, 3, 10);
            await SeedMealLog(user.Id, date,
                Entry(MealTypes.Breakfast, isConsumed: true, calories: 1200),
                Entry(MealTypes.Lunch, isConsumed: true, calories: 900));

            var json = await GetJson(DashboardUrl(date), user);
            var progress = json.GetProperty("progress");

            Assert.Equal(2100, progress.GetProperty("consumedCalories").GetDecimal());
            Assert.Equal(1500, json.GetProperty("goal").GetProperty("dailyCalories").GetDecimal());
        }

        // 9. A requested historical date uses that date's MealLog, not today's or any other date's.
        [Fact]
        public async Task ProgressByDate_HistoricalDate_UsesThatDatesMealLog()
        {
            var user = await SeedUser(1, "alice");
            var today = Utc(2026, 3, 10);
            var yesterday = Utc(2026, 3, 9);
            await SeedMealLog(user.Id, today, Entry(MealTypes.Breakfast, isConsumed: true, calories: 300));
            await SeedMealLog(user.Id, yesterday, Entry(MealTypes.Dinner, isConsumed: true, calories: 777));

            var json = await GetJson(ProgressByDateUrl(yesterday), user);

            Assert.Equal(777, json.GetProperty("consumedCalories").GetDecimal());
            Assert.Equal("2026-03-09", json.GetProperty("date").GetString());
        }

        // 10. Another user's MealLog/entries must never be included.
        [Fact]
        public async Task Dashboard_AnotherUsersMealLog_IsExcluded()
        {
            var alice = await SeedUser(1, "alice");
            var bob = await SeedUser(2, "bob");
            var date = Utc(2026, 3, 10);

            await SeedMealLog(bob.Id, date, Entry(MealTypes.Breakfast, isConsumed: true, calories: 5000));
            // Alice has no meal log for this date at all.

            var json = await GetJson(DashboardUrl(date), alice);
            var progress = json.GetProperty("progress");

            Assert.Equal(0, progress.GetProperty("consumedCalories").GetDecimal());
            Assert.Equal(0, progress.GetProperty("plannedCalories").GetDecimal());
        }

        // 11. Dashboard and progress/today return identical consumed totals for the same user/date.
        [Fact]
        public async Task Dashboard_And_TodayProgress_ReturnIdenticalConsumedTotals()
        {
            var user = await SeedUser(1, "alice");
            var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            await SeedMealLog(user.Id, today,
                Entry(MealTypes.Breakfast, isConsumed: true, calories: 350),
                Entry(MealTypes.Lunch, isConsumed: false, calories: 650));

            var dashboard = await GetJson(DashboardUrl(today), user);
            var todayProgress = await GetJson(TodayProgressUrl, user);

            var dashboardConsumed = dashboard.GetProperty("progress").GetProperty("consumedCalories").GetDecimal();
            var todayConsumed = todayProgress.GetProperty("consumedCalories").GetDecimal();

            Assert.Equal(350, dashboardConsumed);
            Assert.Equal(dashboardConsumed, todayConsumed);

            var dashboardPlanned = dashboard.GetProperty("progress").GetProperty("plannedCalories").GetDecimal();
            var todayPlanned = todayProgress.GetProperty("plannedCalories").GetDecimal();
            Assert.Equal(1000, dashboardPlanned);
            Assert.Equal(dashboardPlanned, todayPlanned);
        }

        // 12. Existing stale NutritionProgress values must be ignored entirely.
        // This test would FAIL against the old implementation, which read NutritionProgresses directly.
        [Fact]
        public async Task Dashboard_IgnoresStaleNutritionProgressRow()
        {
            var user = await SeedUser(1, "alice");
            var goal = await SeedActiveGoal(user.Id);
            var date = Utc(2026, 3, 10);

            await SeedMealLog(user.Id, date, Entry(MealTypes.Breakfast, isConsumed: true, calories: 425));
            // A stale, wildly wrong persisted row for the same user/date.
            await SeedStaleNutritionProgress(user.Id, date, goal.Id, plannedCalories: 99999, consumedCalories: 88888);

            var json = await GetJson(DashboardUrl(date), user);
            var progress = json.GetProperty("progress");

            Assert.Equal(425, progress.GetProperty("consumedCalories").GetDecimal());
            Assert.Equal(425, progress.GetProperty("plannedCalories").GetDecimal());
            Assert.NotEqual(88888, progress.GetProperty("consumedCalories").GetDecimal());
            Assert.NotEqual(99999, progress.GetProperty("plannedCalories").GetDecimal());
        }

        // Fiber and water are real, actively-tracked fields (MealEntry.TotalFiber is
        // computed from FoodItems; MealLog.WaterIntake is written via PUT .../water) and
        // must not be silently hardcoded to zero now that totals are derived live.
        [Fact]
        public async Task Dashboard_FiberAndWater_AreDerivedFromMealLog()
        {
            var user = await SeedUser(1, "alice");
            var date = Utc(2026, 3, 10);
            await SeedMealLog(user.Id, date, waterIntake: 750,
                Entry(MealTypes.Breakfast, isConsumed: true, calories: 300, fiber: 8),
                Entry(MealTypes.Lunch, isConsumed: false, calories: 400, fiber: 6));

            var json = await GetJson(DashboardUrl(date), user);
            var progress = json.GetProperty("progress");

            Assert.Equal(8, progress.GetProperty("consumedFiber").GetDecimal());
            Assert.Equal(14, progress.GetProperty("plannedFiber").GetDecimal());
            Assert.Equal(750, progress.GetProperty("consumedWater").GetDecimal());
        }

        // 13. Response JSON field names/types remain compatible with GoHardAPP's
        // DailyNutritionProgress/NutritionDashboardData Dart models.
        [Fact]
        public async Task Dashboard_ResponseContract_MatchesFlutterExpectedShape()
        {
            var user = await SeedUser(1, "alice");
            var goal = await SeedActiveGoal(user.Id);
            var date = Utc(2026, 3, 10);
            await SeedMealLog(user.Id, date, waterIntake: 500,
                Entry(MealTypes.Breakfast, isConsumed: true, calories: 100, protein: 5, carbs: 10, fat: 2, fiber: 3));

            var json = await GetJson(DashboardUrl(date), user);

            // Top-level NutritionDashboardData contract: date, goal, progress.
            Assert.True(json.TryGetProperty("date", out _));
            Assert.True(json.TryGetProperty("goal", out var goalJson));
            Assert.True(json.TryGetProperty("progress", out var progress));
            Assert.Equal(goal.Id, goalJson.GetProperty("id").GetInt32());

            // DailyNutritionProgress contract: exact camelCase field names json_serializable expects.
            string[] requiredFields =
            {
                "id", "userId", "date", "nutritionGoalId",
                "plannedCalories", "plannedProtein", "plannedCarbohydrates", "plannedFat", "plannedFiber", "plannedWater",
                "consumedCalories", "consumedProtein", "consumedCarbohydrates", "consumedFat", "consumedFiber", "consumedWater",
                "createdAt"
            };
            foreach (var field in requiredFields)
            {
                Assert.True(progress.TryGetProperty(field, out _), $"Missing expected field '{field}' in progress response");
            }

            // Numeric fields must be numbers (json_serializable does `as num?`), not strings.
            Assert.Equal(JsonValueKind.Number, progress.GetProperty("consumedCalories").ValueKind);
            Assert.Equal(JsonValueKind.Number, progress.GetProperty("plannedCalories").ValueKind);
            Assert.Equal(JsonValueKind.Number, progress.GetProperty("userId").ValueKind);

            // Date is date-only "yyyy-MM-dd" per DateOnlyJsonConverter, parseable by DateTime.parse in Dart.
            Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", progress.GetProperty("date").GetString());

            // nutritionGoalId is present and links to the active goal (nullable int in Dart).
            Assert.Equal(goal.Id, progress.GetProperty("nutritionGoalId").GetInt32());

            // Fiber/water must carry real derived values, not a hardcoded placeholder zero.
            Assert.Equal(3, progress.GetProperty("consumedFiber").GetDecimal());
            Assert.Equal(500, progress.GetProperty("consumedWater").GetDecimal());
        }

        // 13b. progress/today and progress/date/{date} return the DTO shape directly (not
        // wrapped), matching how NutritionProgress previously round-tripped through
        // NutritionProvider.todaysProgress.
        [Fact]
        public async Task TodayProgress_ResponseContract_IsUnwrappedDto()
        {
            var user = await SeedUser(1, "alice");
            await SeedMealLog(user.Id, DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc),
                Entry(MealTypes.Breakfast, isConsumed: true, calories: 200));

            var json = await GetJson(TodayProgressUrl, user);

            Assert.True(json.TryGetProperty("consumedCalories", out _));
            Assert.True(json.TryGetProperty("plannedCalories", out _));
            Assert.False(json.TryGetProperty("progress", out _), "progress/today must return the DTO directly, not wrapped");
        }
    }
}
