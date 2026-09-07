using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace GoHardAPI.RateLimiting
{
    /// <summary>
    /// Central rate-limiter wiring for the API.
    ///
    /// <list type="bullet">
    ///   <item><b>Auth attempts</b> — <see cref="AuthAttemptRateLimiter"/>, a fixed
    ///     window (default 5/min, queue 2) <b>per submitted credential identity</b>
    ///     (see <see cref="AuthAttemptIdentity"/>), applied by
    ///     <see cref="AuthAttemptRateLimitFilter"/> on <c>login</c> / <c>signup</c>.
    ///     Replaces the old socket-IP <c>[EnableRateLimiting("auth")]</c> policy, which
    ///     behind Railway's edge locked every user out of login. Section
    ///     <c>RateLimiting:Auth</c>.</item>
    ///   <item><c>GlobalLimiter</c> — the app-wide aggregate ceiling. Authenticated
    ///     requests partition by JWT user id (<c>RateLimiting:Global</c>, default
    ///     200/min); anonymous requests partition by socket peer IP
    ///     (<c>RateLimiting:GlobalAnonymous</c>, default 200/min — a <b>separate</b>
    ///     knob because behind a shared proxy this is one aggregate bucket for all
    ///     anonymous traffic, not per-client fairness). Runs before
    ///     <c>UseAuthorization</c> so invalid / missing-token traffic to protected
    ///     routes is still constrained. It also bounds a rotating-identity auth
    ///     attacker — service protection, not per-client fairness.</item>
    ///   <item><c>"session-write"</c> — the per-authenticated-user token bucket
    ///     (<see cref="SessionWriteRateLimiterPolicy"/>) for <c>POST /api/v1/sessions</c>,
    ///     <c>POST /api/v1/sessions/from-program-workout</c> and the cancel counterpart.
    ///     No-op limiter for a request with no valid user identity.</item>
    /// </list>
    ///
    /// The previously-defined <c>"api"</c> (100/min) and <c>"auth"</c> named policies
    /// are not registered: neither has a rate-limiter-policy consumer any more.
    ///
    /// <para><b>Process-local state — assumes ONE Railway API instance.</b> Every
    /// partition lives in this process's memory. With N instances the effective
    /// per-partition limit is N x the configured value.</para>
    /// </summary>
    public static class RateLimiterRegistration
    {
        public static IServiceCollection AddGoHardRateLimiting(
            this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<ClientPartitionKeyResolver>();

            AddFixedWindowOptions(services, configuration,
                AuthAttemptRateLimiter.OptionsName, FixedWindowRateLimitOptions.AuthSectionName,
                AuthAttemptRateLimiter.DefaultPermitLimit,
                AuthAttemptRateLimiter.DefaultWindowSeconds,
                AuthAttemptRateLimiter.DefaultQueueLimit);

            AddFixedWindowOptions(services, configuration,
                AuthenticatedGlobalOptionsName, FixedWindowRateLimitOptions.GlobalSectionName,
                DefaultGlobalPermitLimit, DefaultGlobalWindowSeconds, DefaultGlobalQueueLimit);

            AddFixedWindowOptions(services, configuration,
                AnonymousGlobalOptionsName, FixedWindowRateLimitOptions.GlobalAnonymousSectionName,
                DefaultGlobalPermitLimit, DefaultGlobalWindowSeconds, DefaultGlobalQueueLimit);

            services.AddSingleton<AuthAttemptRateLimiter>();

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

                // --- per-authenticated-user token bucket for the Session create-operation
                //     write path (POST /sessions, POST /sessions/from-program-workout,
                //     DELETE /sessions/by-operation/{key}) ---
                options.AddPolicy<string, SessionWriteRateLimiterPolicy>(
                    SessionWriteRateLimiterPolicy.PolicyName);

                // --- app-wide aggregate ceiling: separate knobs for authed vs anonymous ---
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var resolver = context.RequestServices.GetRequiredService<ClientPartitionKeyResolver>();

                    // A login / signup attempt is keyed by the anonymous (socket-IP)
                    // aggregate ceiling even if it carries a bearer token, so an attacker
                    // holding one real account cannot use the authenticated per-user
                    // ceiling to probe rotating victim identities.
                    var authEndpoint = context.GetEndpoint()?.Metadata.GetMetadata<IAuthAttemptEndpoint>() is not null;

                    var key = authEndpoint
                        ? resolver.ResolveClientIpPartitionKey(context)
                        : resolver.ResolveGlobalPartitionKey(context);
                    var authenticated = key.StartsWith("u:", StringComparison.Ordinal);

                    var limits = context.RequestServices
                        .GetRequiredService<IOptionsMonitor<FixedWindowRateLimitOptions>>()
                        .Get(authenticated ? AuthenticatedGlobalOptionsName : AnonymousGlobalOptionsName);

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: key,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = limits.PermitLimit,
                            Window = limits.Window,
                            QueueLimit = limits.QueueLimit, // default 0 == pre-change behavior
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        });
                });
            });

            return services;
        }

        internal const string AuthenticatedGlobalOptionsName = "global";
        internal const string AnonymousGlobalOptionsName = "global-anon";

        // Pre-change hard-coded values; also the config defaults.
        internal const int DefaultGlobalPermitLimit = 200;
        internal const int DefaultGlobalWindowSeconds = 60;
        internal const int DefaultGlobalQueueLimit = 0;

        private static void AddFixedWindowOptions(
            IServiceCollection services, IConfiguration configuration,
            string name, string sectionName,
            int defaultPermitLimit, int defaultWindowSeconds, int defaultQueueLimit)
        {
            services.AddOptions<FixedWindowRateLimitOptions>(name)
                .Configure(o =>
                {
                    o.PermitLimit = defaultPermitLimit;
                    o.WindowSeconds = defaultWindowSeconds;
                    o.QueueLimit = defaultQueueLimit;
                })
                .Bind(configuration.GetSection(sectionName))
                .Validate(o =>
                {
                    o.Validate(sectionName);
                    return true;
                })
                .ValidateOnStart();
        }
    }
}
