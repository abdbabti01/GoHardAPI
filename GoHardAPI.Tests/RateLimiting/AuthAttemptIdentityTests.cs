using System;
using GoHardAPI.RateLimiting;
using Xunit;

namespace GoHardAPI.Tests.RateLimiting
{
    /// <summary>
    /// Partition-key derivation for auth attempts. Identity equivalence must match the
    /// production email lookup exactly (case-sensitive, whitespace-significant — see
    /// <see cref="AuthEmailLookupPostgresTests"/>), so only the byte-identical email
    /// string shares a bucket. No pipeline, no sleeps.
    /// </summary>
    public class AuthAttemptIdentityTests
    {
        private const string Login = AuthAttemptIdentity.LoginScope;
        private const string Signup = AuthAttemptIdentity.SignupScope;

        [Fact]
        public void ExactRepeatedEmailText_MapsToOneKey()
        {
            var first = AuthAttemptIdentity.PartitionKey(Login, "user@example.com");
            for (var i = 0; i < 10; i++)
            {
                Assert.Equal(first, AuthAttemptIdentity.PartitionKey(Login, "user@example.com"));
            }
        }

        [Theory] // production lookup is WHERE "Email" = @p on a case-sensitive column -> distinct identities
        [InlineData("user@example.com", "User@example.com")]
        [InlineData("user@example.com", "USER@EXAMPLE.COM")]
        [InlineData("user@example.com", "user@Example.com")]
        public void CaseDifferentEmails_MapToDifferentKeys(string a, string b)
        {
            Assert.NotEqual(
                AuthAttemptIdentity.PartitionKey(Login, a),
                AuthAttemptIdentity.PartitionKey(Login, b));
        }

        [Theory] // varchar "=" is whitespace-significant -> distinct identities
        [InlineData("user@example.com", " user@example.com")]
        [InlineData("user@example.com", "user@example.com ")]
        [InlineData("user@example.com", "\tuser@example.com")]
        public void WhitespaceDifferentEmails_MapToDifferentKeys(string a, string b)
        {
            Assert.NotEqual(
                AuthAttemptIdentity.PartitionKey(Login, a),
                AuthAttemptIdentity.PartitionKey(Login, b));
        }

        [Fact] // no Unicode normalization: precomposed vs decomposed are different byte strings -> different rows -> different keys
        public void UnicodeCanonicalEquivalentEmails_MapToDifferentKeys()
        {
            const string precomposed = "josé@example.com";        // é as U+00E9
            const string decomposed = "josé@example.com";        // e + combining acute U+0301
            Assert.NotEqual(precomposed, decomposed);                  // sanity: distinct strings

            Assert.NotEqual(
                AuthAttemptIdentity.PartitionKey(Login, precomposed),
                AuthAttemptIdentity.PartitionKey(Login, decomposed));
        }

        [Fact]
        public void DifferentIdentities_MapToDifferentKeys()
        {
            Assert.NotEqual(
                AuthAttemptIdentity.PartitionKey(Login, "a@example.com"),
                AuthAttemptIdentity.PartitionKey(Login, "b@example.com"));
        }

        [Fact]
        public void LoginAndSignup_ForTheSameIdentity_AreDifferentPartitions()
        {
            Assert.NotEqual(
                AuthAttemptIdentity.PartitionKey(Login, "same@example.com"),
                AuthAttemptIdentity.PartitionKey(Signup, "same@example.com"));
        }

        [Fact]
        public void Key_NeverContainsTheRawIdentity()
        {
            const string raw = "SecretUser@Private-Domain.example";
            var key = AuthAttemptIdentity.PartitionKey(Login, raw);
            Assert.DoesNotContain("secretuser", key, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("private-domain", key, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("@", key, StringComparison.Ordinal);
            Assert.DoesNotContain(raw, key, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Key_IsScopePrefixPlusLowerHexHmac()
        {
            var key = AuthAttemptIdentity.PartitionKey(Login, "user@example.com");
            Assert.StartsWith("auth:login:", key, StringComparison.Ordinal);
            var digest = key["auth:login:".Length..];
            Assert.Equal(64, digest.Length);
            Assert.Matches("^[0-9a-f]{64}$", digest);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void MissingIdentity_UsesTheOneBoundedScopeFallback(string? raw)
        {
            Assert.Equal(AuthAttemptIdentity.FallbackKey(Login), AuthAttemptIdentity.PartitionKey(Login, raw));
        }

        [Fact]
        public void Fallback_IsRouteSpecific_Deterministic_AndNotRawText()
        {
            var loginFb = AuthAttemptIdentity.FallbackKey(Login);
            var signupFb = AuthAttemptIdentity.FallbackKey(Signup);

            Assert.NotEqual(loginFb, signupFb);
            Assert.Equal(loginFb, AuthAttemptIdentity.FallbackKey(Login)); // deterministic
            Assert.StartsWith("auth:login:", loginFb, StringComparison.Ordinal);
            Assert.StartsWith("auth:signup:", signupFb, StringComparison.Ordinal);
        }
    }
}
