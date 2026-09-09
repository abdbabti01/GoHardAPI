using GoHardAPI.Data;
using GoHardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GoHardAPI.Services
{
    /// <summary>
    /// A user's current body measurements, each field independently resolved.
    /// Any field may be <c>null</c> meaning "unknown" - never 0, never a guess.
    /// </summary>
    public sealed record CurrentMeasurements(
        double? WeightKg,
        double? HeightCm,
        double? BodyFatPercentage,
        double? Bmi);

    /// <summary>
    /// The single source of truth for a user's <b>current</b> body measurements.
    /// Body Metrics rows (<c>BodyMetricsController</c>) are the history; this
    /// service DERIVES the current value of each field ON READ, so there is no
    /// persisted summary that a create / update / delete or an overlapping
    /// request could leave inconsistent, and no older computation can overwrite a
    /// newer one - every caller recomputes from the committed history at that
    /// instant.
    ///
    /// <para><b>Per-field selection</b> (<see cref="GetForUserAsync"/>): scan the
    /// user's Body Metrics ordered <c>RecordedAt</c> DESC, then <c>Id</c> DESC
    /// (deterministic on SQL Server / PostgreSQL / SQLite; the <c>Id</c> tie-break
    /// is the common path because <c>BodyMetric.RecordedAt</c> serialises
    /// date-only, so same-day rows share a timestamp). For each of
    /// Weight / Height / BodyFatPercentage independently, take the first row with
    /// a USABLE (present and &gt; 0) value for THAT field. A partial row moves
    /// only the fields it fills; a backdated row never wins unless nothing newer
    /// fills the field; a stored <c>0</c>/negative is skipped as data-entry
    /// error, never surfaced.</para>
    ///
    /// <para><b>Legacy fallback &amp; provenance.</b> When no history row fills a
    /// field, fall back to <c>User.X</c> - sound only because the
    /// <c>BodyMetricsController</c> write paths keep <c>User.X</c> non-null
    /// <i>iff</i> the schema can still treat it as a genuine legacy profile
    /// value:
    /// <list type="bullet">
    ///   <item><b>create / update / delete</b>: whenever a row that covers X with
    ///     a usable value is written or removed,
    ///     <see cref="RetireCoveredLegacyScalars"/> sets <c>User.X = null</c> in
    ///     the SAME transaction. On delete this is unconditional (not "only if it
    ///     was the last covering row"): a still-present scalar could be a copy of
    ///     the row just removed - old "Updated from profile" edits set both - and
    ///     if surviving rows still cover X the derived read uses them anyway, so
    ///     nulling loses nothing and stays race-free.</item>
    /// </list>
    /// A field a write never touched keeps its legacy scalar, so a genuine
    /// legacy value with no history survives. Retirement is monotonic
    /// (non-null -&gt; null only) and idempotent, so concurrent writers converge
    /// with no lock or rowversion, and EF's per-column UPDATE means retiring
    /// different fields concurrently does not clobber. Consequence: once history
    /// has ever covered a field, deleting or clearing every row that fills it
    /// yields <c>null</c> (unknown) or an older surviving row's value - never the
    /// removed value, never a resurrected legacy scalar.</para>
    ///
    /// <para><b>BMI</b> is a pure function of the resolved Height + Weight
    /// (kg / m²), <c>null</c> when either is missing or height is 0.</para>
    /// </summary>
    public sealed class CurrentMeasurementsService
    {
        private readonly TrainingContext _context;

        public CurrentMeasurementsService(TrainingContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Resolve <paramref name="user"/>'s current measurements. Pass the
        /// already-loaded <see cref="User"/> so the legacy fallback reads from the
        /// same instance the caller holds; only Body Metrics are queried.
        /// </summary>
        public async Task<CurrentMeasurements> GetForUserAsync(
            User user, CancellationToken cancellationToken = default)
        {
            var history = await _context.BodyMetrics
                .AsNoTracking()
                .Where(m => m.UserId == user.Id)
                .OrderByDescending(m => m.RecordedAt)
                .ThenByDescending(m => m.Id)
                .Select(m => new Row(m.Weight, m.Height, m.BodyFatPercentage))
                .ToListAsync(cancellationToken);

            var weight = Resolve(history.Select(r => r.Weight), user.Weight);
            var height = Resolve(history.Select(r => r.Height), user.Height);
            var bodyFat = Resolve(history.Select(r => r.BodyFatPercentage), user.BodyFatPercentage);

            return new CurrentMeasurements(weight, height, bodyFat, ComputeBmi(height, weight));
        }

        /// <summary>
        /// True when <paramref name="value"/> is a usable measurement: present and
        /// strictly positive. A stored <c>0</c> or negative is a data-entry error,
        /// not a measurement - it is skipped, never surfaced as "current" and
        /// never used to retire a legacy scalar (matches the old
        /// <c>NutritionController</c> <c>&gt; 0</c> guard).
        /// </summary>
        internal static bool IsUsable(decimal? value) => value is > 0m;

        /// <summary>
        /// First USABLE value from the (already newest-first) history sequence,
        /// converted to <c>double</c>; otherwise the provably-legacy
        /// <paramref name="legacy"/>; otherwise <c>null</c>.
        /// </summary>
        private static double? Resolve(IEnumerable<decimal?> historyNewestFirst, double? legacy)
        {
            foreach (var v in historyNewestFirst)
            {
                if (IsUsable(v))
                {
                    return (double)v!.Value;
                }
            }
            return legacy;
        }

        /// <summary>
        /// Retire <c>User.X</c> (set to NULL) for every field <paramref name="row"/>
        /// covers with a usable (&gt; 0) value. Call in the SAME unit of work as
        /// persisting <paramref name="row"/> on a create or an update - and, on a
        /// delete, with the row being removed (its coverage makes any still-present
        /// legacy scalar for that field unprovable). Monotonic and idempotent:
        /// only ever sets NULL, so overlapping callers converge with no lock.
        /// </summary>
        public static void RetireCoveredLegacyScalars(User user, BodyMetric row)
        {
            var retired = false;
            if (IsUsable(row.Weight))
            {
                user.Weight = null;
                retired = true;
            }
            if (IsUsable(row.Height))
            {
                user.Height = null;
                retired = true;
            }
            if (IsUsable(row.BodyFatPercentage))
            {
                user.BodyFatPercentage = null;
                retired = true;
            }
            if (retired)
            {
                // BMI is derived-on-read; its stored column is vestigial. Clear it
                // whenever an input is retired so no stale value lingers at rest.
                user.BMI = null;
            }
        }

        /// <summary>
        /// kg / m². <c>null</c> when height or weight is missing or non-positive.
        /// </summary>
        internal static double? ComputeBmi(double? heightCm, double? weightKg)
        {
            if (heightCm is not { } h || weightKg is not { } w || h <= 0 || w <= 0)
            {
                return null;
            }

            var heightMeters = h / 100.0;
            return w / (heightMeters * heightMeters);
        }

        private readonly record struct Row(decimal? Weight, decimal? Height, decimal? BodyFatPercentage);
    }
}
