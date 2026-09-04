using System;
using System.Threading;
using Docker.DotNet;
using Xunit;

namespace GoHardAPI.Tests.Infrastructure
{
    /// <summary>
    /// <see cref="FactAttribute"/> for the real-PostgreSQL suites.
    ///
    /// <para>Local dev (<c>REQUIRE_POSTGRES_TESTS</c> unset): self-skips rather than fails
    /// when no Docker daemon is reachable, so <c>dotnet test</c> stays usable offline.</para>
    ///
    /// <para>CI (<c>REQUIRE_POSTGRES_TESTS=true</c>, see
    /// <see cref="PostgresRequirement"/>): never self-skips. If Docker / the container is
    /// unavailable the test runs and fails hard (the fixture rethrows, and each test also
    /// asserts <c>fixture.Available</c>).</para>
    ///
    /// When Docker IS present these run for real — they are the only concurrency /
    /// real-provider evidence for keyed Session CREATE; SQLite and EF InMemory are never
    /// cited as proof.
    /// </summary>
    public sealed class DockerRequiredFactAttribute : FactAttribute
    {
        private static readonly Lazy<string?> SkipReason = new(Probe, LazyThreadSafetyMode.ExecutionAndPublication);

        public DockerRequiredFactAttribute()
        {
            if (SkipReason.Value is { } reason && !PostgresRequirement.IsRequired)
            {
                Skip = reason;
            }
        }

        private static string? Probe()
        {
            try
            {
                using var client = new DockerClientConfiguration().CreateClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                client.System.PingAsync(cts.Token).GetAwaiter().GetResult();
                return null;
            }
            catch (Exception ex)
            {
                return $"Docker daemon not reachable — skipping real-PostgreSQL test ({ex.GetType().Name}: {ex.Message})";
            }
        }
    }
}
