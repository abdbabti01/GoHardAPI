namespace GoHardAPI.Configuration
{
    /// <summary>
    /// Where profile-photo files physically live. Bound once at startup (see
    /// <c>Program.cs</c>) from, in order of precedence:
    /// <list type="number">
    ///   <item>the <c>PROFILE_PHOTO_STORAGE_PATH</c> environment variable;</item>
    ///   <item>configuration key <c>ProfilePhotoStorage:Directory</c>;</item>
    ///   <item>(non-Production only) a stable temp path OUTSIDE the publish output.</item>
    /// </list>
    ///
    /// <para>In <b>Production</b> the app <b>refuses to start</b> if neither the
    /// env var nor the config key is set - it never silently falls back to
    /// ephemeral storage. A configured directory is a prerequisite for
    /// durability, NOT proof that a persistent volume is actually mounted there;
    /// operators must verify the mount separately (see the Railway notes in the
    /// task report / README).</para>
    ///
    /// <para>The public URL contract is fixed and NOT configurable: photos are
    /// always served under <see cref="PublicPathPrefix"/> and
    /// <c>User.ProfilePhotoUrl</c> is always stored as
    /// <c>"/uploads/profiles/{filename}"</c>. Only the physical directory moves.</para>
    /// </summary>
    public sealed class ProfilePhotoStorageOptions
    {
        /// <summary>Configuration section name.</summary>
        public const string SectionName = "ProfilePhotoStorage";

        /// <summary>Environment variable that overrides the config section.</summary>
        public const string EnvironmentVariable = "PROFILE_PHOTO_STORAGE_PATH";

        /// <summary>
        /// Fixed public request-path prefix these files are served under, and the
        /// prefix every stored <c>ProfilePhotoUrl</c> carries. The mobile client
        /// and all existing rows depend on it - do not change.
        /// </summary>
        public const string PublicPathPrefix = "/uploads/profiles";

        /// <summary>
        /// Absolute filesystem directory holding the photo files. Resolved and
        /// created at startup; guaranteed non-empty and rooted.
        /// </summary>
        public required string Directory { get; init; }

        /// <summary>Maximum accepted upload size in bytes (5 MB).</summary>
        public long MaxBytes { get; init; } = 5 * 1024 * 1024;
    }
}
