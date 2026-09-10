using System;
using System.IO;
using GoHardAPI.Configuration;
using GoHardAPI.Services;

namespace GoHardAPI.Tests.Infrastructure
{
    /// <summary>
    /// Helpers for constructing <see cref="FileUploadService"/> in tests against
    /// a real, isolated temp directory (deleted best-effort on dispose).
    /// </summary>
    public sealed class TestProfilePhotoStorage : IDisposable
    {
        public string Directory { get; }
        public ProfilePhotoStorageOptions Options { get; }
        public FileUploadService Service { get; }

        public TestProfilePhotoStorage()
        {
            Directory = Path.Combine(
                Path.GetTempPath(), "gohardapi-tests", Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(Directory);
            Options = new ProfilePhotoStorageOptions { Directory = Directory };
            Service = new FileUploadService(Options);
        }

        /// <summary>A second service instance over the SAME directory - stands in
        /// for a fresh application instance sharing the persisted volume.</summary>
        public FileUploadService NewIndependentService() => new(Options);

        public string[] Files() =>
            System.IO.Directory.Exists(Directory)
                ? System.IO.Directory.GetFiles(Directory)
                : Array.Empty<string>();

        /// <summary>
        /// A throwaway <see cref="FileUploadService"/> whose directory exists but
        /// is otherwise unused - for controllers under test that never touch
        /// photos.
        /// </summary>
        public static FileUploadService UnusedService() =>
            new(new ProfilePhotoStorageOptions { Directory = Path.GetTempPath() });

        public void Dispose()
        {
            try
            {
                if (System.IO.Directory.Exists(Directory))
                {
                    System.IO.Directory.Delete(Directory, recursive: true);
                }
            }
            catch
            {
                // best effort
            }
        }
    }
}
