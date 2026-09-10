using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GoHardAPI.Configuration;
using GoHardAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace GoHardAPI.Tests.Services
{
    /// <summary>
    /// Serves the configured directory exactly as <c>Program.cs</c> does (shared
    /// <see cref="ProfilePhotoStaticFiles.BuildOptions"/>) through a real HTTP
    /// pipeline, and proves a fresh instance over the same directory serves the
    /// same bytes.
    /// </summary>
    public sealed class ProfilePhotoServingTests : IDisposable
    {
        private readonly TestProfilePhotoStorage _storage = new();

        public void Dispose() => _storage.Dispose();

        private static IHost BuildHost(string directory) =>
            new HostBuilder()
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .Configure(app => app.UseStaticFiles(ProfilePhotoStaticFiles.BuildOptions(directory))))
                .Build();

        private static readonly byte[] Jpeg =
            { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01 };

        [Fact]
        public async Task serves_a_stored_jpeg_with_the_image_content_type()
        {
            var name = $"user_5_{Guid.NewGuid():N}.jpg";
            await File.WriteAllBytesAsync(Path.Combine(_storage.Directory, name), Jpeg);

            using var host = BuildHost(_storage.Directory);
            await host.StartAsync();
            var resp = await host.GetTestClient().GetAsync($"/uploads/profiles/{name}");

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            Assert.Equal("image/jpeg", resp.Content.Headers.ContentType!.MediaType);
            Assert.Equal(Jpeg, await resp.Content.ReadAsByteArrayAsync());
            Assert.Equal("nosniff", resp.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.Contains("max-age=86400", resp.Headers.CacheControl!.ToString());
            Assert.Equal("default-src 'none'; sandbox", resp.Headers.GetValues("Content-Security-Policy").Single());
        }

        [Fact]
        public async Task does_not_serve_a_non_image_file_in_the_same_directory()
        {
            await File.WriteAllTextAsync(Path.Combine(_storage.Directory, "secret.txt"), "top secret");

            using var host = BuildHost(_storage.Directory);
            await host.StartAsync();
            var resp = await host.GetTestClient().GetAsync("/uploads/profiles/secret.txt");

            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        [Theory]
        [InlineData("/uploads/profiles/..%2f..%2fappsettings.json")]
        [InlineData("/uploads/profiles/%2e%2e/%2e%2e/Program.cs")]
        public async Task path_traversal_out_of_the_directory_is_refused(string path)
        {
            using var host = BuildHost(_storage.Directory);
            await host.StartAsync();
            var resp = await host.GetTestClient().GetAsync(path);

            Assert.NotEqual(HttpStatusCode.OK, resp.StatusCode);
        }

        [Fact]
        public async Task the_configured_directory_wins_over_the_wwwroot_fallback_which_still_serves_legacy_files()
        {
            // Mirror Program.cs: profile-photo middleware first, then a plain
            // wwwroot-style UseStaticFiles as the fallback.
            var wwwroot = Path.Combine(Path.GetTempPath(), "gohardapi-tests-www", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(wwwroot, "uploads", "profiles"));
            try
            {
                var legacyName = "user_13_20260107192757.jpg";
                await File.WriteAllBytesAsync(Path.Combine(wwwroot, "uploads", "profiles", legacyName), new byte[] { 1, 1 });

                var freshName = $"user_9_{Guid.NewGuid():N}.jpg";
                await File.WriteAllBytesAsync(Path.Combine(_storage.Directory, freshName), Jpeg);

                using var host = new HostBuilder()
                    .ConfigureWebHost(web => web
                        .UseTestServer()
                        .Configure(app =>
                        {
                            app.UseStaticFiles(ProfilePhotoStaticFiles.BuildOptions(_storage.Directory));
                            app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions
                            {
                                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(wwwroot),
                            });
                        }))
                    .Build();
                await host.StartAsync();
                var client = host.GetTestClient();

                var fresh = await client.GetAsync($"/uploads/profiles/{freshName}");
                Assert.Equal(HttpStatusCode.OK, fresh.StatusCode);
                Assert.Equal(Jpeg, await fresh.Content.ReadAsByteArrayAsync());

                var legacy = await client.GetAsync($"/uploads/profiles/{legacyName}");
                Assert.Equal(HttpStatusCode.OK, legacy.StatusCode); // served by the wwwroot fallback
            }
            finally
            {
                try { Directory.Delete(wwwroot, recursive: true); } catch { }
            }
        }

        [Fact]
        public async Task a_new_application_instance_serves_the_same_persisted_directory()
        {
            var name = $"user_5_{Guid.NewGuid():N}.jpg";
            var path = Path.Combine(_storage.Directory, name);
            await File.WriteAllBytesAsync(path, Jpeg);

            // "instance 1" writes, "instance 2" (fresh host, same directory) reads.
            using (var writer = BuildHost(_storage.Directory))
            {
                await writer.StartAsync();
            }

            using var reader = BuildHost(_storage.Directory);
            await reader.StartAsync();
            var resp = await reader.GetTestClient().GetAsync($"/uploads/profiles/{name}");

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            Assert.Equal(Jpeg, await resp.Content.ReadAsByteArrayAsync());
        }
    }
}
