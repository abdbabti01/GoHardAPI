using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using GoHardAPI.Tests.Infrastructure;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// HTTP-level coverage of <c>DELETE /api/v1/sessions/by-operation/{clientOperationId}</c>
    /// through the real <c>Program.cs</c> pipeline (via
    /// <see cref="SessionWriteRateLimitFactory"/>, environment <c>"Testing"</c>, isolated
    /// in-memory DB): attribute routing (it does not collide with
    /// <c>DELETE /api/v1/sessions/{id}</c>), the malformed / empty key contract, and the
    /// shared <c>session-write</c> rate-limit policy.
    /// </summary>
    public class SessionCreateCancellationHttpTests
    {
        private const string CreateUrl = "/api/v1/sessions";

        private static string CancelUrl(Guid key) => $"/api/v1/sessions/by-operation/{key}";

        private static HttpContent KeyedBody(Guid key) =>
            JsonContent.Create(new { date = "2026-09-03", name = "S", status = "draft", clientOperationId = key });

        private static SessionWriteRateLimitFactory Factory(int tokenLimit = 20)
        {
            var f = new SessionWriteRateLimitFactory();
            f.ConfigOverrides["RateLimiting:SessionWrite:TokenLimit"] = tokenLimit.ToString();
            f.ConfigOverrides["RateLimiting:SessionWrite:TokensPerPeriod"] = "1";
            f.ConfigOverrides["RateLimiting:SessionWrite:ReplenishmentPeriodSeconds"] = "3600";
            f.ConfigOverrides["RateLimiting:SessionWrite:AutoReplenishment"] = "false";
            return f;
        }

        [Fact]
        public async Task CancelByOperation_FreshKey_Returns204_AndRoutesToTheCancelAction_NotDeleteById()
        {
            using var factory = Factory();
            var client = factory.CreateClientForUser(1);

            var resp = await client.DeleteAsync(CancelUrl(Guid.NewGuid()));

            // 204 (not 400/404) proves the {guid} bound to CancelSessionCreateByOperation
            // and not to DeleteSession(int id).
            Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
            Assert.Equal(1, factory.OperationCount(1)); // tombstone written
        }

        [Fact]
        public async Task OrdinaryDeleteById_StillResolves_AndIsUnaffected()
        {
            using var factory = Factory();
            var client = factory.CreateClientForUser(1);

            var resp = await client.DeleteAsync("/api/v1/sessions/999999");

            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
            Assert.NotEqual((HttpStatusCode)429, resp.StatusCode);
        }

        [Fact]
        public async Task CancelByOperation_EmptyGuid_Returns400_WithInvalidOperationKeyCode()
        {
            using var factory = Factory();
            var client = factory.CreateClientForUser(1);

            var resp = await client.DeleteAsync(CancelUrl(Guid.Empty));

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            Assert.Contains("invalid_operation_key", await resp.Content.ReadAsStringAsync());
            Assert.Equal(0, factory.OperationCount(1)); // no write
        }

        [Fact]
        public async Task CancelByOperation_MalformedGuid_IsAModelBinding400()
        {
            using var factory = Factory();
            var client = factory.CreateClientForUser(1);

            var resp = await client.DeleteAsync("/api/v1/sessions/by-operation/not-a-guid");

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            Assert.Equal(0, factory.OperationCount(1));
        }

        [Fact]
        public async Task CancelByOperation_MissingToken_Returns401_Not204()
        {
            using var factory = Factory();
            var anon = factory.CreateAnonymousClient();

            var resp = await anon.DeleteAsync(CancelUrl(Guid.NewGuid()));

            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact]
        public async Task CancelByOperation_CarriesTheSessionWritePolicy_429AfterBucketDrained()
        {
            using var factory = Factory(tokenLimit: 1);
            var client = factory.CreateClientForUser(1);

            // Drain the shared per-user session-write bucket with the keyed create.
            Assert.Equal(HttpStatusCode.Created,
                (await client.PostAsync(CreateUrl, KeyedBody(Guid.NewGuid()))).StatusCode);

            var rejected = await client.DeleteAsync(CancelUrl(Guid.NewGuid()));

            Assert.Equal((HttpStatusCode)429, rejected.StatusCode);
            Assert.Equal("{\"code\":\"rate_limited\"}", await rejected.Content.ReadAsStringAsync());
            Assert.True(rejected.Headers.TryGetValues("Retry-After", out _));
        }

        [Fact]
        public async Task CancelByOperation_RateLimitedRequest_WritesNoTombstone()
        {
            using var factory = Factory(tokenLimit: 1);
            var client = factory.CreateClientForUser(1);

            Assert.Equal(HttpStatusCode.Created,
                (await client.PostAsync(CreateUrl, KeyedBody(Guid.NewGuid()))).StatusCode);
            var opsBefore = factory.OperationCount(1);

            Assert.Equal((HttpStatusCode)429, (await client.DeleteAsync(CancelUrl(Guid.NewGuid()))).StatusCode);

            Assert.Equal(opsBefore, factory.OperationCount(1)); // rejected before the action ran
        }

        [Fact]
        public async Task CancelByOperation_ThenKeyedCreateWithSameKey_Returns409_OperationCanceled()
        {
            using var factory = Factory();
            var client = factory.CreateClientForUser(1);
            var key = Guid.NewGuid();

            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(CancelUrl(key))).StatusCode);

            var create = await client.PostAsync(CreateUrl, KeyedBody(key));

            Assert.Equal(HttpStatusCode.Conflict, create.StatusCode);
            Assert.Contains("operation_canceled", await create.Content.ReadAsStringAsync());
            Assert.Equal(0, factory.SessionCount(1));
        }
    }
}
