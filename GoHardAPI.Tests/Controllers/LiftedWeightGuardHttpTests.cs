using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using GoHardAPI.Data;
using GoHardAPI.Models;
using GoHardAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// HTTP-level coverage of the lifted-weight kg contract guard
    /// (<c>GoHardAPI.Filters.RequireCanonicalLiftedWeightClientAttribute</c>) on
    /// <c>POST</c>/<c>PUT api/v1/exercisesets</c>, and of
    /// <c>GET api/v1/liftedweightcontract</c>. Boots the real <c>Program.cs</c>
    /// pipeline through <see cref="SessionWriteRateLimitFactory"/> (environment
    /// "Testing", isolated in-memory DB) so config-flag wiring, the action
    /// filter, and attribute routing are all exercised end to end.
    /// </summary>
    public class LiftedWeightGuardHttpTests
    {
        private const string HeaderName = "X-Lifted-Weight-Unit";

        private static SessionWriteRateLimitFactory Factory(
            bool? requireCanonicalClient = null, bool? canonicalHistory = null)
        {
            var f = new SessionWriteRateLimitFactory();
            if (requireCanonicalClient.HasValue)
            {
                f.ConfigOverrides["LiftedWeight:RequireCanonicalClient"] =
                    requireCanonicalClient.Value.ToString();
            }
            if (canonicalHistory.HasValue)
            {
                f.ConfigOverrides["LiftedWeight:CanonicalHistory"] = canonicalHistory.Value.ToString();
            }
            return f;
        }

        /// <summary>Seeds a session -> exercise chain for <paramref name="userId"/> and
        /// returns the exercise id new sets can attach to.</summary>
        private static int SeedExercise(SessionWriteRateLimitFactory factory, int userId)
        {
            factory.SeedUser(userId);
            using var scope = factory.Services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<TrainingContext>();
            var session = new Session { UserId = userId, Name = "S", Date = DateTime.UtcNow };
            ctx.Sessions.Add(session);
            ctx.SaveChanges();
            var exercise = new Exercise { SessionId = session.Id, Name = "Bench" };
            ctx.Exercises.Add(exercise);
            ctx.SaveChanges();
            return exercise.Id;
        }

        private static int SeedSet(SessionWriteRateLimitFactory factory, int userId, int exerciseId, double weight)
        {
            using var scope = factory.Services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<TrainingContext>();
            var set = new ExerciseSet { ExerciseId = exerciseId, SetNumber = 1, Weight = weight };
            ctx.ExerciseSets.Add(set);
            ctx.SaveChanges();
            return set.Id;
        }

        private static double? StoredWeight(SessionWriteRateLimitFactory factory, int setId)
        {
            using var scope = factory.Services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<TrainingContext>();
            return ctx.ExerciseSets.AsNoTracking().First(s => s.Id == setId).Weight;
        }

        private static HttpContent SetBody(int exerciseId, double weight) =>
            JsonContent.Create(new { exerciseId, setNumber = 1, weight });

        // ---- guard off ----------------------------------------------------------------

        [Fact]
        public async Task GuardOff_PostWithoutHeader_Returns201_UnchangedBehaviour()
        {
            using var factory = Factory(requireCanonicalClient: false, canonicalHistory: false);
            var client = factory.CreateClientForUser(1);
            var exerciseId = SeedExercise(factory, 1);

            var resp = await client.PostAsync("/api/v1/exercisesets", SetBody(exerciseId, 100));

            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        }

        // ---- RequireCanonicalClient ----------------------------------------------------

        [Fact]
        public async Task RequireCanonicalClient_PostWithoutHeader_Returns400_WithUnitRequiredCode()
        {
            using var factory = Factory(requireCanonicalClient: true);
            var client = factory.CreateClientForUser(1);
            var exerciseId = SeedExercise(factory, 1);

            var resp = await client.PostAsync("/api/v1/exercisesets", SetBody(exerciseId, 100));

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            Assert.Equal("LIFTED_WEIGHT_UNIT_REQUIRED", doc.RootElement.GetProperty("code").GetString());
        }

        [Fact]
        public async Task RequireCanonicalClient_PostWithLbHeader_Returns400()
        {
            using var factory = Factory(requireCanonicalClient: true);
            var client = factory.CreateClientForUser(1);
            var exerciseId = SeedExercise(factory, 1);
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/exercisesets")
            {
                Content = SetBody(exerciseId, 100),
            };
            req.Headers.Add(HeaderName, "lb");

            var resp = await client.SendAsync(req);

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task RequireCanonicalClient_PostWithKgHeader_Returns201_AndStoresWeightExactly()
        {
            using var factory = Factory(requireCanonicalClient: true);
            var client = factory.CreateClientForUser(1);
            var exerciseId = SeedExercise(factory, 1);
            const double weight = 61.23496995;
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/exercisesets")
            {
                Content = SetBody(exerciseId, weight),
            };
            req.Headers.Add(HeaderName, "kg");

            var resp = await client.SendAsync(req);

            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
            var created = await resp.Content.ReadFromJsonAsync<ExerciseSet>();
            Assert.Equal(weight, StoredWeight(factory, created!.Id));
        }

        [Fact]
        public async Task CanonicalHistoryAlone_EnablesGuard_PostWithoutHeader_Returns400()
        {
            using var factory = Factory(canonicalHistory: true);
            var client = factory.CreateClientForUser(1);
            var exerciseId = SeedExercise(factory, 1);

            var resp = await client.PostAsync("/api/v1/exercisesets", SetBody(exerciseId, 100));

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        // ---- PUT --------------------------------------------------------------------

        [Fact]
        public async Task RequireCanonicalClient_PutWithoutHeader_Returns400()
        {
            using var factory = Factory(requireCanonicalClient: true);
            var client = factory.CreateClientForUser(1);
            var exerciseId = SeedExercise(factory, 1);
            var setId = SeedSet(factory, 1, exerciseId, 50);

            var resp = await client.PutAsync($"/api/v1/exercisesets/{setId}", JsonContent.Create(new
            {
                id = setId,
                exerciseId,
                setNumber = 1,
                weight = 55,
                isCompleted = false,
            }));

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task RequireCanonicalClient_PutWithKgHeader_Returns204_AndStoresWeightExactly()
        {
            using var factory = Factory(requireCanonicalClient: true);
            var client = factory.CreateClientForUser(1);
            var exerciseId = SeedExercise(factory, 1);
            var setId = SeedSet(factory, 1, exerciseId, 50);
            const double weight = 61.23496995;
            var req = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/exercisesets/{setId}")
            {
                Content = JsonContent.Create(new
                {
                    id = setId,
                    exerciseId,
                    setNumber = 1,
                    weight,
                    isCompleted = false,
                }),
            };
            req.Headers.Add(HeaderName, "kg");

            var resp = await client.SendAsync(req);

            Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
            Assert.Equal(weight, StoredWeight(factory, setId));
        }

        // ---- PATCH complete: not guarded ----------------------------------------------

        [Fact]
        public async Task RequireCanonicalClient_PatchComplete_WithoutHeader_StillSucceeds()
        {
            using var factory = Factory(requireCanonicalClient: true);
            var client = factory.CreateClientForUser(1);
            var exerciseId = SeedExercise(factory, 1);
            var setId = SeedSet(factory, 1, exerciseId, 50);

            var resp = await client.PatchAsync(
                $"/api/v1/exercisesets/{setId}/complete", new StringContent(string.Empty));

            Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        }

        // ---- contract endpoint ---------------------------------------------------------

        [Fact]
        public async Task ContractEndpoint_DefaultsToFalse()
        {
            using var factory = Factory();
            var client = factory.CreateClientForUser(1);

            var resp = await client.GetAsync("/api/v1/liftedweightcontract");

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            Assert.False(doc.RootElement.GetProperty("canonicalHistory").GetBoolean());
        }

        [Fact]
        public async Task ContractEndpoint_ReflectsConfiguredTrue()
        {
            using var factory = Factory(canonicalHistory: true);
            var client = factory.CreateClientForUser(1);

            var resp = await client.GetAsync("/api/v1/liftedweightcontract");

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            Assert.True(doc.RootElement.GetProperty("canonicalHistory").GetBoolean());
        }

        [Fact]
        public async Task ContractEndpoint_Unauthenticated_Returns401()
        {
            using var factory = Factory();
            var client = factory.CreateAnonymousClient();

            var resp = await client.GetAsync("/api/v1/liftedweightcontract");

            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }
    }
}
