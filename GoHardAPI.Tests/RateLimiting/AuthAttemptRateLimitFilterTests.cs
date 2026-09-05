using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GoHardAPI.DTOs;
using GoHardAPI.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace GoHardAPI.Tests.RateLimiting
{
    /// <summary>
    /// The auth-attempt action filter, exercised directly (post-model-binding seam):
    /// per-identity quotas, exact-once invocation, rejection, cancellation, and the
    /// bare 429 shape. Tiny permit counts + a 1-hour window — no sleeps.
    /// </summary>
    public class AuthAttemptRateLimitFilterTests
    {
        private static AuthAttemptRateLimiter Limiter(int permit = 2, int queue = 0, int windowSeconds = 3600)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Auth:PermitLimit"] = permit.ToString(),
                ["RateLimiting:Auth:WindowSeconds"] = windowSeconds.ToString(),
                ["RateLimiting:Auth:QueueLimit"] = queue.ToString(),
            }).Build();

            return new ServiceCollection().AddGoHardRateLimiting(config)
                .BuildServiceProvider().GetRequiredService<AuthAttemptRateLimiter>();
        }

        private sealed class Attempt
        {
            public int ActionRuns;
            public ActionExecutingContext Context = null!;
            public ActionExecutionDelegate Next = null!;
        }

        private static Attempt NewAttempt(object? dto, CancellationToken abort = default)
        {
            var http = new DefaultHttpContext();
            http.Response.Body = new MemoryStream();
            http.RequestAborted = abort;

            var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
            var args = new Dictionary<string, object?>();
            if (dto is not null)
            {
                args["request"] = dto;
            }

            var a = new Attempt();
            a.Context = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), args, controller: new object());
            a.Next = () =>
            {
                a.ActionRuns++;
                return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), controller: new object()));
            };
            return a;
        }

        private static async Task<int> AdmittedOutOf(
            AuthAttemptRateLimitFilter filter, Func<object?> dto, int attempts)
        {
            var admitted = 0;
            for (var i = 0; i < attempts; i++)
            {
                var a = NewAttempt(dto());
                await filter.OnActionExecutionAsync(a.Context, a.Next);
                if (a.ActionRuns == 1 && a.Context.Result is null)
                {
                    admitted++;
                }
            }
            return admitted;
        }

        private static AuthAttemptRateLimitFilter LoginFilter(AuthAttemptRateLimiter l) => new(AuthAttemptIdentity.LoginScope, l);
        private static AuthAttemptRateLimitFilter SignupFilter(AuthAttemptRateLimiter l) => new(AuthAttemptIdentity.SignupScope, l);

        private static LoginRequest LoginDto(string email, string password = "pw") => new(email, password);
        private static SignupRequest SignupDto(string email, string name = "N", string username = "u", string password = "Abcd1234") =>
            new(name, username, email, password);

        // ---- per-identity quotas -------------------------------------------------

        [Fact]
        public async Task TwoLoginIdentities_HaveIndependentQuotas()
        {
            using var l = Limiter(permit: 2);
            var filter = LoginFilter(l);

            Assert.Equal(2, await AdmittedOutOf(filter, () => LoginDto("a@example.com"), 5));
            Assert.Equal(2, await AdmittedOutOf(filter, () => LoginDto("b@example.com"), 5));
        }

        [Fact]
        public async Task ThrottlingIdentityA_DoesNotBlockIdentityB()
        {
            using var l = Limiter(permit: 2);
            var filter = LoginFilter(l);

            await AdmittedOutOf(filter, () => LoginDto("a@example.com"), 10); // drain + reject A
            Assert.Equal(2, await AdmittedOutOf(filter, () => LoginDto("b@example.com"), 2));
        }

        [Fact] // exact-repeated email text shares one partition
        public async Task ExactRepeatedEmailText_SharesOneQuota()
        {
            using var l = Limiter(permit: 2);
            var filter = LoginFilter(l);

            Assert.Equal(2, await AdmittedOutOf(filter, () => LoginDto("user@example.com"), 5));
        }

        [Fact] // case-distinct identities are different partitions; one cannot exhaust the other's quota
        public async Task CaseDifferentIdentities_DoNotShareAQuota()
        {
            using var l = Limiter(permit: 2);
            var filter = LoginFilter(l);

            // drain + reject "User@Example.com"
            await AdmittedOutOf(filter, () => LoginDto("User@Example.com"), 10);

            // the lowercase identity still has its full quota
            Assert.Equal(2, await AdmittedOutOf(filter, () => LoginDto("user@example.com"), 5));
        }

        [Fact] // surrounding whitespace makes a different identity (varchar "=" is whitespace-significant)
        public async Task WhitespaceDifferentIdentities_DoNotShareAQuota()
        {
            using var l = Limiter(permit: 1);
            var filter = LoginFilter(l);

            Assert.Equal(1, await AdmittedOutOf(filter, () => LoginDto("user@example.com"), 3));
            Assert.Equal(1, await AdmittedOutOf(filter, () => LoginDto(" user@example.com"), 3));
        }

        [Fact]
        public async Task LoginAndSignup_ForOneIdentity_UseSeparatePartitions()
        {
            using var l = Limiter(permit: 1);

            Assert.Equal(1, await AdmittedOutOf(LoginFilter(l), () => LoginDto("same@example.com"), 3));
            // signup for the same identity still has its full quota
            Assert.Equal(1, await AdmittedOutOf(SignupFilter(l), () => SignupDto("same@example.com"), 3));
        }

        [Fact]
        public async Task Password_DoesNotInfluenceThePartition()
        {
            using var l = Limiter(permit: 2);
            var filter = LoginFilter(l);
            var passwords = new[] { "pw1", "pw2", "totally-different" };

            var admitted = 0;
            foreach (var p in passwords)
            {
                var a = NewAttempt(LoginDto("victim@example.com", p));
                await filter.OnActionExecutionAsync(a.Context, a.Next);
                if (a.ActionRuns == 1) admitted++;
            }
            Assert.Equal(2, admitted); // same email, 3 passwords, one bucket
        }

        [Fact]
        public async Task ChangingUnrelatedSignupFields_CannotMintPartitions()
        {
            using var l = Limiter(permit: 2);
            var filter = SignupFilter(l);
            // same email, rotating name/username/password
            var dtos = new Func<object?>[]
            {
                () => SignupDto("target@example.com", name: "A", username: "aaa", password: "Abcd1234"),
                () => SignupDto("target@example.com", name: "B", username: "bbb", password: "Wxyz9876"),
                () => SignupDto("target@example.com", name: "C", username: "ccc", password: "Mnop5432"),
            };

            var admitted = 0;
            foreach (var d in dtos)
            {
                var a = NewAttempt(d());
                await filter.OnActionExecutionAsync(a.Context, a.Next);
                if (a.ActionRuns == 1) admitted++;
            }
            Assert.Equal(2, admitted);
        }

        [Fact] // no bound DTO (or a null/empty email) -> the one bounded per-scope fallback
        public async Task MissingDtoOrEmptyEmail_UsesTheOneBoundedFallback()
        {
            using var l = Limiter(permit: 2);
            var filter = LoginFilter(l);

            Assert.Equal(2, await AdmittedOutOf(filter, () => null, 5));                 // no DTO
            Assert.Equal(0, await AdmittedOutOf(filter, () => LoginDto(string.Empty), 2)); // "" -> same fallback, drained

            // signup's fallback is a separate partition
            var signup = SignupFilter(l);
            Assert.Equal(2, await AdmittedOutOf(signup, () => null, 5));
        }

        // ---- exact-once / rejection / cancellation ------------------------------

        [Fact]
        public async Task Admitted_InvokesTheActionExactlyOnce()
        {
            using var l = Limiter(permit: 5);
            var a = NewAttempt(LoginDto("once@example.com"));

            await LoginFilter(l).OnActionExecutionAsync(a.Context, a.Next);

            Assert.Equal(1, a.ActionRuns);
            Assert.Null(a.Context.Result);
        }

        [Fact]
        public async Task Rejected_NeverInvokesTheAction_AndReturnsABare429()
        {
            using var l = Limiter(permit: 1);
            var filter = LoginFilter(l);

            var first = NewAttempt(LoginDto("full@example.com"));
            await filter.OnActionExecutionAsync(first.Context, first.Next);
            Assert.Equal(1, first.ActionRuns);

            var rejected = NewAttempt(LoginDto("full@example.com"));
            await filter.OnActionExecutionAsync(rejected.Context, rejected.Next);

            Assert.Equal(0, rejected.ActionRuns);
            Assert.IsType<EmptyResult>(rejected.Context.Result);
            Assert.Equal(429, rejected.Context.HttpContext.Response.StatusCode);
            Assert.Equal(0, rejected.Context.HttpContext.Response.Body.Length); // no body
        }

        [Fact] // guard: every controller action that binds a password-bearing DTO must carry [AuthAttemptRateLimit]
        public void EveryCredentialAcceptingAction_CarriesTheAuthAttemptRateLimitAttribute()
        {
            var actions = typeof(GoHardAPI.Controllers.AuthController).Assembly.GetTypes()
                .Where(t => typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
                .SelectMany(t => t.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
                .Where(m => m.GetParameters().Any(p =>
                    p.ParameterType.GetProperty("Password", typeof(string)) is not null))
                .ToList();

            Assert.NotEmpty(actions); // Login + Signup at least — guards against the query silently matching nothing
            foreach (var action in actions)
            {
                Assert.True(
                    action.GetCustomAttributes(typeof(AuthAttemptRateLimitAttribute), inherit: true).Length == 1,
                    $"{action.DeclaringType!.Name}.{action.Name} binds a password but has no [AuthAttemptRateLimit].");
            }
        }

        [Fact] // the preserved contract: 5 requests / 60 s / queue 2 per identity
        public void DefaultLimit_Is5Per60SecondsQueue2()
        {
            using var sp = new ServiceCollection()
                .AddGoHardRateLimiting(new ConfigurationBuilder().Build())
                .BuildServiceProvider();

            var o = sp.GetRequiredService<IOptionsMonitor<FixedWindowRateLimitOptions>>()
                .Get(AuthAttemptRateLimiter.OptionsName);

            Assert.Equal(5, o.PermitLimit);
            Assert.Equal(60, o.WindowSeconds);
            Assert.Equal(2, o.QueueLimit);
        }

        [Fact]
        public async Task CancelledRequest_PropagatesWithoutInvokingTheAction()
        {
            using var l = Limiter(permit: 1, queue: 1);
            var filter = LoginFilter(l);

            // consume the only permit so a further attempt would have to queue/wait
            var first = NewAttempt(LoginDto("cancel@example.com"));
            await filter.OnActionExecutionAsync(first.Context, first.Next);
            Assert.Equal(1, first.ActionRuns);

            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var aborted = NewAttempt(LoginDto("cancel@example.com"), cts.Token);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => filter.OnActionExecutionAsync(aborted.Context, aborted.Next));
            Assert.Equal(0, aborted.ActionRuns);
        }
    }
}
