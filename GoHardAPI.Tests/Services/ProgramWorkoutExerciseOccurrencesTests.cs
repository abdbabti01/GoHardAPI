using System.Text.Json;
using GoHardAPI.Services;
using Xunit;

namespace GoHardAPI.Tests.Services
{
    /// <summary>
    /// Pure, I/O-free coverage of <see cref="ProgramWorkoutExerciseOccurrences"/>: the
    /// validation contract for write paths (<see cref="ProgramWorkoutExerciseOccurrences.Normalize"/>)
    /// and the never-fails fill-in used by read/self-heal paths
    /// (<see cref="ProgramWorkoutExerciseOccurrences.NormalizeForRead"/>). The persisted CAS
    /// orchestration (<c>EnsurePersistedAsync</c>) is covered against a real database in
    /// <c>ProgramWorkoutOccurrenceKeyPostgresTests</c>.
    /// </summary>
    public class ProgramWorkoutExerciseOccurrencesTests
    {
        private static JsonElement[] Parse(string json) =>
            JsonSerializer.Deserialize<JsonElement[]>(json)!;

        private static string? KeyOf(JsonElement e) =>
            e.TryGetProperty("occurrenceKey", out var k) ? k.GetString() : null;

        // ---- missing-key fill --------------------------------------------------------------

        [Fact]
        public void Normalize_RepeatedExerciseTemplateId_GetsDistinctOccurrenceKeys()
        {
            const string json = """
            [
              { "name": "Bench Press", "exerciseTemplateId": 7 },
              { "name": "Bench Press", "exerciseTemplateId": 7 }
            ]
            """;

            var result = ProgramWorkoutExerciseOccurrences.Normalize(json);

            Assert.True(result.IsValid);
            Assert.True(result.Changed);
            var entries = Parse(result.Json);
            var k1 = KeyOf(entries[0]);
            var k2 = KeyOf(entries[1]);
            Assert.False(string.IsNullOrWhiteSpace(k1));
            Assert.False(string.IsNullOrWhiteSpace(k2));
            Assert.NotEqual(k1, k2);
        }

        [Fact]
        public void Normalize_AdHocEntryWithNoTemplateId_GetsAStableKey()
        {
            const string json = """[ { "name": "Farmers Carry" } ]""";

            var first = ProgramWorkoutExerciseOccurrences.Normalize(json);
            Assert.True(first.Changed);
            var key = KeyOf(Parse(first.Json)[0]);
            Assert.False(string.IsNullOrWhiteSpace(key));

            // Re-normalizing the already-keyed output is a no-op: the key is stable, not
            // regenerated on every pass.
            var second = ProgramWorkoutExerciseOccurrences.Normalize(first.Json);
            Assert.False(second.Changed);
            Assert.Equal(first.Json, second.Json);
            Assert.Equal(key, KeyOf(Parse(second.Json)[0]));
        }

        [Fact]
        public void Normalize_PreservesEveryOtherField_Untouched()
        {
            const string json = """
            [ { "name": "Row", "sets": 3, "reps": 10, "weight": 45.5, "rest": 60, "notes": "tempo", "exerciseTemplateId": 3 } ]
            """;

            var result = ProgramWorkoutExerciseOccurrences.Normalize(json);
            var e = Parse(result.Json)[0];

            Assert.Equal("Row", e.GetProperty("name").GetString());
            Assert.Equal(3, e.GetProperty("sets").GetInt32());
            Assert.Equal(10, e.GetProperty("reps").GetInt32());
            Assert.Equal(45.5, e.GetProperty("weight").GetDouble());
            Assert.Equal(60, e.GetProperty("rest").GetInt32());
            Assert.Equal("tempo", e.GetProperty("notes").GetString());
            Assert.Equal(3, e.GetProperty("exerciseTemplateId").GetInt32());
        }

        // ---- reorder / prescription-edit preserve identity ----------------------------------

        [Fact]
        public void Normalize_SuppliedKeys_ReorderedArray_PreservesEachKeyVerbatim()
        {
            const string original = """
            [
              { "name": "A", "occurrenceKey": "k-a" },
              { "name": "B", "occurrenceKey": "k-b" }
            ]
            """;
            const string reordered = """
            [
              { "name": "B", "occurrenceKey": "k-b" },
              { "name": "A", "occurrenceKey": "k-a" }
            ]
            """;

            var result = ProgramWorkoutExerciseOccurrences.Normalize(reordered);

            Assert.True(result.IsValid);
            Assert.False(result.Changed); // nothing was missing/invalid; keys pass through as-is
            var entries = Parse(result.Json);
            Assert.Equal("k-b", KeyOf(entries[0]));
            Assert.Equal("k-a", KeyOf(entries[1]));
        }

        [Fact]
        public void Normalize_PrescriptionEditOnSameOccurrence_KeyUnchanged()
        {
            const string edited = """[ { "name": "Bench Press", "sets": 5, "occurrenceKey": "k-1" } ]""";

            var result = ProgramWorkoutExerciseOccurrences.Normalize(edited);

            Assert.False(result.Changed);
            Assert.Equal("k-1", KeyOf(Parse(result.Json)[0]));
        }

