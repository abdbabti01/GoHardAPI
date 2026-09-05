using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace GoHardAPI.RateLimiting
{
    /// <summary>
    /// Proxy-independent authentication-attempt limiter: a fixed window per
    /// <b>submitted credential identity</b> (see <see cref="AuthAttemptIdentity"/>),
    /// applied by <see cref="AuthAttemptRateLimitFilter"/> after MVC model binding.
    ///
    /// <para>Replaces the old <c>[EnableRateLimiting("auth")]</c> socket-IP limiter,
    /// which behind Railway's edge collapsed every login/signup on the platform into
    /// one bucket (5 attempts locked everyone out). Partitioning by identity means one
    /// targeted identity's attempts cannot deny login to unrelated users; a
    /// rotating-identity attacker is instead bounded by the aggregate anonymous
    /// <c>GlobalLimiter</c> ceiling (service protection, not per-client fairness).</para>
    ///
    /// <para>Default 5 requests / 60 s / queue 2 per identity — the limit the old
    /// <c>auth</c> limiter carried. Tune via section <c>RateLimiting:Auth</c>. State is
    /// process-local (one instance assumed, as elsewhere).</para>
    /// </summary>
    public sealed class AuthAttemptRateLimiter : IDisposable, IAsyncDisposable
    {
        /// <summary>Named-options key for this limiter's <see cref="FixedWindowRateLimitOptions"/>.</summary>
        public const string OptionsName = "auth";

        internal const int DefaultPermitLimit = 5;
        internal const int DefaultWindowSeconds = 60;
        internal const int DefaultQueueLimit = 2;

        private readonly PartitionedRateLimiter<string> _limiter;

        public AuthAttemptRateLimiter(IOptionsMonitor<FixedWindowRateLimitOptions> options)
        {
            _limiter = PartitionedRateLimiter.Create<string, string>(
                partitionKey =>
                {
                    var o = options.Get(OptionsName);
                    return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = o.PermitLimit,
                        Window = o.Window,
                        QueueLimit = o.QueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true,
                    });
                },
                StringComparer.Ordinal);
        }

        /// <summary>
        /// Acquires one permit for <paramref name="partitionKey"/>. Honors
        /// <paramref name="cancellationToken"/> (the request's <c>RequestAborted</c>) —
        /// a cancelled wait throws <see cref="OperationCanceledException"/> and the
        /// caller must not run the action. The returned lease must be disposed.
        /// </summary>
        public ValueTask<RateLimitLease> AcquireAsync(string partitionKey, CancellationToken cancellationToken)
            => _limiter.AcquireAsync(partitionKey, permitCount: 1, cancellationToken);

        public void Dispose() => _limiter.Dispose();

        public ValueTask DisposeAsync() => _limiter.DisposeAsync();
    }
}
