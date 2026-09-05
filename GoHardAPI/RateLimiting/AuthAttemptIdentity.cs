using System.Security.Cryptography;
using System.Text;

namespace GoHardAPI.RateLimiting
{
    /// <summary>
    /// Derives the rate-limit partition key for an authentication attempt from the
    /// <b>submitted credential identity</b> (the email on the bound login / signup
    /// DTO) — never from the socket peer, a forwarding header, or the password.
    ///
    /// <list type="bullet">
    ///   <item><b>Identity equivalence matches production authentication exactly.</b>
    ///     <c>IUserRepository.GetByEmailAsync</c> runs <c>WHERE "Email" = @p</c> against
    ///     a PostgreSQL <c>character varying(255)</c> column with the database's default
    ///     (case-sensitive, whitespace-significant) collation, no case-insensitive
    ///     collation and no unique index — so two emails differing only in case or
    ///     surrounding whitespace are <b>different accounts</b> that can coexist. The
    ///     limiter therefore keys on the <b>exact validated email string</b> the
    ///     controller received: no trim, no case folding, no Unicode normalization.
    ///     Merging identities the auth lookup treats as distinct would let two accounts
    ///     lock each other out.</item>
    ///   <item>The key embeds only an <b>opaque keyed digest</b> (HMAC-SHA-256 under a
    ///     per-process random key) of <c>scope + exactEmail</c> — the raw email never
    ///     appears in the key, a log, an exception, a metric, or a 429 response, and
    ///     the digest is not dictionary-reversible outside this process.</item>
    ///   <item><c>login</c> and <c>signup</c> use different scopes, so the same email
    ///     is two independent partitions.</item>
    ///   <item>A missing DTO / null / empty identity uses the single, bounded,
    ///     scope-specific <see cref="FallbackKey"/> — never a random or raw-text key.
    ///     (A whitespace-only email never reaches here: <c>[Required][EmailAddress]</c>
    ///     + <c>[ApiController]</c> returns 400 before the filter runs.)</item>
    /// </list>
    ///
    /// Existent and nonexistent accounts produce keys the same way, so the limiter
    /// gives no account-enumeration signal.
    /// </summary>
    public static class AuthAttemptIdentity
    {
        public const string LoginScope = "login";
        public const string SignupScope = "signup";

        private const string KeyPrefix = "auth:";

        // Per-process random key: the partition digest is stable for the lifetime of the
        // process (all a rate limiter needs — partitions are in-memory and lost on
        // restart anyway) but is not offline-reversible if a digest ever escapes to a
        // less-privileged sink such as a metrics tag.
        private static readonly byte[] DigestKey = RandomNumberGenerator.GetBytes(32);

        /// <summary>The one bounded partition for requests whose identity cannot be extracted, per scope.</summary>
        public static string FallbackKey(string scope) => KeyPrefix + scope + ":fallback";

        /// <summary>
        /// <c>auth:&lt;scope&gt;:&lt;hex HMAC-SHA-256 digest of the exact email&gt;</c> for a
        /// present identity, or <see cref="FallbackKey"/> for a missing (null / empty)
        /// one. Deterministic within the process; the identical email string always
        /// maps to the same key, and any difference in the string (case, whitespace,
        /// encoding) maps to a different key — mirroring the production email lookup.
        /// </summary>
        public static string PartitionKey(string scope, string? exactEmail)
        {
            return string.IsNullOrEmpty(exactEmail)
                ? FallbackKey(scope)
                : KeyPrefix + scope + ":" + Digest(scope, exactEmail);
        }

        private static string Digest(string scope, string exactEmail)
        {
            // scope is in the MAC input as well as the key prefix so a digest can
            // never collide across login/signup even if a prefix were dropped.
            var bytes = Encoding.UTF8.GetBytes(scope + "\n" + exactEmail);
            return Convert.ToHexString(HMACSHA256.HashData(DigestKey, bytes)).ToLowerInvariant();
        }
    }
}
