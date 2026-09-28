namespace GoHardAPI.Configuration
{
    /// <summary>Lifted-weight contract cutover (Phase 2C). Both flags default false.</summary>
    public class LiftedWeightOptions
    {
        public const string SectionName = "LiftedWeight";
        public const string HeaderName = "X-Lifted-Weight-Unit";
        public const string CanonicalUnit = "kg";

        /// <summary>Reject set writes from clients that do not declare canonical kg.</summary>
        public bool RequireCanonicalClient { get; set; }

        /// <summary>Set ONLY after the production workout-history reset was run and verified.</summary>
        public bool CanonicalHistory { get; set; }

        /// <summary>History cannot be canonical while legacy clients may still write.</summary>
        public bool GuardEnabled => RequireCanonicalClient || CanonicalHistory;
    }
}
