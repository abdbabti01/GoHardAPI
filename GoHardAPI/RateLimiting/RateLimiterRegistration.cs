using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace GoHardAPI.RateLimiting
{
    /// <summary>
    /// Central rate-limiter wiring for the API.
    ///
    /// Preserves the two pre-existing limiters unchanged:
    /// <list type="bullet">
    ///   <item><c>"auth"</c> — fixed window 5/min, applied by
    ///     <c>[EnableRateLimiting("auth")]</c> on <c>AuthController</c>;</item>
    ///   <item>the per-IP <c>GlobalLimiter</c> — fixed window 200/min, applied to
    ///     every request.</item>
    /// </list>
    /// Adds the per-user token-bucket <see cref="SessionWriteRateLimiterPolicy"/>
    /// (<c>"session-write"</c>), applied only by
    /// <c>[EnableRateLimiting("session-write")]</c> on
    /// <c>SessionsController.CreateSession</c>.
    ///
    /// The previously-defined <c>"api"</c> named limiter (100/min) is intentionally
    /// NOT registered here: it had zero consumers (no
    /// <c>[EnableRateLimiting("api")]</c> anywhere) and was dead configuration.
    ///
    /// <para><b>Process-local state — assumes ONE Railway API instance.</b> The
    /// <c>session-write</c> token bucket and every partition live in this
    /// process's memory. With N instances the effective per-user limit is
    /// N x <c>TokenLimit</c> and a user is throttled unevenly across instances.
    /// Horizontal scale-out would need a distributed limiter (Redis) or an edge /
    /// gateway limit; neither is in this PR. See <see cref="SessionWriteRateLimitOptions"/>.</para>
    /// </summary>
    public static class RateLimiterRegistration
    {
        public static IServiceCollection AddGoHardRateLimiting(
            this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<SessionWriteRateLimitOptions>()
                .Bind(configuration.GetSection(SessionWriteRateLimitOptions.SectionName))
                .Validate(o =>
                {
                    o.Validate(); // throws with a clear message on a bad value
                    return true;
                })
                .ValidateOnStart();

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // --- unchanged: strict limit for authentication endpoints ---
                options.AddFixedWindowLimiter("auth", limiterOptions =>
                {
                    limiterOptions.PermitLimit = 5;
                    limiterOptions.Window = TimeSpan.FromMinutes(1);
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = 2;
                });

                // --- new: per-authenticated-user token bucket for generic Session CREATE ---
                options.AddPolicy<string, SessionWriteRateLimiterPolicy>(
                    SessionWriteRateLimiterPolicy.PolicyName);

                // --- unchanged: per-IP limit for unauthenticated / general traffic ---
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 200,
                            Window = TimeSpan.FromMinutes(1)
                        });
                });
            });

            return services;
        }
    }
}
