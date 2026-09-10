using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoHardAPI.Configuration;
using GoHardAPI.Services;
using GoHardAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace GoHardAPI.Tests.Services
{
    /// <summary>
    /// Unit behaviour of <see cref="FileUploadService"/> against a real temp
    /// directory: content-signature validation, exclusive collision-free writes,
    /// and sandboxed deletion.
    /// </summary>
    public sealed class FileUploadServiceTests : IDisposable
    {
        private static readonly byte[] JpegBytes =
            new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }.Concat(new byte[64]).ToArray();

        private static readonly byte[] PngBytes =
            new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.Concat(new byte[64]).ToArray();

        private static readonly byte[] HeicBytes = BuildHeic();

        private readonly TestProfilePhotoStorage _storage = new();

        public void Dispose() => _storage.Dispose();

        private static byte[] BuildHeic()
        {
            var b = new byte[32];
            b[4] = (byte)'f'; b[5] = (byte)'t'; b[6] = (byte)'y'; b[7] = (byte)'p';
            b[8] = (byte)'h'; b[9] = (byte)'e'; b[10] = (byte)'i'; b[11] = (byte)'c';
            return b;
        }

        private static IFormFile FormFile(byte[] content, string fileName, string contentType = "application/octet-stream")
        {
            var stream = new MemoryStream(content);
            return new FormFile(stream, 0, content.Length, "photo", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType,
            };
        }

        [Fact]
        public async Task saves_a_jpeg_to_a_closed_file_under_the_public_prefix()
        {
            var saved = await _storage.Service.SaveNewAsync(7, FormFile(JpegBytes, "whatever.jpg"));

            Assert.StartsWith("/uploads/profiles/user_7_", saved.RelativeUrl);
            Assert.EndsWith(".jpg", saved.RelativeUrl);
            Assert.True(File.Exists(saved.AbsolutePath));
            // File is closed - we can open it exclusively.
            await using (var _ = new FileStream(saved.AbsolutePath, FileMode.Open, FileAccess.Read, FileShare.None)) { }
            Assert.Equal(JpegBytes, await File.ReadAllBytesAsync(saved.AbsolutePath));
        }

        [Fact]
        public async Task detects_png_by_signature_regardless_of_declared_name()
        {
            var saved = await _storage.Service.SaveNewAsync(7, FormFile(PngBytes, "photo.jpg"));
            Assert.EndsWith(".png", saved.RelativeUrl);
        }

        [Fact]
        public async Task rejects_a_non_image_payload_with_a_jpg_name()
        {
            var ex = await Assert.ThrowsAsync<PhotoValidationException>(() =>
                _storage.Service.SaveNewAsync(7, FormFile(Encoding.ASCII.GetBytes("not an image at all"), "sneaky.jpg")));
            Assert.Contains("Unsupported image format", ex.Message);
            Assert.Empty(_storage.Files());
        }

        [Fact]
        public async Task stores_by_content_not_by_a_lying_client_extension()
        {
            // JPEG bytes with a ".gif" name -> it is a real JPEG, stored as .jpg.
            var saved = await _storage.Service.SaveNewAsync(7, FormFile(JpegBytes, "photo.gif"));
            Assert.EndsWith(".jpg", saved.RelativeUrl);
        }

        [Fact]
        public async Task rejects_a_real_but_unsupported_image_format()
        {
            var gif = Encoding.ASCII.GetBytes("GIF89a").Concat(new byte[32]).ToArray();
            var ex = await Assert.ThrowsAsync<PhotoValidationException>(() =>
                _storage.Service.SaveNewAsync(7, FormFile(gif, "anim.gif")));
            Assert.Contains("JPEG or PNG", ex.Message);
        }

        [Fact]
        public async Task rejects_an_svg()
        {
            var svg = Encoding.ASCII.GetBytes("<?xml version=\"1.0\"?><svg xmlns=\"http://www.w3.org/2000/svg\"><script/></svg>");
            var ex = await Assert.ThrowsAsync<PhotoValidationException>(() =>
                _storage.Service.SaveNewAsync(7, FormFile(svg, "x.svg")));
            Assert.Contains("JPEG or PNG", ex.Message);
        }

        [Fact]
        public async Task a_traversal_style_multipart_filename_is_ignored_and_the_stored_name_is_server_generated()
        {
            var saved = await _storage.Service.SaveNewAsync(
                7, FormFile(JpegBytes, "../../../etc/passwd.jpg"));

            var name = Path.GetFileName(saved.AbsolutePath);
            Assert.Matches(@"^user_7_[0-9a-f]{32}\.jpg$", name);
            Assert.Equal(_storage.Directory, Path.GetDirectoryName(saved.AbsolutePath));
            Assert.Single(_storage.Files());
        }

        [Fact]
        public async Task a_mismatched_multipart_content_type_does_not_change_the_stored_extension()
        {
            var saved = await _storage.Service.SaveNewAsync(
                7, FormFile(JpegBytes, "photo", contentType: "text/html"));

            Assert.EndsWith(".jpg", saved.RelativeUrl); // decided by bytes, not the header
        }

        [Fact]
        public async Task rejects_heic_with_a_specific_message()
        {
            var ex = await Assert.ThrowsAsync<PhotoValidationException>(() =>
                _storage.Service.SaveNewAsync(7, FormFile(HeicBytes, "IMG_0001.heic")));
            Assert.Contains("HEIC", ex.Message);
        }

        [Fact]
        public async Task rejects_an_empty_or_oversize_file()
        {
            await Assert.ThrowsAsync<PhotoValidationException>(() =>
                _storage.Service.SaveNewAsync(7, FormFile(Array.Empty<byte>(), "e.jpg")));

            var big = new byte[] { 0xFF, 0xD8, 0xFF }.Concat(new byte[_storage.Options.MaxBytes]).ToArray();
            var ex = await Assert.ThrowsAsync<PhotoValidationException>(() =>
                _storage.Service.SaveNewAsync(7, FormFile(big, "big.jpg")));
            Assert.Contains("too large", ex.Message);
        }

        [Fact]
        public async Task same_second_uploads_for_one_user_never_collide()
        {
            var saved = new System.Collections.Concurrent.ConcurrentBag<string>();
            await Task.WhenAll(Enumerable.Range(0, 60).Select(async _ =>
            {
                var s = await _storage.Service.SaveNewAsync(7, FormFile(JpegBytes, "p.jpg"));
                saved.Add(s.AbsolutePath);
            }));

            Assert.Equal(60, saved.Distinct().Count());
            Assert.All(saved, p => Assert.True(File.Exists(p)));
            Assert.Equal(60, _storage.Files().Length);
        }

        [Theory]
        [InlineData("/uploads/profiles/../../Program.cs")]
        [InlineData("/uploads/profiles/..%2f..%2fsecret")]
        [InlineData("/etc/passwd")]
        [InlineData("/uploads/other/user_7_00000000000000000000000000000000.jpg")]
        [InlineData("/uploads/profiles/not-our-naming.jpg")]
        [InlineData("/uploads/profiles/")]
        [InlineData("")]
        [InlineData(null)]
        public void resolve_owned_path_rejects_anything_that_is_not_our_own_file(string? url)
        {
            Assert.Null(_storage.Service.ResolveOwnedPath(url));
            Assert.False(_storage.Service.TryDeleteByRelativeUrl(url));
        }

        [Fact]
        public async Task try_delete_removes_our_own_file_and_is_a_safe_noop_when_missing()
        {
            var saved = await _storage.Service.SaveNewAsync(7, FormFile(JpegBytes, "p.jpg"));

            Assert.True(_storage.Service.TryDeleteByRelativeUrl(saved.RelativeUrl));
            Assert.False(File.Exists(saved.AbsolutePath));
            // Second call: nothing to delete, must not throw, returns false.
            Assert.False(_storage.Service.TryDeleteByRelativeUrl(saved.RelativeUrl));
        }

        [Fact]
        public async Task a_fresh_service_instance_resolves_files_written_by_another_instance()
        {
            var saved = await _storage.Service.SaveNewAsync(7, FormFile(JpegBytes, "p.jpg"));

            var independent = _storage.NewIndependentService();
            var resolved = independent.ResolveOwnedPath(saved.RelativeUrl);

            Assert.NotNull(resolved);
            Assert.Equal(saved.AbsolutePath, resolved);
            Assert.True(File.Exists(resolved));
        }
    }
}
