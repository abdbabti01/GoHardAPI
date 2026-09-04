using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace GoHardAPI.Tests.Infrastructure
{
    /// <summary>
    /// Mutation guard for the CI split. CI runs the PostgreSQL suites with
    /// <c>--filter "Category=PostgresIntegration"</c> and then fails the job if zero of
    /// them executed. That guarantee only holds if every real-PostgreSQL test class is
    /// tagged. This test — itself NOT a PostgreSQL test, so it always runs — fails if any
    /// class that declares <c>[DockerRequiredFact]</c> methods is missing the class-level
    /// <c>[Trait("Category", "PostgresIntegration")]</c>, which would let those tests slip
    /// out of the mandatory filter unnoticed.
    ///
    /// xUnit v2's <see cref="TraitAttribute"/> exposes no public Name/Value, so the trait
    /// pair is read from the attribute's constructor arguments via
    /// <see cref="MemberInfo.GetCustomAttributesData"/> — the same data the xUnit trait
    /// discoverer uses.
    /// </summary>
    public class PostgresIntegrationTaggingTests
    {
        private const string Category = "Category";
        private const string PostgresIntegration = "PostgresIntegration";

        private static bool HasPostgresIntegrationTrait(System.Type type) =>
            type.GetCustomAttributesData().Any(a =>
                a.AttributeType == typeof(TraitAttribute)
                && a.ConstructorArguments.Count == 2
                && (string?)a.ConstructorArguments[0].Value == Category
                && (string?)a.ConstructorArguments[1].Value == PostgresIntegration);

        [Fact]
        public void EveryClassWithDockerRequiredFacts_IsTaggedPostgresIntegration()
        {
            var assembly = typeof(DockerRequiredFactAttribute).Assembly;
            var offenders = new List<string>();

            foreach (var type in assembly.GetTypes())
            {
                var hasDockerFacts = type
                    .GetMethods()
                    .Any(m => m.GetCustomAttributes(typeof(DockerRequiredFactAttribute), inherit: false).Any());

                if (!hasDockerFacts)
                {
                    continue;
                }

                if (!HasPostgresIntegrationTrait(type))
                {
                    offenders.Add(
                        $"{type.FullName} has [DockerRequiredFact] tests but no class-level " +
                        $"[Trait(\"{Category}\", \"{PostgresIntegration}\")] - CI's mandatory filter would miss it.");
                }
            }

            Assert.True(offenders.Count == 0, string.Join("\n", offenders));
        }

        [Fact]
        public void AtLeastOnePostgresIntegrationClassExists()
        {
            var assembly = typeof(DockerRequiredFactAttribute).Assembly;
            var count = assembly.GetTypes().Count(HasPostgresIntegrationTrait);

            Assert.True(count >= 1,
                "No test class is tagged [Trait(\"Category\", \"PostgresIntegration\")]; CI would run zero mandatory PostgreSQL tests.");
        }
    }
}
