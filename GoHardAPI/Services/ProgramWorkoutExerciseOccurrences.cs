using System.Text.Json;
using System.Text.Json.Nodes;
using GoHardAPI.Data;
using GoHardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GoHardAPI.Services
{
    /// <summary>
    /// Single source of truth for the <c>occurrenceKey</c> field inside
    /// <see cref="ProgramWorkout.ExercisesJson"/> entries: assigning missing keys, validating
    /// supplied ones, and persisting a normalized workout exactly once (self-heal on read).
    ///
    /// <para><b>Scope</b>: <c>occurrenceKey</c> identifies one exercise <i>occurrence</i> within
    /// a single workout's <c>ExercisesJson</c> array. It is never globally unique — the same key
    /// legitimately repeats across different <see cref="ProgramWorkout"/> rows (template reuse
    /// across weeks) and across every <see cref="Exercise"/> materialized from the same
    /// <see cref="ProgramWorkout"/> into different Sessions. Uniqueness is enforced only within
    /// one array.</para>
    ///
    /// <para><b>Every write path</b> (<c>AddWorkout</c>, <c>UpdateWorkout</c>,
    /// <c>CreateProgram</c>'s embedded workouts, the two AI-generation writers in
    /// <c>ChatController</c>) calls <see cref="Normalize"/>: missing keys are filled in
    /// (compatibility with clients that don't yet send them), malformed or duplicate supplied
    /// keys are rejected outright — the client is responsible for minting a fresh key when it
    /// replaces an occurrence's exercise identity or duplicates an occurrence; reordering or
    /// editing an occurrence's prescription must resubmit its existing key unchanged.</para>
    ///
    /// <para><b>Every read path</b> that returns a <see cref="ProgramWorkout"/> to a client
    /// (program/workout GETs) and Session creation (both the keyed first-write and the legacy
    /// unkeyed controller path) calls <see cref="EnsurePersistedAsync"/>: it fills in whatever
    /// is missing exactly like a write, but — since a GET can't reject a legacy row it didn't
    /// create — never errors, and persists the result through a compare-and-swap write so a
    /// concurrent edit is never clobbered and a key, once assigned, is never regenerated on a
    /// later read or independently inside
    /// <see cref="ProgramWorkoutSessionMaterializer"/>.</para>
    /// </summary>
    public static class ProgramWorkoutExerciseOccurrences
    {
        private const string KeyProperty = "occurrenceKey";
        private const int MaxKeyLength = 100;

        /// <summary>Result of a normalization pass over one workout's <c>ExercisesJson</c>.</summary>
        public readonly record struct NormalizeResult(string Json, bool Changed, string? Error)
        {
            public bool IsValid => Error is null;
        }

        /// <summary>
        /// Write-path normalization: fills in missing <c>occurrenceKey</c> values, and rejects
        /// (non-null <see cref="NormalizeResult.Error"/>) a malformed or duplicate supplied key.
        /// A payload that isn't a JSON array of objects is left completely untouched here —
        /// shape validation is <see cref="ProgramWorkoutSessionMaterializer"/>'s job at
        /// materialization time, not this normalizer's.
        /// </summary>
        public static NormalizeResult Normalize(string exercisesJson) =>
            NormalizeCore(exercisesJson, rejectInvalid: true);

        /// <summary>
        /// Read-path normalization: same fill-in behavior as <see cref="Normalize"/>, but never
        /// errors. Pre-existing malformed or duplicate keys (which validated writes should no
        /// longer produce, but legacy data might already contain) are healed by replacing the
        /// offending entries' keys with freshly minted ones rather than surfacing a failure from
        /// a GET.
        /// </summary>
        public static NormalizeResult NormalizeForRead(string exercisesJson) =>
            NormalizeCore(exercisesJson, rejectInvalid: false);

        private static NormalizeResult NormalizeCore(string exercisesJson, bool rejectInvalid)
        {
            JsonNode? root;
            try
            {
                root = JsonNode.Parse(exercisesJson);
            }
            catch (JsonException)
            {
                return new NormalizeResult(exercisesJson, false, null);
            }

            if (root is not JsonArray array)
            {
                return new NormalizeResult(exercisesJson, false, null);
            }

            var changed = false;
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var item in array)
            {
                if (item is not JsonObject obj)
                {
                    // Not an exercise object (malformed shape). Not this normalizer's concern —
                    // materialization-time parsing already surfaces a clear format error.
                    continue;
                }

                var hasKey = obj.TryGetPropertyValue(KeyProperty, out var keyNode) && keyNode is not null;

                if (!hasKey)
                {
                    obj[KeyProperty] = NewKey(seen);
                    changed = true;
                    continue;
                }

                var malformed = keyNode!.GetValueKind() != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(keyNode.GetValue<string>())
                    || keyNode.GetValue<string>().Length > MaxKeyLength;

                if (malformed)
                {
                    if (rejectInvalid)
                    {
                        return new NormalizeResult(
                            exercisesJson, false,
                            "Each exercise occurrenceKey must be a non-empty string of at most " +
                            $"{MaxKeyLength} characters.");
                    }

                    obj[KeyProperty] = NewKey(seen);
                    changed = true;
                    continue;
                }

                var key = keyNode.GetValue<string>();
                if (!seen.Add(key))
                {
                    if (rejectInvalid)
                    {
                        return new NormalizeResult(
                            exercisesJson, false,
                            $"Duplicate exercise occurrenceKey '{key}' within the same workout.");
                    }

                    // Self-heal: the first occurrence of this key keeps it, later duplicates get
                    // a fresh one.
                    var fresh = NewKey(seen);
                    obj[KeyProperty] = fresh;
                    changed = true;
                }
            }

            if (!changed)
            {
                return new NormalizeResult(exercisesJson, false, null);
            }

            return new NormalizeResult(array.ToJsonString(), true, null);
        }

        private static string NewKey(HashSet<string> seen)
        {
            string key;
            do
            {
                key = Guid.NewGuid().ToString("N");
            } while (!seen.Add(key));

            return key;
        }

        /// <summary>
        /// Read-path orchestration: normalizes <paramref name="workout"/>'s <c>ExercisesJson</c>
        /// (<see cref="NormalizeForRead"/>) and, if anything changed, persists it with a
        /// compare-and-swap write scoped to the row's original text — so a concurrent
        /// <c>UpdateWorkout</c> (or another concurrent normalization) is never overwritten.
        /// Losing the race is a silent no-op: the concurrent writer already produced its own
        /// valid, normalized (or about-to-be-normalized) content. Always returns the normalized
        /// text for the caller to use immediately, whether or not this call's write persisted.
        ///
        /// <c>ExecuteUpdateAsync</c> requires a relational provider; the EF InMemory provider
        /// (unit tests only — production is always relational) does not support it. On that
        /// provider this normalizes in-memory only and skips the persist, matching how the rest
        /// of the keyed-CREATE path already degrades non-relational providers
        /// (<c>SessionCreateService.RunKeyedWithRetryAsync</c>).
        /// </summary>
        public static async Task<string> EnsurePersistedAsync(
            TrainingContext context, ProgramWorkout workout, CancellationToken cancellationToken)
        {
            var original = workout.ExercisesJson;
            var result = NormalizeForRead(original);

            if (!result.Changed)
            {
                return original;
            }

            if (!context.Database.IsRelational())
            {
                return result.Json;
            }

            var rowsAffected = await context.ProgramWorkouts
                .Where(w => w.Id == workout.Id && w.ExercisesJson == original)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(w => w.ExercisesJson, result.Json),
                    cancellationToken);

            if (rowsAffected > 0)
            {
                return result.Json;
            }

            // Lost the compare-and-swap: a concurrent writer already changed this row. Every
            // write path (this one, UpdateWorkout/AddWorkout, the AI-generation writers) runs
            // its content through Normalize/NormalizeForRead before persisting, so whatever the
            // winner just wrote is already fully keyed — adopt it rather than handing back our
            // own locally-generated (and now divergent) keys for entries that were missing one.
            var current = await context.ProgramWorkouts
                .AsNoTracking()
                .Where(w => w.Id == workout.Id)
                .Select(w => w.ExercisesJson)
                .FirstOrDefaultAsync(cancellationToken);

            if (current is null)
            {
                // Row deleted concurrently; nothing left to reconcile against.
                return result.Json;
            }

            var reread = NormalizeForRead(current);
            // Expected case: the winner's write was already fully normalized, so this is a
            // no-op and `current` is returned verbatim. If it somehow still needs filling
            // (shouldn't happen given every writer normalizes), use the locally-recomputed
            // text for this response without a further persist attempt — bounded to one retry.
            return reread.Changed ? reread.Json : current;
        }
    }
}
