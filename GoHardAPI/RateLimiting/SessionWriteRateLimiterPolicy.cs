using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace GoHardAPI.RateLimiting
{
    /// <summary>
    /// Per-authenticated-user token-bucket policy for generic Session CREATE
    /// (<c>POST /api/v1/sessions</c> only).
    ///
    /// <para><b>Partition key</b> comes exclusively from the server-validated JWT
    /// <c>sub</c> claim (<see cref="ClaimTypes.NameIdentifier"/>) — never from a
    /// request body, query string, header, or any client-supplied id. A numeric
    /// id becomes <c>"u:&lt;id&gt;"</c>; anything else (no principal, missing or
    /// non-numeric claim) becomes the constant <c>"anon"</c>. Partition creation
    /// never throws. Because <c>UseRateLimiter</c> runs after
    /// <c>UseAuthorization</c> and the endpoint is <c>[Authorize]</c>, a real
    /// unauthenticated caller is already rejected with 401 before reaching this
    /// policy; the <c>"anon"</c> bucket is purely defensive and never lets an
    /// unauthenticated request into the controller.</para>
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

            if (!_options.Enabled)
            {
                // Registered but inert: never limits, never affects other limiters.
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
