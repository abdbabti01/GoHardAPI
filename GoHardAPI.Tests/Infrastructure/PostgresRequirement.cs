using System;

namespace GoHardAPI.Tests.Infrastructure
{
    /// <summary>
    /// Central switch for "the PostgreSQL integration tests are mandatory here".
    ///
    /// CI sets <c>REQUIRE_POSTGRES_TESTS=true</c> (see <c>.github/workflows/dotnet-ci.yml</c>).
    /// When required:
    /// <list type="bullet">
    ///   <item><see cref="DockerRequiredFactAttribute"/> does NOT self-skip — the test runs
    ///     and fails loudly if Docker / the container is unavailable;</item>
    ///   <item>the PostgreSQL fixtures rethrow a container-start failure instead of
    ///     degrading to <c>Available = false</c>.</item>
    /// </list>
    /// When the variable is absent or not truthy (a local dev box without Docker) the old
    /// self-skip behavior is kept so <c>dotnet test</c> stays usable offline.
    /// </summary>
    public static class PostgresRequirement
    {
        public const string EnvVar = "REQUIRE_POSTGRES_TESTS";

        public static bool IsRequired
        {
            get
            {
                var value = Environment.GetEnvironmentVariable(EnvVar);
                return value is not null
                    && (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1");
            }
        }

        /// <summary>
        /// Call from a fixture after a failed container start. Throws when PostgreSQL is
        /// required (CI); otherwise returns so the caller can degrade to
        /// <c>Available = false</c> for a local offline run.
        /// </summary>
        public static void ThrowIfRequired(Exception? cause)
        {
            if (!IsRequired)
            {
                return;
            }

            throw new InvalidOperationException(
                $"{EnvVar}=true but a PostgreSQL test container could not start. In CI this is " +
                "a hard failure: Docker must be reachable and the keyed-Session-CREATE " +
                "PostgreSQL suites must execute (zero executed / skipped is not allowed).",
                cause);
        }
    }
}
