using System.Text.RegularExpressions;
using GoHardAPI.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace GoHardAPI.Services
{
    /// <summary>
    /// A profile photo the caller has just written to disk.
    /// </summary>
    /// <param name="RelativeUrl">
    /// Public reference to persist in <c>User.ProfilePhotoUrl</c> and return to
    /// the client, e.g. <c>/uploads/profiles/user_5_ab12...cd.jpg</c>.
    /// </param>
    /// <param name="AbsolutePath">Full path on disk (never sent to a client).</param>
    public readonly record struct SavedProfilePhoto(string RelativeUrl, string AbsolutePath);

    /// <summary>
    /// Stores and removes profile-photo files under
    /// <see cref="ProfilePhotoStorageOptions.Directory"/> (a configurable
    /// directory outside the publish output - see that type). The public URL and
    /// multipart contracts are unchanged: files are served under
    /// <see cref="ProfilePhotoStorageOptions.PublicPathPrefix"/>, uploads arrive
    /// as an <see cref="IFormFile"/>.
    ///
    /// <para><b>Write path</b> (<see cref="SaveNewAsync"/>): validate first (size,
    /// then a real content-signature check - the client-supplied filename and
    /// extension are never trusted), then write to a FRESH, GUID-named file with
    /// <see cref="FileMode.CreateNew"/> (exclusive create - two same-second
    /// uploads cannot collide or overwrite), flush and close it, and only then
    /// return its reference. Nothing existing is touched.</para>
    ///
    /// <para><b>Delete path</b> (<see cref="TryDeleteByRelativeUrl"/>): best-effort,
    /// never throws, and only ever removes a file that (a) sits directly inside
    /// the configured directory and (b) matches this service's own
    /// <c>user_{id}_{guid}.{ext}</c> naming - it cannot be pointed at an
    /// arbitrary path.</para>
    /// </summary>
    public sealed class FileUploadService
    {
        /// <summary>
        /// Files this service is allowed to delete: its own output only.
        /// </summary>
        private static readonly Regex OwnedFileName = new(
            @"^user_[0-9]+_[0-9a-fA-F]{32}\.(jpg|jpeg|png)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly ProfilePhotoStorageOptions _options;
        private readonly ILogger<FileUploadService> _logger;

        /// <summary>
        /// Test seam: when set, replaces the real <see cref="File.Delete"/> call
        /// in <see cref="TryDeleteByRelativeUrl"/> so a cleanup failure can be
        /// exercised portably (POSIX <c>unlink</c> ignores open handles, so a
        /// file lock cannot simulate this on Linux). Always <c>null</c> in
        /// production.
        /// </summary>
        internal Action<string>? DeleteFileForTests { get; set; }

        public FileUploadService(
            ProfilePhotoStorageOptions options,
            ILogger<FileUploadService>? logger = null)
        {
            _options = options;
            _logger = logger ?? NullLogger<FileUploadService>.Instance;
        }

        /// <summary>
        /// Validate <paramref name="file"/> and write it to a new file. Throws
        /// <see cref="PhotoValidationException"/> (safe message) if the file is
        /// missing, too large, or not a JPEG/PNG by content. On success the file
        /// is fully written and closed before this returns; the caller then
        /// persists <see cref="SavedProfilePhoto.RelativeUrl"/> and, only after
        /// that commit, removes any previous file.
        /// </summary>
        public async Task<SavedProfilePhoto> SaveNewAsync(
            int userId, IFormFile? file, CancellationToken cancellationToken = default)
        {
            var extension = await ValidateAndDetectExtensionAsync(file, cancellationToken);

            Directory.CreateDirectory(_options.Directory);

            // Exclusive create on a random name. A GUID collision is astronomically
            // unlikely; retry a handful of times purely as a belt-and-braces guard.
            for (var attempt = 0; ; attempt++)
            {
                var fileName = $"user_{userId}_{Guid.NewGuid():N}{extension}";
                var absolutePath = Path.Combine(_options.Directory, fileName);
                try
                {
                    await using (var destination = new FileStream(
                        absolutePath,
                        FileMode.CreateNew,          // fails if the name already exists
                        FileAccess.Write,
                        FileShare.None,              // no concurrent writer/reader while we fill it
                        bufferSize: 81920,
                        useAsync: true))
                    {
                        await using var source = file!.OpenReadStream();
                        await source.CopyToAsync(destination, cancellationToken);
                        await destination.FlushAsync(cancellationToken);
                    } // disposed here => flushed to OS and closed before we publish the reference

                    return new SavedProfilePhoto(
                        $"{ProfilePhotoStorageOptions.PublicPathPrefix}/{fileName}", absolutePath);
                }
                catch (IOException) when (attempt < 5 && File.Exists(absolutePath))
                {
                    // Name already taken - try another GUID.
                }
            }
        }

        /// <summary>
        /// Best-effort removal of a previously stored photo. Returns <c>true</c>
        /// only if a file was actually deleted. NEVER throws - a cleanup failure
        /// must not fail an already-committed replacement or removal. Refuses any
        /// path that is not one of this service's own files inside the configured
        /// directory (no traversal, no foreign files).
        /// </summary>
        public bool TryDeleteByRelativeUrl(string? relativeUrl)
        {
            var absolutePath = ResolveOwnedPath(relativeUrl);
            if (absolutePath is null)
            {
                return false;
            }

            try
            {
                if (File.Exists(absolutePath))
                {
                    if (DeleteFileForTests is { } hook)
                    {
                        hook(absolutePath);
                    }
                    else
                    {
                        File.Delete(absolutePath);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                // Log the failure class only - never the path contents or bytes.
                _logger.LogWarning(
                    "Profile photo cleanup failed ({ExceptionType}); leaving a recoverable orphan file.",
                    ex.GetType().Name);
            }

            return false;
        }

        /// <summary>
        /// Map a stored <c>ProfilePhotoUrl</c> to an absolute path IFF it is one
        /// of this service's own files directly inside the configured directory.
        /// Returns <c>null</c> for anything else (empty, foreign prefix,
        /// traversal, unexpected name). Exposed for tests and for a fresh
        /// instance resolving files another instance wrote to the same directory.
        /// </summary>
        public string? ResolveOwnedPath(string? relativeUrl) => ResolveOwnedPathInternal(relativeUrl);

        private string? ResolveOwnedPathInternal(string? relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl))
            {
                return null;
            }

            // Must be under our fixed public prefix.
            var prefix = ProfilePhotoStorageOptions.PublicPathPrefix + "/";
            if (!relativeUrl.StartsWith(prefix, StringComparison.Ordinal))
            {
                return null;
            }

            // Only the last path segment is trusted, and only if it matches our
            // own naming - defeats "/uploads/profiles/../../secret" and foreign
            // files that happen to live in the same folder.
            var fileName = relativeUrl[prefix.Length..];
            if (fileName.Length == 0
                || fileName != Path.GetFileName(fileName)
                || !OwnedFileName.IsMatch(fileName))
            {
                return null;
            }

            var root = Path.GetFullPath(_options.Directory);
            var candidate = Path.GetFullPath(Path.Combine(root, fileName));

            // Final containment check (defence in depth).
            var rootWithSep = root.EndsWith(Path.DirectorySeparatorChar)
                ? root
                : root + Path.DirectorySeparatorChar;
            return candidate.StartsWith(rootWithSep, StringComparison.Ordinal) ? candidate : null;
        }

        private async Task<string> ValidateAndDetectExtensionAsync(
            IFormFile? file, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
            {
                throw new PhotoValidationException("No image was uploaded.");
            }

            if (file.Length > _options.MaxBytes)
            {
                throw new PhotoValidationException(
                    $"Image is too large. The maximum size is {_options.MaxBytes / (1024 * 1024)} MB.");
            }

            // The client-supplied filename and its extension are NOT trusted for
            // anything - the content signature alone decides the format and the
            // stored extension.
            var header = new byte[12];
            int read;
            await using (var probe = file.OpenReadStream())
            {
                read = await ReadAtLeastAsync(probe, header.AsMemory(), cancellationToken);
            }

            return DetectExtension(header.AsSpan(0, read)) ?? throw UnsupportedFormat();
        }

        /// <summary>
        /// Returns <c>.jpg</c> / <c>.png</c> for a recognised signature, a
        /// dedicated message for HEIC/HEIF (the one iOS format that is never
        /// renderable in a browser), or <c>null</c> for anything else.
        /// </summary>
        private static string? DetectExtension(ReadOnlySpan<byte> header)
        {
            // JPEG: FF D8 FF
            if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            {
                return ".jpg";
            }

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            ReadOnlySpan<byte> png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            if (header.Length >= 8 && header[..8].SequenceEqual(png))
            {
                return ".png";
            }

            // ISO-BMFF "ftyp" box (bytes 4..8) with a HEIF/HEIC brand.
            if (header.Length >= 12
                && header[4] == (byte)'f' && header[5] == (byte)'t'
                && header[6] == (byte)'y' && header[7] == (byte)'p')
            {
                var brand = System.Text.Encoding.ASCII.GetString(header[8..12]);
                if (brand is "heic" or "heix" or "hevc" or "hevx" or "mif1" or "msf1" or "heim" or "heis")
                {
                    throw new PhotoValidationException(
                        "HEIC images aren't supported yet. Please choose a JPEG or PNG photo.");
                }
            }

            return null;
        }

        private static PhotoValidationException UnsupportedFormat() => new(
            "Unsupported image format. Please upload a JPEG or PNG photo.");

        private static async Task<int> ReadAtLeastAsync(
            Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var total = 0;
            while (total < buffer.Length)
            {
                var n = await stream.ReadAsync(buffer[total..], cancellationToken);
                if (n == 0)
                {
                    break;
                }
                total += n;
            }
            return total;
        }
    }
}
