using System.Globalization;
using System.Net;
using System.Security.Claims;

namespace GoHardAPI.RateLimiting
{
    /// <summary>
    /// The one place that decides "which client is this request?" for rate limiting.
    /// Every limiter partition key in the app comes from here so the rules live in a
    /// single testable unit.
    ///
    /// <para><see cref="ResolveGlobalPartitionKey"/> is used by the app-wide
    /// <c>GlobalLimiter</c>: a request carrying a valid authenticated user id is
    /// partitioned by that id (<c>u:&lt;id&gt;</c>); everything else is partitioned by
    /// the normalized socket peer address (<c>ip:&lt;value&gt;</c>).
    /// <see cref="ResolveClientIpPartitionKey"/> is the IP-only form.</para>
    ///
    /// <para><b>No forwarding-header trust.</b> The client identity is always the real
    /// socket peer address (<c>HttpContext.Connection.RemoteIpAddress</c>). Forwarding
    /// headers (<c>X-Forwarded-For</c>, <c>X-Real-IP</c>, <c>X-Railway-Edge</c>, …) are
    /// never read: they are attacker-controllable unless a proven trusted-proxy
    /// boundary is configured, and Railway publishes neither stable inbound proxy
    /// CIDRs nor an authoritative guarantee that inbound copies are sanitized. Behind a
    /// shared reverse proxy the anonymous partition is therefore as coarse as the set
    /// of proxy egress addresses; the authenticated partition (JWT user id) is
    /// unaffected. A missing socket address falls back to the bounded constant
    /// <see cref="UnknownClientKey"/> — never a unique per-request value.</para>
    ///
    /// <para><b>Logging.</b> This type logs nothing. Callers must not log the returned
    /// key at information level or above: it can contain a full client IP.</para>
    /// </summary>
    public sealed class ClientPartitionKeyResolver
    {
        /// <summary>Partition key for a request with no usable authenticated identity and no usable IP.</summary>
        public const string UnknownClientKey = "ip:unknown";

        private const string UserKeyPrefix = "u:";
        private const string IpKeyPrefix = "ip:";

        /// <summary>App-wide limiter key: authenticated user id when present, else socket peer IP.</summary>
        public string ResolveGlobalPartitionKey(HttpContext httpContext)
        {
            ArgumentNullException.ThrowIfNull(httpContext);
            return TryResolveUserKey(httpContext, out var userKey)
                ? userKey
                : ResolveClientIpPartitionKey(httpContext);
        }

        /// <summary><c>ip:&lt;normalized&gt;</c>, or <see cref="UnknownClientKey"/> when no address is available.</summary>
        public string ResolveClientIpPartitionKey(HttpContext httpContext)
        {
            ArgumentNullException.ThrowIfNull(httpContext);
            var ip = httpContext.Connection.RemoteIpAddress;
            return ip is null ? UnknownClientKey : IpKeyPrefix + ClientIpNormalizer.Normalize(ip);
        }

        /// <summary>
        /// <c>u:&lt;id&gt;</c> from the server-validated <see cref="ClaimTypes.NameIdentifier"/>
        /// claim. Never derived from a body, query string, or arbitrary header. Never throws.
        /// </summary>
        internal static bool TryResolveUserKey(HttpContext httpContext, out string key)
        {
            key = string.Empty;
            try
            {
                var value = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(value)
                    && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId)
                    && userId > 0)
                {
                    key = UserKeyPrefix + userId.ToString(CultureInfo.InvariantCulture);
                    return true;
                }
            }
            catch
            {
                // A partition-callback exception surfaces as a per-request 500; fail
                // closed to the IP partition instead.
            }

            return false;
        }
    }
}
