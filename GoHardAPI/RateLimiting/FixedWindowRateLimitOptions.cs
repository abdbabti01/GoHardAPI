namespace GoHardAPI.RateLimiting
{
    /// <summary>
    /// Numeric knobs for a partitioned fixed-window limiter. Bound once per consumer:
    /// the per-identity auth-attempt limiter (<c>RateLimiting:Auth</c>), the
    /// authenticated-user aggregate ceiling (<c>RateLimiting:Global</c>), and the
    /// anonymous aggregate ceiling (<c>RateLimiting:GlobalAnonymous</c>). Defaults
    /// reproduce the limits in effect before this change; configuration exists so a
    /// deployment can tune them and so tests can pick deterministic values.
    /// </summary>
    public sealed class FixedWindowRateLimitOptions
    {
        public const string AuthSectionName = "RateLimiting:Auth";
        public const string GlobalSectionName = "RateLimiting:Global";
        public const string GlobalAnonymousSectionName = "RateLimiting:GlobalAnonymous";

        /// <summary>Requests admitted per window per partition. Must be &gt; 0.</summary>
        public int PermitLimit { get; set; }

        /// <summary>Window length in seconds. Must be &gt; 0.</summary>
        public int WindowSeconds { get; set; } = 60;

        /// <summary>Requests allowed to wait for the next window. Must be &gt;= 0.</summary>
        public int QueueLimit { get; set; }

        internal const int MaxPermitLimit = 10_000_000;
        internal const int MaxWindowSeconds = 86_400;
        internal const int MaxQueueLimit = 1_000_000;

        public void Validate(string sectionName)
        {
            if (PermitLimit <= 0 || PermitLimit > MaxPermitLimit)
            {
                throw new InvalidOperationException(
                    $"{sectionName}:PermitLimit must be in [1, {MaxPermitLimit}] (was {PermitLimit}).");
            }

            if (WindowSeconds <= 0 || WindowSeconds > MaxWindowSeconds)
            {
                throw new InvalidOperationException(
                    $"{sectionName}:WindowSeconds must be in [1, {MaxWindowSeconds}] (was {WindowSeconds}).");
            }

            if (QueueLimit < 0 || QueueLimit > MaxQueueLimit)
            {
                throw new InvalidOperationException(
                    $"{sectionName}:QueueLimit must be in [0, {MaxQueueLimit}] (was {QueueLimit}).");
            }
        }

        public TimeSpan Window => TimeSpan.FromSeconds(WindowSeconds);
    }
}
