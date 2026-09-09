namespace GoHardAPI.Configuration
{
    /// <summary>
    /// Resolves the physical profile-photo directory at startup. Kept separate
    /// from <c>Program.cs</c> so the "Production never silently falls back to
    /// ephemeral storage" rule is directly testable.
    /// </summary>
    public static class ProfilePhotoStorageResolver
    {
        /// <summary>
        /// Returns the directory to use. Precedence: <paramref name="envValue"/>,
        /// then <paramref name="configValue"/>, then (non-Production only) a
        /// stable temp path. Throws <see cref="InvalidOperationException"/> when
        /// <paramref name="isProduction"/> is <c>true</c> and neither source is
        /// set - the app must not start against ephemeral storage.
        /// The result is <see cref="System.IO.Path.GetFullPath(string)"/>-rooted;
        /// callers still create the directory and must separately verify a
        /// persistent volume is actually mounted there.
        /// </summary>
        public static string Resolve(
            string? envValue,
            string? configValue,
            bool isProduction,
            out bool usedDevFallback)
        {
            usedDevFallback = false;

            var configured = FirstNonBlank(envValue, configValue);
            if (configured is not null)
            {
                return Path.GetFullPath(configured);
            }

            if (isProduction)
            {
                throw new InvalidOperationException(
                    $"Profile photo storage is not configured. Set the " +
                    $"'{ProfilePhotoStorageOptions.EnvironmentVariable}' environment variable " +
                    $"(or '{ProfilePhotoStorageOptions.SectionName}:Directory') to an absolute path " +
                    $"on a persistent volume. Refusing to start with ephemeral storage in Production.");
            }

            usedDevFallback = true;
            return Path.GetFullPath(Path.Combine(Path.GetTempPath(), "gohardapi", "profile-photos"));
        }

        private static string? FirstNonBlank(params string?[] values)
        {
            foreach (var v in values)
            {
                if (!string.IsNullOrWhiteSpace(v))
                {
                    return v;
                }
            }
            return null;
        }
    }
}
