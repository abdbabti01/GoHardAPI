using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
using System.Text.Json;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Full-pipeline tests for SharedWorkoutsController's friends-only visibility rule.
    /// Mirrors the ExerciseTemplatesAuthorizationTests minimal-test-host pattern: direct
    /// controller instantiation bypasses [Authorize] and routing and cannot prove 401/404s,
    /// so this spins up a real ASP.NET Core pipeline (auth middleware + routing) against an
    /// EF Core InMemory database, one per test class instance.
    /// </summary>
    public class SharedWorkoutsAuthorizationTests : IAsyncLifetime
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
                            .AddApplicationPart(typeof(SharedWorkoutsController).Assembly);
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

        private async Task SeedFriendship(int requesterId, int addresseeId, string status)
        {
            using var context = GetContext();
            context.Friendships.Add(new Friendship
            {
                RequesterId = requesterId,
                AddresseeId = addresseeId,
                Status = status,
                RequestedAt = DateTime.UtcNow,
                RespondedAt = status == "pending" ? null : DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        private async Task<SharedWorkout> SeedSharedWorkout(int id, int sharedByUserId, string workoutName = "Leg Day")
        {
            using var context = GetContext();
            var workout = new SharedWorkout
            {
                Id = id,
                OriginalId = 1,
                Type = "session",
                SharedByUserId = sharedByUserId,
                WorkoutName = workoutName,
                ExercisesJson = "[]",
                Duration = 30,
                Category = "Strength",
                SharedAt = DateTime.UtcNow
            };
            context.SharedWorkouts.Add(workout);
            await context.SaveChangesAsync();
            return workout;
        }

        // Seeds a SharedWorkoutSave row directly, bypassing ToggleSave's own visibility check -
        // needed to set up a "stale" save (one that predates a friendship being revoked) since
        // ToggleSave itself would now correctly refuse to create such a row.
        private async Task SeedSharedWorkoutSave(int sharedWorkoutId, int userId, DateTime? savedAt = null)
        {
            using var context = GetContext();
            context.SharedWorkoutSaves.Add(new SharedWorkoutSave
            {
                SharedWorkoutId = sharedWorkoutId,
                UserId = userId,
                SavedAt = savedAt ?? DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        private async Task SetFriendshipStatus(int userA, int userB, string status)
        {
            using var context = GetContext();
            var friendship = await context.Friendships.FirstAsync(f =>
                (f.RequesterId == userA && f.AddresseeId == userB) ||
                (f.RequesterId == userB && f.AddresseeId == userA));
            friendship.Status = status;
            await context.SaveChangesAsync();
        }

        private async Task RemoveFriendship(int userA, int userB)
        {
            using var context = GetContext();
            var friendship = await context.Friendships.FirstAsync(f =>
                (f.RequesterId == userA && f.AddresseeId == userB) ||
                (f.RequesterId == userB && f.AddresseeId == userA));
            context.Friendships.Remove(friendship);
            await context.SaveChangesAsync();
        }

        private string TokenFor(User user) => _authService.GenerateJwtToken(user);

        private HttpRequestMessage Authorized(HttpMethod method, string url, User user)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor(user));
            return request;
        }

        // --- 1-2: owner and confirmed friend can fetch by id ---

        [Fact]
        public async Task GetSharedWorkout_Owner_ReturnsOk()
        {
            var owner = await SeedUser(1, "alice");
            await SeedSharedWorkout(100, sharedByUserId: 1);

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/100", owner));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetSharedWorkout_ConfirmedFriend_ReturnsOk()
        {
            var owner = await SeedUser(1, "alice");
            var friend = await SeedUser(2, "bob");
            await SeedFriendship(1, 2, "accepted");
            await SeedSharedWorkout(101, sharedByUserId: 1);

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/101", friend));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // --- 3-6: non-friend, pending, declined/removed, and missing all behave identically ---

        [Fact]
        public async Task GetSharedWorkout_NonFriend_ReturnsNotFound()
        {
            await SeedUser(1, "alice");
            var stranger = await SeedUser(2, "bob");
            await SeedSharedWorkout(102, sharedByUserId: 1);

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/102", stranger));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetSharedWorkout_PendingFriendship_ReturnsNotFound()
        {
            await SeedUser(1, "alice");
            var requester = await SeedUser(2, "bob");
            await SeedFriendship(2, 1, "pending");
            await SeedSharedWorkout(103, sharedByUserId: 1);

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/103", requester));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetSharedWorkout_DeclinedFriendship_ReturnsNotFound()
        {
            await SeedUser(1, "alice");
            var declined = await SeedUser(2, "bob");
            await SeedFriendship(2, 1, "declined");
            await SeedSharedWorkout(104, sharedByUserId: 1);

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/104", declined));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetSharedWorkout_RemovedFriendship_ReturnsNotFound()
        {
            // "Removed" friendships have no Friendship row at all (FriendsController.RemoveFriend
            // hard-deletes it) - same domain state as two users who were never friends.
            await SeedUser(1, "alice");
            var removed = await SeedUser(2, "bob");
            await SeedSharedWorkout(105, sharedByUserId: 1);

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/105", removed));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetSharedWorkout_MissingAndHiddenIds_ReturnIdenticalResponses()
        {
            await SeedUser(1, "alice");
            var stranger = await SeedUser(2, "bob");
            await SeedSharedWorkout(106, sharedByUserId: 1);

            var hiddenResponse = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/106", stranger));
            var missingResponse = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/999999", stranger));

            Assert.Equal(HttpStatusCode.NotFound, hiddenResponse.StatusCode);
            Assert.Equal(missingResponse.StatusCode, hiddenResponse.StatusCode);

            // Compare bodies structurally, ignoring the per-request traceId: both are the bare
            // ASP.NET Core 404 problem-details response with no workout-specific detail.
            var hiddenBody = await hiddenResponse.Content.ReadFromJsonAsync<JsonElement>();
            var missingBody = await missingResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(missingBody.GetProperty("status").GetInt32(), hiddenBody.GetProperty("status").GetInt32());
            Assert.Equal(missingBody.GetProperty("title").GetString(), hiddenBody.GetProperty("title").GetString());
            Assert.Equal(missingBody.ToString().Length, hiddenBody.ToString().Length);
        }

        // --- 7-9: list by user id ---

        [Fact]
        public async Task GetSharedWorkoutsByUser_Owner_ReturnsOwnShares()
        {
            var owner = await SeedUser(1, "alice");
            await SeedSharedWorkout(200, sharedByUserId: 1);

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/user/1", owner));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, body.GetArrayLength());
        }

        [Fact]
        public async Task GetSharedWorkoutsByUser_ConfirmedFriend_ReturnsShares()
        {
            await SeedUser(1, "alice");
            var friend = await SeedUser(2, "bob");
            await SeedFriendship(1, 2, "accepted");
            await SeedSharedWorkout(201, sharedByUserId: 1);

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/user/1", friend));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, body.GetArrayLength());
        }

        [Fact]
        public async Task GetSharedWorkoutsByUser_NonFriend_ReturnsSameEmptyListAsNoShares()
        {
            await SeedUser(1, "alice");
            var stranger = await SeedUser(2, "bob");
            await SeedSharedWorkout(202, sharedByUserId: 1);
            // user 3 exists but has never shared anything - the non-enumerating baseline
            await SeedUser(3, "carol");

            var hiddenResponse = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/user/1", stranger));
            var noSharesResponse = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/user/3", stranger));

            Assert.Equal(HttpStatusCode.OK, hiddenResponse.StatusCode);
            var hiddenBody = await hiddenResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(0, hiddenBody.GetArrayLength());

            var noSharesBody = await noSharesResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(0, noSharesBody.GetArrayLength());
        }

        // --- 10: main feed set is unchanged ---

        [Fact]
        public async Task GetSharedWorkouts_FriendsOnly_ReturnsOnlyFriendsSharesNotOwn()
        {
            var me = await SeedUser(1, "alice");
            await SeedUser(2, "friend");
            await SeedUser(3, "stranger");
            await SeedFriendship(1, 2, "accepted");
            await SeedSharedWorkout(300, sharedByUserId: 1, "My Own Workout");
            await SeedSharedWorkout(301, sharedByUserId: 2, "Friend's Workout");
            await SeedSharedWorkout(302, sharedByUserId: 3, "Stranger's Workout");

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts", me));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var ids = body.EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).ToList();

            Assert.Single(ids);
            Assert.Contains(301, ids);
            Assert.DoesNotContain(300, ids); // feed still excludes the caller's own shares
            Assert.DoesNotContain(302, ids); // stranger's share stays hidden
        }

        [Fact]
        public async Task GetSharedWorkouts_FriendsOnlyFalse_IncludesOwnAndFriendsButNeverStrangers()
        {
            // friendsOnly=false has no consumer in GoHardAPP today, but it is a reachable,
            // authenticated query param (e.g. via Swagger or curl) and must not become a
            // full-database enumeration bypass of the friends-only rule.
            var me = await SeedUser(1, "alice");
            await SeedUser(2, "friend");
            await SeedUser(3, "stranger");
            await SeedFriendship(1, 2, "accepted");
            await SeedSharedWorkout(310, sharedByUserId: 1, "My Own Workout");
            await SeedSharedWorkout(311, sharedByUserId: 2, "Friend's Workout");
            await SeedSharedWorkout(312, sharedByUserId: 3, "Stranger's Workout");

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts?friendsOnly=false", me));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var ids = body.EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).ToList();

            Assert.Contains(310, ids); // own shares now included
            Assert.Contains(311, ids); // friend's shares included
            Assert.DoesNotContain(312, ids); // stranger's share still hidden - not a global browse
        }

        // --- 11-14: like/save follow the same visibility rule ---

        [Fact]
        public async Task ToggleLike_OwnerOnVisibleWorkout_Succeeds()
        {
            var owner = await SeedUser(1, "alice");
            await SeedSharedWorkout(400, sharedByUserId: 1);

            var response = await _client.SendAsync(Authorized(HttpMethod.Post, "/api/v1/sharedworkouts/400/like", owner));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ToggleLike_FriendOnVisibleWorkout_Succeeds()
        {
            await SeedUser(1, "alice");
            var friend = await SeedUser(2, "bob");
            await SeedFriendship(1, 2, "accepted");
            await SeedSharedWorkout(401, sharedByUserId: 1);

            var response = await _client.SendAsync(Authorized(HttpMethod.Post, "/api/v1/sharedworkouts/401/like", friend));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var context = GetContext();
            var like = await context.SharedWorkoutLikes.FirstOrDefaultAsync(l => l.SharedWorkoutId == 401 && l.UserId == 2);
            Assert.NotNull(like);
            var workout = await context.SharedWorkouts.FindAsync(401);
            Assert.Equal(1, workout!.LikeCount); // persisted through the tracked entity SharedWorkoutsVisibleTo returns
        }

        [Fact]
        public async Task ToggleSave_OwnerOnVisibleWorkout_Succeeds()
        {
            var owner = await SeedUser(1, "alice");
            await SeedSharedWorkout(402, sharedByUserId: 1);

            var response = await _client.SendAsync(Authorized(HttpMethod.Post, "/api/v1/sharedworkouts/402/save", owner));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ToggleSave_FriendOnVisibleWorkout_Succeeds()
        {
            await SeedUser(1, "alice");
            var friend = await SeedUser(2, "bob");
            await SeedFriendship(1, 2, "accepted");
            await SeedSharedWorkout(403, sharedByUserId: 1);

            var response = await _client.SendAsync(Authorized(HttpMethod.Post, "/api/v1/sharedworkouts/403/save", friend));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var context = GetContext();
            var save = await context.SharedWorkoutSaves.FirstOrDefaultAsync(s => s.SharedWorkoutId == 403 && s.UserId == 2);
            Assert.NotNull(save);
            var workout = await context.SharedWorkouts.FindAsync(403);
            Assert.Equal(1, workout!.SaveCount); // persisted through the tracked entity SharedWorkoutsVisibleTo returns
        }

        [Fact]
        public async Task ToggleLike_HiddenGuessedId_RejectedIdenticallyToMissing()
        {
            await SeedUser(1, "alice");
            var stranger = await SeedUser(2, "bob");
            await SeedSharedWorkout(404, sharedByUserId: 1);

            var hiddenResponse = await _client.SendAsync(Authorized(HttpMethod.Post, "/api/v1/sharedworkouts/404/like", stranger));
            var missingResponse = await _client.SendAsync(Authorized(HttpMethod.Post, "/api/v1/sharedworkouts/999998/like", stranger));

            Assert.Equal(HttpStatusCode.NotFound, hiddenResponse.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);

            using var context = GetContext();
            var like = await context.SharedWorkoutLikes.FirstOrDefaultAsync(l => l.SharedWorkoutId == 404 && l.UserId == 2);
            Assert.Null(like);
            var untouched = await context.SharedWorkouts.FindAsync(404);
            Assert.Equal(0, untouched!.LikeCount);
        }

        [Fact]
        public async Task ToggleSave_HiddenGuessedId_RejectedIdenticallyToMissing()
        {
            await SeedUser(1, "alice");
            var stranger = await SeedUser(2, "bob");
            await SeedSharedWorkout(405, sharedByUserId: 1);

            var hiddenResponse = await _client.SendAsync(Authorized(HttpMethod.Post, "/api/v1/sharedworkouts/405/save", stranger));
            var missingResponse = await _client.SendAsync(Authorized(HttpMethod.Post, "/api/v1/sharedworkouts/999997/save", stranger));

            Assert.Equal(HttpStatusCode.NotFound, hiddenResponse.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);

            using var context = GetContext();
            var save = await context.SharedWorkoutSaves.FirstOrDefaultAsync(s => s.SharedWorkoutId == 405 && s.UserId == 2);
            Assert.Null(save);
            var untouched = await context.SharedWorkouts.FindAsync(405);
            Assert.Equal(0, untouched!.SaveCount);
        }

        // --- 15: delete remains creator-only ---

        [Fact]
        public async Task DeleteSharedWorkout_Owner_Succeeds()
        {
            var owner = await SeedUser(1, "alice");
            await SeedSharedWorkout(500, sharedByUserId: 1);

            var response = await _client.SendAsync(Authorized(HttpMethod.Delete, "/api/v1/sharedworkouts/500", owner));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteSharedWorkout_ConfirmedFriend_Returns403()
        {
            await SeedUser(1, "alice");
            var friend = await SeedUser(2, "bob");
            await SeedFriendship(1, 2, "accepted");
            await SeedSharedWorkout(501, sharedByUserId: 1);

            var response = await _client.SendAsync(Authorized(HttpMethod.Delete, "/api/v1/sharedworkouts/501", friend));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            using var context = GetContext();
            var stillThere = await context.SharedWorkouts.FindAsync(501);
            Assert.NotNull(stillThere);
        }

        [Fact]
        public async Task DeleteSharedWorkout_NonFriend_Returns403()
        {
            await SeedUser(1, "alice");
            var stranger = await SeedUser(2, "bob");
            await SeedSharedWorkout(502, sharedByUserId: 1);

            var response = await _client.SendAsync(Authorized(HttpMethod.Delete, "/api/v1/sharedworkouts/502", stranger));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // --- 16: JWT identity wins over client-supplied identity ---

        [Fact]
        public async Task ShareWorkout_ClientSuppliedSharedByUserId_IsIgnored()
        {
            var me = await SeedUser(1, "alice");
            await SeedUser(2, "bob");

            var request = Authorized(HttpMethod.Post, "/api/v1/sharedworkouts", me);
            request.Content = JsonContent.Create(new
            {
                originalId = 1,
                type = "session",
                sharedByUserId = 2, // spoofed - must be ignored in favor of the JWT's user id
                workoutName = "Spoofed Share",
                exercisesJson = "[]",
                duration = 20,
                category = "Cardio"
            });

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<SharedWorkout>();
            Assert.NotNull(created);
            Assert.Equal(1, created!.SharedByUserId); // caller's id, not the spoofed 2
        }

        // --- 17: like/save projection flags still computed for the requester ---

        [Fact]
        public async Task GetSharedWorkout_ComputesLikeAndSaveFlagsForRequester()
        {
            var owner = await SeedUser(1, "alice");
            await SeedSharedWorkout(600, sharedByUserId: 1);

            await _client.SendAsync(Authorized(HttpMethod.Post, "/api/v1/sharedworkouts/600/like", owner));
            await _client.SendAsync(Authorized(HttpMethod.Post, "/api/v1/sharedworkouts/600/save", owner));

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/600", owner));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("isLikedByCurrentUser").GetBoolean());
            Assert.True(body.GetProperty("isSavedByCurrentUser").GetBoolean());
        }

        [Fact]
        public async Task GetSharedWorkoutsByUser_ComputesLikeAndSaveFlagsForRequester()
        {
            await SeedUser(1, "alice");
            var friend = await SeedUser(2, "bob");
            await SeedFriendship(1, 2, "accepted");
            await SeedSharedWorkout(601, sharedByUserId: 1);

            await _client.SendAsync(Authorized(HttpMethod.Post, "/api/v1/sharedworkouts/601/like", friend));

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/user/1", friend));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var item = body.EnumerateArray().Single();
            Assert.True(item.GetProperty("isLikedByCurrentUser").GetBoolean());
            Assert.False(item.GetProperty("isSavedByCurrentUser").GetBoolean());
        }

        // --- 18: visibility scales to many friendships without behavioral drift ---
        // (EF Core translates SharedWorkoutsVisibleTo's nested `_context.Friendships.Any(...)`
        // into a single correlated EXISTS subquery per call - reviewed structurally in
        // SharedWorkoutsController.cs rather than via a runtime SQL command counter, since the
        // EF Core InMemory provider used by this test host has no relational command pipeline
        // to instrument.)
        [Fact]
        public async Task GetSharedWorkoutsByUser_ManyFriendshipsAndWorkouts_StillFiltersCorrectly()
        {
            var target = await SeedUser(1, "alice");
            for (int i = 2; i <= 40; i++)
            {
                await SeedUser(i, $"user{i}");
                await SeedFriendship(1, i, i % 2 == 0 ? "accepted" : "pending");
            }
            for (int i = 0; i < 25; i++)
            {
                await SeedSharedWorkout(700 + i, sharedByUserId: 1, $"Workout {i}");
            }

            var acceptedFriend = await GetUserFromDb(2);
            var pendingRequester = await GetUserFromDb(3);

            var friendResponse = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/user/1", acceptedFriend));
            var pendingResponse = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/user/1", pendingRequester));

            Assert.Equal(HttpStatusCode.OK, friendResponse.StatusCode);
            var friendBody = await friendResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(25, friendBody.GetArrayLength());

            Assert.Equal(HttpStatusCode.OK, pendingResponse.StatusCode);
            var pendingBody = await pendingResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(0, pendingBody.GetArrayLength());
        }

        private async Task<User> GetUserFromDb(int id)
        {
            using var context = GetContext();
            return (await context.Users.FindAsync(id))!;
        }

        // === Contract tests: DTO shape, sensitive-field exclusion, Flutter compatibility ===

        // Fields GoHardAPP's SharedWorkoutJson.fromJson (shared_workout_repository.dart) reads
        // without a null-coalescing fallback - i.e. required for a successful parse.
        private static readonly string[] FlutterRequiredFields =
        {
            "id", "originalId", "type", "sharedByUserId", "sharedByUserName",
            "workoutName", "exercisesJson", "duration", "category", "sharedAt"
        };

        // Full set of keys the response may legitimately contain: everything GoHardAPP's model
        // reads, plus "updatedAt" - present in the DTO/entity but not in the Dart model, and
        // harmlessly ignored by fromJson (unknown keys are dropped, not an error).
        private static readonly string[] FlutterKnownFields =
        {
            "id", "originalId", "type", "sharedByUserId", "sharedByUserName",
            "workoutName", "description", "exercisesJson", "duration", "category",
            "difficulty", "likeCount", "saveCount", "commentCount",
            "isLikedByCurrentUser", "isSavedByCurrentUser", "sharedAt", "updatedAt"
        };

        private static readonly string[] ForbiddenFieldNames =
        {
            "passwordHash", "password", "token", "securityStamp", "email",
            "sharedByUser" // the raw User navigation object itself must never appear
        };

        [Fact]
        public async Task ShareWorkout_ResponseContainsFieldsGoHardAPPNeeds()
        {
            var me = await SeedUser(1, "alice");

            var response = await PostShareWorkout(me);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            foreach (var field in FlutterRequiredFields)
            {
                Assert.True(body.TryGetProperty(field, out _), $"Response missing Flutter-required field '{field}'");
            }
        }

        [Fact]
        public async Task ShareWorkout_ResponseDoesNotContainSensitiveFields()
        {
            var me = await SeedUser(1, "alice");

            var response = await PostShareWorkout(me);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var rawBody = await response.Content.ReadAsStringAsync();
            var body = JsonDocument.Parse(rawBody).RootElement;

            foreach (var forbidden in ForbiddenFieldNames)
            {
                Assert.False(body.TryGetProperty(forbidden, out _), $"Response must not contain field '{forbidden}'");
            }

            // Belt-and-braces: the seeded bcrypt hash itself must never appear anywhere in the
            // payload, regardless of what key it might have been serialized under.
            Assert.DoesNotContain("super-secret-bcrypt-hash", rawBody);
            Assert.DoesNotContain("alice@example.com", rawBody); // the seeded user's Email
        }

        [Fact]
        public async Task GetEndpoints_DoNotContainSensitiveFields()
        {
            // Defense in depth alongside ShareWorkout_ResponseDoesNotContainSensitiveFields:
            // ProjectToDto is shared by all three read endpoints, but this guards each one
            // directly so a future change to just one of them (e.g. selecting SharedByUser
            // itself instead of only .Name) can't slip past coverage.
            var me = await SeedUser(1, "alice");
            var friend = await SeedUser(2, "bob");
            await SeedFriendship(1, 2, "accepted");
            await SeedSharedWorkout(900, sharedByUserId: 1);

            var listResponse = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts", friend));
            var byIdResponse = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/900", friend));
            var byUserResponse = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/user/1", friend));

            foreach (var response in new[] { listResponse, byIdResponse, byUserResponse })
            {
                var rawBody = await response.Content.ReadAsStringAsync();
                var body = JsonDocument.Parse(rawBody).RootElement;
                var items = body.ValueKind == JsonValueKind.Array ? body.EnumerateArray() : new[] { body }.AsEnumerable();

                foreach (var item in items)
                {
                    foreach (var forbidden in ForbiddenFieldNames)
                    {
                        Assert.False(item.TryGetProperty(forbidden, out _), $"Response must not contain field '{forbidden}'");
                    }
                }

                Assert.DoesNotContain("super-secret-bcrypt-hash", rawBody);
                Assert.DoesNotContain("alice@example.com", rawBody);
            }
        }

        [Fact]
        public async Task ShareWorkout_CreatorIdentity_RendersCorrectly()
        {
            var me = await SeedUser(1, "alice");

            var response = await PostShareWorkout(me);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(1, body.GetProperty("sharedByUserId").GetInt32());
            Assert.Equal("Name 1", body.GetProperty("sharedByUserName").GetString());
        }

        [Fact]
        public async Task AllReadAndCreateEndpoints_ReturnCompatibleDtoShape()
        {
            var me = await SeedUser(1, "alice");
            var friend = await SeedUser(2, "bob");
            await SeedFriendship(1, 2, "accepted");

            var createResponse = await PostShareWorkout(me);
            var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            var id = created.GetProperty("id").GetInt32();

            var byIdResponse = await _client.SendAsync(Authorized(HttpMethod.Get, $"/api/v1/sharedworkouts/{id}", friend));
            var byId = await byIdResponse.Content.ReadFromJsonAsync<JsonElement>();

            var byUserResponse = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/user/1", friend));
            var byUserList = await byUserResponse.Content.ReadFromJsonAsync<JsonElement>();
            var byUser = byUserList.EnumerateArray().Single(e => e.GetProperty("id").GetInt32() == id);

            var listResponse = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts", friend));
            var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
            var listItem = list.EnumerateArray().Single(e => e.GetProperty("id").GetInt32() == id);

            var createdKeys = created.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToList();
            var byIdKeys = byId.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToList();
            var byUserKeys = byUser.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToList();
            var listKeys = listItem.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToList();

            Assert.Equal(createdKeys, byIdKeys);
            Assert.Equal(createdKeys, byUserKeys);
            Assert.Equal(createdKeys, listKeys);
        }

        [Fact]
        public async Task ShareWorkout_ResponseIsFlutterCompatible_ExactJsonFixture()
        {
            // Proxy for running GoHardAPP's SharedWorkoutJson.fromJson (Dart) against the live
            // response: cross-repo execution isn't practical from this test project, so this
            // re-implements fromJson's exact field/type expectations against the real JSON the
            // API now returns, field for field.
            var me = await SeedUser(1, "alice");

            var response = await PostShareWorkout(me);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(body.GetProperty("id").GetInt32() > 0);
            Assert.True(body.GetProperty("originalId").TryGetInt32(out _));
            Assert.False(string.IsNullOrEmpty(body.GetProperty("type").GetString()));
            Assert.True(body.GetProperty("sharedByUserId").TryGetInt32(out _));
            Assert.False(string.IsNullOrEmpty(body.GetProperty("sharedByUserName").GetString()));
            Assert.False(string.IsNullOrEmpty(body.GetProperty("workoutName").GetString()));
            Assert.False(string.IsNullOrEmpty(body.GetProperty("exercisesJson").GetString()));
            Assert.True(body.GetProperty("duration").TryGetInt32(out _));
            Assert.False(string.IsNullOrEmpty(body.GetProperty("category").GetString()));
            // DateTime.parse(json['sharedAt'] as String) - must be a string, and parseable.
            var sharedAtString = body.GetProperty("sharedAt").GetString();
            Assert.False(string.IsNullOrEmpty(sharedAtString));
            Assert.True(DateTime.TryParse(sharedAtString, out _));

            foreach (var key in body.EnumerateObject().Select(p => p.Name))
            {
                Assert.Contains(key, FlutterKnownFields);
            }
        }

        private async Task<HttpResponseMessage> PostShareWorkout(User user)
        {
            var request = Authorized(HttpMethod.Post, "/api/v1/sharedworkouts", user);
            request.Content = JsonContent.Create(new
            {
                originalId = 1,
                type = "session",
                workoutName = "Leg Day",
                description = "Heavy squats",
                exercisesJson = "[{\"name\":\"Squat\",\"sets\":5,\"reps\":5}]",
                duration = 45,
                category = "Strength",
                difficulty = "Intermediate"
            });
            return await _client.SendAsync(request);
        }

        // === GetSavedWorkouts visibility (must reapply owner-or-accepted-friend, live) ===

        [Fact]
        public async Task GetSavedWorkouts_AcceptedFriend_ReturnsSavedWorkout()
        {
            await SeedUser(1, "alice");
            var friend = await SeedUser(2, "bob");
            await SeedFriendship(1, 2, "accepted");
            await SeedSharedWorkout(1000, sharedByUserId: 1);
            await SeedSharedWorkoutSave(1000, userId: 2);

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/saved", friend));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var item = Assert.Single(body.EnumerateArray());
            Assert.Equal(1000, item.GetProperty("id").GetInt32());
            Assert.True(item.GetProperty("isSavedByCurrentUser").GetBoolean()); // requirement 11
        }

        [Fact]
        public async Task GetSavedWorkouts_Owner_ReturnsOwnSavedWorkout()
        {
            // SharedWorkoutsVisibleTo defaults includeOwn:true, and ToggleSave already allows an
            // owner to save their own workout (no code path forbids it) - GetSavedWorkouts must
            // follow that same established behavior rather than inventing a new restriction.
            var owner = await SeedUser(1, "alice");
            await SeedSharedWorkout(1001, sharedByUserId: 1);

            var saveResponse = await _client.SendAsync(Authorized(HttpMethod.Post, "/api/v1/sharedworkouts/1001/save", owner));
            Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/saved", owner));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var item = Assert.Single(body.EnumerateArray());
            Assert.Equal(1001, item.GetProperty("id").GetInt32());
        }

        [Fact]
        public async Task GetSavedWorkouts_FriendshipBecomesPending_WorkoutDisappears()
        {
            await SeedUser(1, "alice");
            var friend = await SeedUser(2, "bob");
            await SeedFriendship(1, 2, "accepted");
            await SeedSharedWorkout(1002, sharedByUserId: 1);
            await SeedSharedWorkoutSave(1002, userId: 2);

            var beforeResponse = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/saved", friend));
            var beforeBody = await beforeResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, beforeBody.GetArrayLength());

            await SetFriendshipStatus(1, 2, "pending");

            var afterResponse = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/saved", friend));
            var afterBody = await afterResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(0, afterBody.GetArrayLength());

            // The saved association itself must survive - only the response excludes it.
            using var context = GetContext();
            Assert.NotNull(await context.SharedWorkoutSaves.FirstOrDefaultAsync(s => s.SharedWorkoutId == 1002 && s.UserId == 2));
        }

        [Fact]
        public async Task GetSavedWorkouts_FriendshipDeclined_WorkoutDisappears()
        {
            await SeedUser(1, "alice");
            var friend = await SeedUser(2, "bob");
            await SeedFriendship(1, 2, "accepted");
            await SeedSharedWorkout(1003, sharedByUserId: 1);
            await SeedSharedWorkoutSave(1003, userId: 2);

            await SetFriendshipStatus(1, 2, "declined");

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/saved", friend));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(0, body.GetArrayLength());
        }

        [Fact]
        public async Task GetSavedWorkouts_FriendshipRemoved_WorkoutDisappears()
        {
            await SeedUser(1, "alice");
            var friend = await SeedUser(2, "bob");
            await SeedFriendship(1, 2, "accepted");
            await SeedSharedWorkout(1004, sharedByUserId: 1);
            await SeedSharedWorkoutSave(1004, userId: 2);

            await RemoveFriendship(1, 2);

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/saved", friend));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(0, body.GetArrayLength());

            using var context = GetContext();
            Assert.NotNull(await context.SharedWorkoutSaves.FirstOrDefaultAsync(s => s.SharedWorkoutId == 1004 && s.UserId == 2));
        }

        [Fact]
        public async Task GetSavedWorkouts_UnrelatedUserWithStaleSave_CannotRetrieve()
        {
            await SeedUser(1, "alice");
            var stranger = await SeedUser(2, "bob"); // never friended
            await SeedSharedWorkout(1005, sharedByUserId: 1);
            // Only reachable by seeding directly - ToggleSave itself would reject this today.
            await SeedSharedWorkoutSave(1005, userId: 2);

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/saved", stranger));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(0, body.GetArrayLength());
        }

        [Fact]
        public async Task GetSavedWorkouts_FriendshipRestored_WorkoutReappears()
        {
            await SeedUser(1, "alice");
            var friend = await SeedUser(2, "bob");
            await SeedFriendship(1, 2, "accepted");
            await SeedSharedWorkout(1006, sharedByUserId: 1);
            await SeedSharedWorkoutSave(1006, userId: 2);

            await SetFriendshipStatus(1, 2, "declined");
            var hiddenResponse = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/saved", friend));
            Assert.Equal(0, (await hiddenResponse.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength());

            await SetFriendshipStatus(1, 2, "accepted");
            var restoredResponse = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/saved", friend));
            var restoredBody = await restoredResponse.Content.ReadFromJsonAsync<JsonElement>();
            var item = Assert.Single(restoredBody.EnumerateArray());
            Assert.Equal(1006, item.GetProperty("id").GetInt32());
        }

        [Fact]
        public async Task GetSavedWorkouts_HiddenSaveAndNoSaves_ProduceIndistinguishableEmptyResults()
        {
            await SeedUser(1, "alice");
            var stranger = await SeedUser(2, "bob");
            await SeedUser(3, "carol"); // has genuinely never saved anything
            await SeedSharedWorkout(1007, sharedByUserId: 1);
            await SeedSharedWorkoutSave(1007, userId: 2); // stale/hidden - stranger, no friendship

            var hiddenResponse = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/saved", stranger));
            var noSavesResponse = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/saved", await GetUserFromDb(3)));

            Assert.Equal(HttpStatusCode.OK, hiddenResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, noSavesResponse.StatusCode);

            var hiddenBody = await hiddenResponse.Content.ReadAsStringAsync();
            var noSavesBody = await noSavesResponse.Content.ReadAsStringAsync();
            Assert.Equal("[]", hiddenBody);
            Assert.Equal("[]", noSavesBody);
        }

        [Fact]
        public async Task GetSavedWorkouts_ReturnsSameDtoShapeAsOtherEndpoints()
        {
            var me = await SeedUser(1, "alice");
            var friend = await SeedUser(2, "bob");
            await SeedFriendship(1, 2, "accepted");

            var createResponse = await PostShareWorkout(me);
            var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            var id = created.GetProperty("id").GetInt32();

            await _client.SendAsync(Authorized(HttpMethod.Post, $"/api/v1/sharedworkouts/{id}/save", friend));

            var savedResponse = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/saved", friend));
            var savedList = await savedResponse.Content.ReadFromJsonAsync<JsonElement>();
            var saved = savedList.EnumerateArray().Single(e => e.GetProperty("id").GetInt32() == id);

            var byIdResponse = await _client.SendAsync(Authorized(HttpMethod.Get, $"/api/v1/sharedworkouts/{id}", friend));
            var byId = await byIdResponse.Content.ReadFromJsonAsync<JsonElement>();

            var byUserResponse = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/user/1", friend));
            var byUserList = await byUserResponse.Content.ReadFromJsonAsync<JsonElement>();
            var byUser = byUserList.EnumerateArray().Single(e => e.GetProperty("id").GetInt32() == id);

            var listResponse = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts", friend));
            var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
            var listItem = list.EnumerateArray().Single(e => e.GetProperty("id").GetInt32() == id);

            var createdKeys = created.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToList();
            var savedKeys = saved.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToList();
            var byIdKeys = byId.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToList();
            var byUserKeys = byUser.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToList();
            var listKeys = listItem.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToList();

            Assert.Equal(createdKeys, savedKeys);
            Assert.Equal(createdKeys, byIdKeys);
            Assert.Equal(createdKeys, byUserKeys);
            Assert.Equal(createdKeys, listKeys);
        }

        [Fact]
        public async Task GetSavedWorkouts_DoesNotContainSensitiveFields()
        {
            await SeedUser(1, "alice");
            var friend = await SeedUser(2, "bob");
            await SeedFriendship(1, 2, "accepted");
            await SeedSharedWorkout(1008, sharedByUserId: 1);
            await SeedSharedWorkoutSave(1008, userId: 2);

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/saved", friend));
            var rawBody = await response.Content.ReadAsStringAsync();
            var body = JsonDocument.Parse(rawBody).RootElement;

            foreach (var item in body.EnumerateArray())
            {
                foreach (var forbidden in ForbiddenFieldNames)
                {
                    Assert.False(item.TryGetProperty(forbidden, out _), $"Response must not contain field '{forbidden}'");
                }
            }

            Assert.DoesNotContain("super-secret-bcrypt-hash", rawBody);
            Assert.DoesNotContain("alice@example.com", rawBody);
        }

        [Fact]
        public async Task GetSavedWorkouts_ComputesIsLikedByCurrentUserAccurately()
        {
            await SeedUser(1, "alice");
            var friend = await SeedUser(2, "bob");
            await SeedFriendship(1, 2, "accepted");
            await SeedSharedWorkout(1009, sharedByUserId: 1);
            await SeedSharedWorkoutSave(1009, userId: 2);

            // Not liked yet.
            var beforeResponse = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/saved", friend));
            var beforeItem = (await beforeResponse.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().Single();
            Assert.False(beforeItem.GetProperty("isLikedByCurrentUser").GetBoolean());

            await _client.SendAsync(Authorized(HttpMethod.Post, "/api/v1/sharedworkouts/1009/like", friend));

            var afterResponse = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/saved", friend));
            var afterItem = (await afterResponse.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().Single();
            Assert.True(afterItem.GetProperty("isLikedByCurrentUser").GetBoolean());
            Assert.True(afterItem.GetProperty("isSavedByCurrentUser").GetBoolean());
        }

        [Fact]
        public async Task GetSavedWorkouts_OrdersByWhenSaved_NotByWhenShared()
        {
            // Guards against a future accidental swap of SharedWorkoutSave.SavedAt for
            // SharedWorkout.SharedAt in the OrderByDescending clause: sharing order and saving
            // order are deliberately reversed here so only save-order sorting passes.
            var me = await SeedUser(1, "alice");
            var older = await SeedSharedWorkout(1010, sharedByUserId: 1, "Older Share");
            var newer = await SeedSharedWorkout(1011, sharedByUserId: 1, "Newer Share");

            await SeedSharedWorkoutSave(older.Id, userId: 1, savedAt: DateTime.UtcNow); // saved second (most recent)
            await SeedSharedWorkoutSave(newer.Id, userId: 1, savedAt: DateTime.UtcNow.AddMinutes(-10)); // saved first

            var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/sharedworkouts/saved", me));
            var ids = (await response.Content.ReadFromJsonAsync<JsonElement>())
                .EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).ToList();

            Assert.Equal(new[] { older.Id, newer.Id }, ids); // most-recently-saved first, not most-recently-shared
        }
    }
}
