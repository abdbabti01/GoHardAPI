using System;
using System.IO;
using GoHardAPI.Configuration;
using Xunit;

namespace GoHardAPI.Tests.Services
{
    public sealed class ProfilePhotoStorageResolverTests
    {
        [Fact]
        public void env_var_wins_and_is_rooted()
        {
            var dir = ProfilePhotoStorageResolver.Resolve(
                envValue: "/data/photos", configValue: "/other", isProduction: true, out var fell);

            Assert.False(fell);
            Assert.Equal(Path.GetFullPath("/data/photos"), dir);
        }

        [Fact]
        public void config_value_is_used_when_no_env_var()
        {
            var dir = ProfilePhotoStorageResolver.Resolve(
                envValue: null, configValue: "/cfg/photos", isProduction: true, out var fell);

            Assert.False(fell);
            Assert.Equal(Path.GetFullPath("/cfg/photos"), dir);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "   ")]
        public void production_with_no_configuration_refuses_to_start(string? env, string? cfg)
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                ProfilePhotoStorageResolver.Resolve(env, cfg, isProduction: true, out _));

            Assert.Contains(ProfilePhotoStorageOptions.EnvironmentVariable, ex.Message);
            Assert.Contains("Refusing to start", ex.Message);
        }

        [Fact]
        public void non_production_with_no_configuration_uses_a_temp_fallback_and_flags_it()
        {
            var dir = ProfilePhotoStorageResolver.Resolve(
                envValue: null, configValue: null, isProduction: false, out var fell);

            Assert.True(fell);
            Assert.StartsWith(Path.GetFullPath(Path.GetTempPath()), dir);
            Assert.DoesNotContain("publish", dir, StringComparison.OrdinalIgnoreCase);
        }
    }
}