        // ---- replace / duplicate: client mints a new key, server just accepts + validates ---

        [Fact]
        public void Normalize_ReplacedExerciseIdentity_WithFreshKey_IsAccepted()
        {
            // Client replaced "Bench Press" (k-1) with "Incline Press" but kept a fresh key.
            const string json = """[ { "name": "Incline Press", "occurrenceKey": "k-2" } ]""";

            var result = ProgramWorkoutExerciseOccurrences.Normalize(json);

            Assert.True(result.IsValid);
            Assert.False(result.Changed);
        }

        [Fact]
        public void Normalize_DuplicatedOccurrence_WithDistinctKeys_IsAccepted()
        {
            const string json = """
            [
              { "name": "Bench Press", "occurrenceKey": "k-1" },
              { "name": "Bench Press", "occurrenceKey": "k-1-copy" }
            ]
            """;

            var result = ProgramWorkoutExerciseOccurrences.Normalize(json);

            Assert.True(result.IsValid);
        }

        // ---- validation contract: reject malformed / duplicate ------------------------------

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Normalize_EmptyOrWhitespaceKey_IsRejected(string badKey)
        {
            var json = $$"""[ { "name": "X", "occurrenceKey": "{{badKey}}" } ]""";

            var result = ProgramWorkoutExerciseOccurrences.Normalize(json);

            Assert.False(result.IsValid);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public void Normalize_NonStringKey_IsRejected()
        {
            const string json = """[ { "name": "X", "occurrenceKey": 12345 } ]""";

            var result = ProgramWorkoutExerciseOccurrences.Normalize(json);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Normalize_TooLongKey_IsRejected()
        {
            var json = $$"""[ { "name": "X", "occurrenceKey": "{{new string('a', 101)}}" } ]""";

            var result = ProgramWorkoutExerciseOccurrences.Normalize(json);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Normalize_KeyAtMaxLength_IsAccepted()
        {
            var json = $$"""[ { "name": "X", "occurrenceKey": "{{new string('a', 100)}}" } ]""";

            var result = ProgramWorkoutExerciseOccurrences.Normalize(json);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Normalize_DuplicateSuppliedKeysWithinOneWorkout_IsRejected()
        {
            const string json = """
            [
              { "name": "A", "occurrenceKey": "same" },
              { "name": "B", "occurrenceKey": "same" }
            ]
            """;

            var result = ProgramWorkoutExerciseOccurrences.Normalize(json);

            Assert.False(result.IsValid);
            Assert.Contains("Duplicate", result.Error);
        }

        [Fact]
        public void Normalize_SameKeyAcrossDifferentPayloads_IsNotARejection_ScopeIsPerWorkoutOnly()
        {
            // occurrenceKey is only unique WITHIN one workout's array — the same value in two
            // separate ExercisesJson payloads (e.g. two different ProgramWorkout rows, or the
            // same workout reused across weeks) is fine.
            const string workoutA = """[ { "name": "A", "occurrenceKey": "shared" } ]""";
            const string workoutB = """[ { "name": "A", "occurrenceKey": "shared" } ]""";

            Assert.True(ProgramWorkoutExerciseOccurrences.Normalize(workoutA).IsValid);
            Assert.True(ProgramWorkoutExerciseOccurrences.Normalize(workoutB).IsValid);
        }

        // ---- NormalizeForRead never rejects, self-heals bad data instead --------------------

        [Fact]
        public void NormalizeForRead_MalformedKey_IsHealedNotRejected()
        {
            const string json = """[ { "name": "X", "occurrenceKey": "" } ]""";

            var result = ProgramWorkoutExerciseOccurrences.NormalizeForRead(json);

            Assert.True(result.Changed);
            Assert.False(string.IsNullOrWhiteSpace(KeyOf(Parse(result.Json)[0])));
        }

        [Fact]
        public void NormalizeForRead_DuplicateSuppliedKeys_FirstKeptSecondReassigned()
        {
            const string json = """
            [
              { "name": "A", "occurrenceKey": "same" },
              { "name": "B", "occurrenceKey": "same" }
            ]
            """;

            var result = ProgramWorkoutExerciseOccurrences.NormalizeForRead(json);

            Assert.True(result.Changed);
            var entries = Parse(result.Json);
            Assert.Equal("same", KeyOf(entries[0]));
            Assert.NotEqual("same", KeyOf(entries[1]));
        }

        // ---- non-array / unparseable payloads pass through untouched (not this normalizer's job) --

        [Fact]
        public void Normalize_NonArrayJson_PassesThroughUnchanged()
        {
            const string json = """{ "not": "an array" }""";

            var result = ProgramWorkoutExerciseOccurrences.Normalize(json);

            Assert.True(result.IsValid);
            Assert.False(result.Changed);
            Assert.Equal(json, result.Json);
        }

        [Fact]
        public void Normalize_InvalidJson_PassesThroughUnchanged()
        {
            const string json = "not json at all";

            var result = ProgramWorkoutExerciseOccurrences.Normalize(json);

            Assert.True(result.IsValid);
            Assert.Equal(json, result.Json);
        }
    }
}
