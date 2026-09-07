namespace GoHardAPI.RateLimiting
{
    /// <summary>
    /// Bound from configuration section <see cref="SectionName"/>
    /// (<c>RateLimiting:SessionWrite</c>), overridable by environment variables
    /// (e.g. <c>RateLimiting__SessionWrite__TokenLimit</c>).
    ///
    /// Drives the per-authenticated-user token-bucket limiter applied to the Session
    /// create-operation write path: <c>POST /api/v1/sessions</c> and
    /// <c>DELETE /api/v1/sessions/by-operation/{clientOperationId}</c> (one shared
    /// per-user bucket). It does not touch any other endpoint, the <c>"auth"</c>
    /// limiter, or the per-IP global limiter.
    ///
    /// State is process-local: the bucket lives in this instance's memory. The
    /// design assumes ONE Railway API instance; with N instances the effective
    /// per-user limit is N x <see cref="TokenLimit"/>. There is no distributed
    /// coordination and none is planned in this PR.
    /// </summary>
    public sealed class SessionWriteRateLimitOptions
    {
        public const string SectionName = "RateLimiting:SessionWrite";

        /// <summary>
        /// When <c>false</c>, the <c>session-write</c> policy is still registered
        /// (so <c>[EnableRateLimiting("session-write")]</c> resolves) but becomes a
        /// pass-through no-op. Other limiters are unaffected.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Bucket capacity / maximum burst. Must be &gt; 0.</summary>
        public int TokenLimit { get; set; } = 40;

        /// <summary>Tokens added each replenishment period. Must be &gt; 0.</summary>
        public int TokensPerPeriod { get; set; } = 8;

        /// <summary>Length of one replenishment period, in seconds. Must be &gt; 0.</summary>
        public int ReplenishmentPeriodSeconds { get; set; } = 60;

        /// <summary>Queued (waiting) requests allowed. Must be &gt;= 0; default 0 = reject immediately.</summary>
        public int QueueLimit { get; set; }

        /// <summary>Let the framework timer replenish tokens. Tests set this false for determinism.</summary>
        public bool AutoReplenishment { get; set; } = true;

        // Upper bounds: a typo'd huge value would otherwise pass validation and
        // silently neuter the limiter with no signal. These are generous ceilings,
        // not tuning limits.
        internal const int MaxTokenLimit = 1_000_000;
        internal const int MaxTokensPerPeriod = 1_000_000;
        internal const int MaxReplenishmentPeriodSeconds = 86_400; // 1 day
        internal const int MaxQueueLimit = 100_000;

        /// <summary>
        /// Fails fast at startup (via <c>ValidateOnStart</c>) with a clear message
        /// when a value is out of range.
        /// </summary>
        public void Validate()
        {
            if (TokenLimit <= 0 || TokenLimit > MaxTokenLimit)
            {
                throw new InvalidOperationException(
                    $"{SectionName}:TokenLimit must be in [1, {MaxTokenLimit}] (was {TokenLimit}).");
            }

            if (TokensPerPeriod <= 0 || TokensPerPeriod > MaxTokensPerPeriod)
            {
                throw new InvalidOperationException(
                    $"{SectionName}:TokensPerPeriod must be in [1, {MaxTokensPerPeriod}] (was {TokensPerPeriod}).");
            }

            if (ReplenishmentPeriodSeconds <= 0 || ReplenishmentPeriodSeconds > MaxReplenishmentPeriodSeconds)
            {
                throw new InvalidOperationException(
                    $"{SectionName}:ReplenishmentPeriodSeconds must be in [1, {MaxReplenishmentPeriodSeconds}] " +
                    $"(was {ReplenishmentPeriodSeconds}).");
            }

            if (QueueLimit < 0 || QueueLimit > MaxQueueLimit)
            {
                throw new InvalidOperationException(
                    $"{SectionName}:QueueLimit must be in [0, {MaxQueueLimit}] (was {QueueLimit}).");
            }
        }

        /// <summary>
        /// Integer <c>Retry-After</c> seconds advertised on a 429. Always &gt;= 1.
        ///
        /// .NET's <c>TokenBucketRateLimiter</c> replenishes <see cref="TokensPerPeriod"/>
        /// tokens in a lump at each <see cref="ReplenishmentPeriodSeconds"/> boundary
        /// (it does not drip). After exhaustion the true wait for the next token is
        /// therefore up to a full period, so the hint is the whole period — a client
        /// that honors it never retries too early. Token-bucket leases carry no
        /// framework <c>RetryAfter</c> metadata, so the rejection handler derives the
        /// header from this.
        /// </summary>
        public int RetryAfterSeconds() =>
            ReplenishmentPeriodSeconds < 1 ? 1 : ReplenishmentPeriodSeconds;
    }
}
