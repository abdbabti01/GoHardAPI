using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using GoHardAPI.Controllers;
using GoHardAPI.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace GoHardAPI.Tests.RateLimiting
{
    /// <summary>
    /// Deterministic unit coverage for the <c>session-write</c> token-bucket limiter:
    /// partition-key derivation, configuration binding/validation, the disabled
    /// pass-through, and the exact 429 rejection contract. No wall-clock sleeps —
    /// tests use tiny limits with <c>AutoReplenishment = false</c> and a fake lease.
    /// </summary>
    public class SessionWriteRateLimitUnitTests
    {
        // ---- helpers -------------------------------------------------------------------

        private static HttpContext ContextWithUser(params Claim[] claims)
        {
            var ctx = new DefaultHttpContext();
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, claims.Length > 0 ? "TestAuth" : null));
            return ctx;
        }

        private static SessionWriteRateLimiterPolicy PolicyWith(Action<SessionWriteRateLimitOptions>? tune = null)
        {
            var o = new SessionWriteRateLimitOptions
            {
                Enabled = true,
                TokenLimit = 3,
                TokensPerPeriod = 1,
                ReplenishmentPeriodSeconds = 3600,
                QueueLimit = 0,
                AutoReplenishment = false,
            };
            tune?.Invoke(o);
            return new SessionWriteRateLimiterPolicy(Options.Create(o));
        }

        private static async Task<int> AdmittedOutOf(SessionWriteRateLimiterPolicy policy, HttpContext ctx, int attempts)
        {
            using var limiter = PartitionedRateLimiter.Create<HttpContext, string>(policy.GetPartition);
            var admitted = 0;
            for (var i = 0; i < attempts; i++)
            {
                using var lease = await limiter.AcquireAsync(ctx, permitCount: 1, CancellationToken.None);
                if (lease.IsAcquired)
                {
                    admitted++;
                }
            }
            return admitted;
        }

        // ---- 1-4: partition key -------------------------------------------------------

        [Fact] // 1
        public void DistinctUsers_ProduceDistinctPartitionKeys()
        {
            var a = SessionWriteRateLimiterPolicy.ResolvePartitionKey(
                ContextWithUser(new Claim(ClaimTypes.NameIdentifier, "1")));
            var b = SessionWriteRateLimiterPolicy.ResolvePartitionKey(
                ContextWithUser(new Claim(ClaimTypes.NameIdentifier, "2")));

            Assert.Equal("u:1", a);
            Assert.Equal("u:2", b);
            Assert.NotEqual(a, b);
        }

        [Fact] // 2
        public void SameUser_AlwaysProducesTheSamePartitionKey()
        {
            var keys = Enumerable.Range(0, 5)
                .Select(_ => SessionWriteRateLimiterPolicy.ResolvePartitionKey(
                    ContextWithUser(new Claim(ClaimTypes.NameIdentifier, "42"))))
                .Distinct()
                .ToList();

            Assert.Equal(new[] { "u:42" }, keys);
        }

        [Fact] // 3
        public void BodyQueryHeaderUserIds_CannotAffectThePartition()
        {
            var ctx = ContextWithUser(new Claim(ClaimTypes.NameIdentifier, "7"));
            ctx.Request.Headers["X-User-Id"] = "999";
            ctx.Request.Headers["userId"] = "999";
            ctx.Request.QueryString = new QueryString("?userId=999&user=999");
            ctx.Items["userId"] = 999;

            Assert.Equal("u:7", SessionWriteRateLimiterPolicy.ResolvePartitionKey(ctx));
        }

        [Fact] // 4
        public void MissingOrNonNumericIdentity_ReturnsAnon_WithoutThrowing()
        {
            Assert.Equal("anon", SessionWriteRateLimiterPolicy.ResolvePartitionKey(new DefaultHttpContext()));
            Assert.Equal("anon", SessionWriteRateLimiterPolicy.ResolvePartitionKey(ContextWithUser()));
            Assert.Equal("anon", SessionWriteRateLimiterPolicy.ResolvePartitionKey(
                ContextWithUser(new Claim(ClaimTypes.NameIdentifier, "not-a-number"))));
            Assert.Equal("anon", SessionWriteRateLimiterPolicy.ResolvePartitionKey(
                ContextWithUser(new Claim(ClaimTypes.NameIdentifier, ""))));
            Assert.Equal("anon", SessionWriteRateLimiterPolicy.ResolvePartitionKey(
                ContextWithUser(new Claim(ClaimTypes.NameIdentifier, "0"))));
            Assert.Equal("anon", SessionWriteRateLimiterPolicy.ResolvePartitionKey(
                ContextWithUser(new Claim(ClaimTypes.NameIdentifier, "-5"))));
        }

        // ---- 5-6: configuration -----------------------------------------------------

        [Fact] // 5
        public void ValidConfiguration_BindsCorrectly()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:SessionWrite:Enabled"] = "true",
                    ["RateLimiting:SessionWrite:TokenLimit"] = "40",
                    ["RateLimiting:SessionWrite:TokensPerPeriod"] = "8",
                    ["RateLimiting:SessionWrite:ReplenishmentPeriodSeconds"] = "60",
                    ["RateLimiting:SessionWrite:QueueLimit"] = "0",
                    ["RateLimiting:SessionWrite:AutoReplenishment"] = "true",
                })
                .Build();

            var provider = new ServiceCollection()
                .AddGoHardRateLimiting(config)
                .BuildServiceProvider();

            var o = provider.GetRequiredService<IOptions<SessionWriteRateLimitOptions>>().Value;

            Assert.True(o.Enabled);
            Assert.Equal(40, o.TokenLimit);
            Assert.Equal(8, o.TokensPerPeriod);
            Assert.Equal(60, o.ReplenishmentPeriodSeconds);
            Assert.Equal(0, o.QueueLimit);
            Assert.True(o.AutoReplenishment);
        }

        [Fact] // 5b: a later configuration source overrides the bound TokenLimit; other
               // keys keep their earlier value. Isolated ConfigurationBuilder, no process
               // state (env-var `__`->`:` translation is a framework guarantee, not retested here).
        public void ConfigurationOverride_ChangesBoundTokenLimit()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // earlier source (e.g. appsettings.json)
                    ["RateLimiting:SessionWrite:TokenLimit"] = "40",
                    ["RateLimiting:SessionWrite:TokensPerPeriod"] = "8",
                    ["RateLimiting:SessionWrite:ReplenishmentPeriodSeconds"] = "60",
                })
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // later source (e.g. appsettings.{Environment}.json / an env var) — last wins
                    ["RateLimiting:SessionWrite:TokenLimit"] = "5",
                })
                .Build();

            var provider = new ServiceCollection()
                .AddGoHardRateLimiting(config)
                .BuildServiceProvider();

            var o = provider.GetRequiredService<IOptions<SessionWriteRateLimitOptions>>().Value;
            Assert.Equal(5, o.TokenLimit);       // override won
            Assert.Equal(8, o.TokensPerPeriod);  // untouched earlier value preserved
            Assert.Equal(60, o.ReplenishmentPeriodSeconds);
        }

        [Theory] // 6
        [InlineData(0, 8, 60, 0, "TokenLimit")]
        [InlineData(-1, 8, 60, 0, "TokenLimit")]
        [InlineData(1_000_001, 8, 60, 0, "TokenLimit")]
        [InlineData(40, 0, 60, 0, "TokensPerPeriod")]
        [InlineData(40, 1_000_001, 60, 0, "TokensPerPeriod")]
        [InlineData(40, 8, 0, 0, "ReplenishmentPeriodSeconds")]
        [InlineData(40, 8, -1, 0, "ReplenishmentPeriodSeconds")]
        [InlineData(40, 8, 86_401, 0, "ReplenishmentPeriodSeconds")]
        [InlineData(40, 8, 60, -1, "QueueLimit")]
        [InlineData(40, 8, 60, 100_001, "QueueLimit")]
        public void InvalidConfiguration_FailsWithAClearMessage(
            int tokenLimit, int perPeriod, int periodSeconds, int queueLimit, string offendingKey)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:SessionWrite:TokenLimit"] = tokenLimit.ToString(),
                    ["RateLimiting:SessionWrite:TokensPerPeriod"] = perPeriod.ToString(),
                    ["RateLimiting:SessionWrite:ReplenishmentPeriodSeconds"] = periodSeconds.ToString(),
                    ["RateLimiting:SessionWrite:QueueLimit"] = queueLimit.ToString(),
                })
                .Build();

            var provider = new ServiceCollection()
                .AddGoHardRateLimiting(config)
                .BuildServiceProvider();

            var ex = Assert.ThrowsAny<Exception>(
                () => _ = provider.GetRequiredService<IOptions<SessionWriteRateLimitOptions>>().Value);

            var message = Flatten(ex);
            Assert.Contains(offendingKey, message, StringComparison.Ordinal);
            Assert.Contains("RateLimiting:SessionWrite", message, StringComparison.Ordinal);
        }

        // ---- 7: disabled bypass -----------------------------------------------------

        [Fact] // 7
        public async Task Disabled_BypassesTheNamedLimiter_EveryRequestAdmitted()
        {
            var policy = PolicyWith(o => { o.Enabled = false; o.TokenLimit = 1; });
            var ctx = ContextWithUser(new Claim(ClaimTypes.NameIdentifier, "1"));

            Assert.Equal(50, await AdmittedOutOf(policy, ctx, 50));
        }

        [Fact] // 7d: no queuing -> QueueLimit flows from config and defaults to 0
        public void BucketOptions_HaveNoQueue_ByDefault_AndHonorConfig()
        {
            Assert.Equal(0, PolicyWith().BuildBucketOptions().QueueLimit);
            Assert.Equal(0, PolicyWith(o => o.QueueLimit = 0).BuildBucketOptions().QueueLimit);
            // If a future config sets it, it must be reflected exactly (never silently 1).
            Assert.Equal(3, PolicyWith(o => o.QueueLimit = 3).BuildBucketOptions().QueueLimit);

            var opts = PolicyWith(o =>
            {
                o.TokenLimit = 40;
                o.TokensPerPeriod = 8;
                o.ReplenishmentPeriodSeconds = 60;
                o.AutoReplenishment = true;
            }).BuildBucketOptions();
            Assert.Equal(40, opts.TokenLimit);
            Assert.Equal(8, opts.TokensPerPeriod);
            Assert.Equal(TimeSpan.FromSeconds(60), opts.ReplenishmentPeriod);
            Assert.True(opts.AutoReplenishment);
        }

        [Fact] // 7b: enabled -> the bucket actually limits (control for the disabled test)
        public async Task Enabled_LimitsToTheTokenBucketCapacity()
        {
            var policy = PolicyWith(o => o.TokenLimit = 3);
            var ctx = ContextWithUser(new Claim(ClaimTypes.NameIdentifier, "1"));

            Assert.Equal(3, await AdmittedOutOf(policy, ctx, 10));
        }

        [Fact] // 7e: a request with no valid user identity gets a no-op limiter, never a shared "anon" bucket
        public async Task NoValidUser_GetsANoOpLimiter_NotASharedAnonBucket()
        {
            var policy = PolicyWith(o => o.TokenLimit = 1);

            // no principal at all
            Assert.Equal(50, await AdmittedOutOf(policy, new DefaultHttpContext(), 50));
            // present principal, no usable id
            Assert.Equal(50, await AdmittedOutOf(policy, ContextWithUser(), 50));
            Assert.Equal(50, await AdmittedOutOf(policy,
                ContextWithUser(new Claim(ClaimTypes.NameIdentifier, "not-a-number")), 50));
        }

        [Fact] // 7c: enabled -> two users have independent buckets
        public async Task Enabled_PartitionsPerUser()
        {
            var policy = PolicyWith(o => o.TokenLimit = 2);
            using var limiter = PartitionedRateLimiter.Create<HttpContext, string>(policy.GetPartition);
            var userA = ContextWithUser(new Claim(ClaimTypes.NameIdentifier, "1"));
            var userB = ContextWithUser(new Claim(ClaimTypes.NameIdentifier, "2"));

            Assert.True((await limiter.AcquireAsync(userA)).IsAcquired);
            Assert.True((await limiter.AcquireAsync(userA)).IsAcquired);
            Assert.False((await limiter.AcquireAsync(userA)).IsAcquired); // A exhausted
            Assert.True((await limiter.AcquireAsync(userB)).IsAcquired);  // B independent
        }

        // ---- 8-9: rejection contract ----------------------------------------------

        [Fact] // 8 + 9
        public async Task Rejection_Returns429_ExactBody_IntegerRetryAfter_NoDisclosure()
        {
            var policy = PolicyWith(o =>
            {
                o.TokenLimit = 40;
                o.TokensPerPeriod = 8;
                o.ReplenishmentPeriodSeconds = 60; // => Retry-After = the whole period (lump replenish)
            });

            var ctx = new DefaultHttpContext();
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "12345") }, "TestAuth"));
            var body = new MemoryStream();
            ctx.Response.Body = body;

            var onRejected = policy.OnRejected!;
            await onRejected(new OnRejectedContext { HttpContext = ctx, Lease = new FailedLease() }, CancellationToken.None);

            Assert.Equal(429, ctx.Response.StatusCode);
            Assert.Equal("application/json", ctx.Response.ContentType);

            var text = System.Text.Encoding.UTF8.GetString(body.ToArray());
            Assert.Equal("{\"code\":\"rate_limited\"}", text);

            var retryAfter = ctx.Response.Headers.RetryAfter.ToString();
            Assert.True(int.TryParse(retryAfter, out var seconds), $"Retry-After '{retryAfter}' is not an integer");
            Assert.True(seconds >= 1, "Retry-After must be >= 1");
            Assert.Equal(60, seconds); // the full replenishment period, so a client never retries too early

            // 9: no identity / limits / partition key / internal state leaked anywhere.
            Assert.DoesNotContain("12345", text);
            Assert.DoesNotContain("u:", text);
            Assert.DoesNotContain("40", text);
            Assert.DoesNotContain("token", text, StringComparison.OrdinalIgnoreCase);
            foreach (var h in ctx.Response.Headers)
            {
                Assert.DoesNotContain("12345", h.Value.ToString());
                Assert.False(h.Key.Contains("User", StringComparison.OrdinalIgnoreCase));
                Assert.False(h.Key.Contains("Limit", StringComparison.OrdinalIgnoreCase));
                Assert.False(h.Key.Contains("Partition", StringComparison.OrdinalIgnoreCase));
            }
        }

        // ---- 10-11: existing limiters ----------------------------------------------

        [Fact] // 10: no global OnRejected installed -> auth/global 429s keep their bare shape
        public void Registration_InstallsNoGlobalOnRejectedHandler()
        {
            var provider = new ServiceCollection()
                .AddGoHardRateLimiting(new ConfigurationBuilder().Build())
                .BuildServiceProvider();

            var options = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;

            Assert.Null(options.OnRejected);
            Assert.Equal(429, options.RejectionStatusCode);
            Assert.NotNull(options.GlobalLimiter);
        }

        [Fact] // 11: session-write is the only registered rate-limiter policy; "api" and the
               // old socket-IP "auth" policy are gone, and no [EnableRateLimiting] references them
        public void SessionWriteIsTheOnlyPolicy_ApiAndAuthPoliciesAreGone()
        {
            var provider = new ServiceCollection()
                .AddGoHardRateLimiting(new ConfigurationBuilder().Build())
                .BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;

            var registeredPolicies = PolicyNames(options);
            // Guard against the reflection silently returning nothing on a future BCL
            // restructure, which would make the DoesNotContain asserts pass vacuously.
            Assert.NotEmpty(registeredPolicies);
            Assert.Contains(SessionWriteRateLimiterPolicy.PolicyName, registeredPolicies);
            Assert.DoesNotContain("api", registeredPolicies);
            Assert.DoesNotContain("auth", registeredPolicies);

            // No endpoint references any rate-limiter policy other than session-write.
            var referenced = typeof(SessionsController).Assembly.GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Cast<MemberInfo>()
                    .Concat(new MemberInfo[] { t }))
                .SelectMany(m => m.GetCustomAttributes<EnableRateLimitingAttribute>())
                .Select(a => a.PolicyName)
                .Where(n => n is not null)
                .Distinct()
                .ToList();

            Assert.DoesNotContain("api", referenced);
            Assert.DoesNotContain("auth", referenced);
            Assert.All(referenced, n => Assert.Equal(SessionWriteRateLimiterPolicy.PolicyName, n));
        }

        // ---- reflection over RateLimiterOptions's internal policy maps --------------

        private static HashSet<string> PolicyNames(RateLimiterOptions options)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            foreach (var member in options.GetType()
                         .GetProperties(flags).Cast<MemberInfo>()
                         .Concat(options.GetType().GetFields(flags)))
            {
                object? value = member switch
                {
                    PropertyInfo p when p.GetIndexParameters().Length == 0 => p.GetValue(options),
                    FieldInfo f => f.GetValue(options),
                    _ => null,
                };

                if (value is IDictionary dict)
                {
                    foreach (var key in dict.Keys)
                    {
                        if (key is string s)
                        {
                            names.Add(s);
                        }
                    }
                }
            }

            return names;
        }

        private static string Flatten(Exception ex)
        {
            var parts = new List<string>();
            for (var e = ex; e is not null; e = e.InnerException)
            {
                parts.Add(e.Message);
                if (e is OptionsValidationException ove)
                {
                    parts.AddRange(ove.Failures);
                }
            }
            return string.Join(" | ", parts);
        }
    }

    /// <summary>A minimal never-acquired lease for exercising <c>OnRejected</c> deterministically.</summary>
    internal sealed class FailedLease : RateLimitLease
    {
        public override bool IsAcquired => false;
        public override IEnumerable<string> MetadataNames => Array.Empty<string>();
        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            metadata = null;
            return false;
        }
    }
}
