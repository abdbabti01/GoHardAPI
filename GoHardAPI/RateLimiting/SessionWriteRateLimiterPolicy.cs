using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace GoHardAPI.RateLimiting
{
    /// <summary>
    /// Per-authenticated-user token-bucket policy for the Session create-operation write
    /// path:
    /// <list type="bullet">
    ///   <item><c>POST /api/v1/sessions</c> (keyed or legacy create);</item>
    ///   <item><c>POST /api/v1/sessions/from-program-workout</c> (keyed or legacy create —
    ///     it persists a Session, its child Exercises, and, when keyed, a
    ///     <c>SessionCreateOperations</c> row);</item>
    ///   <item>the cancel counterpart
    ///     <c>DELETE /api/v1/sessions/by-operation/{clientOperationId}</c>.</item>
    /// </list>
    /// All three carry <c>[EnableRateLimiting(PolicyName)]</c> and share one per-user bucket
    /// because all three write to <c>Sessions</c> / <c>SessionCreateOperations</c>. No other
    /// endpoint uses it.
    ///
    /// <para><b>Partition key</b> comes exclusively from the server-validated JWT
    /// <c>sub</c> claim (<see cref="ClaimTypes.NameIdentifier"/>) — never from a
    /// request body, query string, header, or any client-supplied id. A numeric
    /// id becomes <c>"u:&lt;id&gt;"</c>. A request with <b>no</b> valid user identity
    /// (no principal, missing or non-numeric claim) gets a <b>no-op limiter</b>: it
    /// opens and consumes nothing here. <c>UseRateLimiter</c> runs before
    /// <c>UseAuthorization</c>, so such a request is constrained by the app-wide
    /// <c>GlobalLimiter</c> (socket-peer partition) and then rejected <c>401</c> by
    /// authorization — it must not be able to drain a shared "anon" token bucket and
    /// deny generic Session CREATE to every unauthenticated caller. Partition creation
    /// never throws.</para>
    ///
    /// <para><b>Rejection</b> is handled here (policy-scoped, not a global
    /// <c>OnRejected</c>) so the existing <c>"auth"</c> limiter and the per-IP
    /// global limiter keep their current bare-429 responses. A rejected request
    /// returns <c>429</c> with <c>{"code":"rate_limited"}</c> and an integer
    /// <c>Retry-After</c> header (>= 1), and never reaches
    /// <c>SessionCreateService</c> — so no Session and no
    /// <c>SessionCreateOperation</c> row is written.</para>
    /// </summary>
    public sealed class SessionWriteRateLimiterPolicy : IRateLimiterPolicy<string>
    {
        public const string PolicyName = "session-write";

        internal const string AnonymousPartitionKey = "anon";

        private static readonly byte[] RejectionBody =
            JsonSerializer.SerializeToUtf8Bytes(new { code = "rate_limited" });

        private readonly SessionWriteRateLimitOptions _options;

        public SessionWriteRateLimiterPolicy(IOptions<SessionWriteRateLimitOptions> options)
        {
            _options = options.Value;
        }

        public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => RejectAsync;

        public RateLimitPartition<string> GetPartition(HttpContext httpContext)
        {
            var key = ResolvePartitionKey(httpContext);

            if (!_options.Enabled || key == AnonymousPartitionKey)
            {
                // Disabled -> registered but inert. No valid user identity -> a no-op
                // limiter so an unauthenticated caller (this policy now runs before
                // UseAuthorization) cannot open or drain a shared bucket; the
                // GlobalLimiter constrains it and authorization returns 401.
                return RateLimitPartition.GetNoLimiter(key);
            }

            return RateLimitPartition.GetTokenBucketLimiter(key, _ => BuildBucketOptions());
        }

        /// <summary>The token-bucket options this policy applies (test seam).</summary>
        internal TokenBucketRateLimiterOptions BuildBucketOptions() => new()
        {
            TokenLimit = _options.TokenLimit,
            TokensPerPeriod = _options.TokensPerPeriod,
            ReplenishmentPeriod = TimeSpan.FromSeconds(_options.ReplenishmentPeriodSeconds),
            QueueLimit = _options.QueueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = _options.AutoReplenishment,
        };

        /// <summary>
        /// JWT-<c>sub</c>-only key derivation. Defensive against a null principal /
        /// missing claim / non-numeric claim; never throws.
        /// </summary>
        internal static string ResolvePartitionKey(HttpContext httpContext)
        {
            try
            {
                var value = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(value)
                    && int.TryParse(value, out var userId)
                    && userId > 0)
                {
                    return "u:" + userId.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            catch
            {
                // Fall through to the anonymous partition — a partition-callback
                // exception would surface as a per-request 500.
            }

            return AnonymousPartitionKey;
        }

        private ValueTask RejectAsync(OnRejectedContext context, CancellationToken cancellationToken)
        {
            var response = context.HttpContext.Response;
            response.StatusCode = StatusCodes.Status429TooManyRequests;
            response.ContentType = "application/json";

            var retryAfter = _options.RetryAfterSeconds();
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var leaseRetryAfter))
            {
                var fromLease = (int)Math.Ceiling(leaseRetryAfter.TotalSeconds);
                if (fromLease > retryAfter)
                {
                    retryAfter = fromLease;
                }
            }

            response.Headers.RetryAfter =
                retryAfter.ToString(System.Globalization.CultureInfo.InvariantCulture);

            return new ValueTask(response.Body.WriteAsync(RejectionBody, 0, RejectionBody.Length, cancellationToken));
        }
    }
}
