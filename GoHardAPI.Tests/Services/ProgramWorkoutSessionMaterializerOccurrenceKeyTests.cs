using GoHardAPI.Models;
using GoHardAPI.Services;
using Xunit;

namespace GoHardAPI.Tests.Services
{
    /// <summary>
    /// Proves <see cref="ProgramWorkoutSessionMaterializer.Build"/> copies a source
    /// <c>occurrenceKey</c> verbatim onto the materialized <see cref="Exercise"/> and never
    /// invents one independently when an entry doesn't have one — normalization/backfill is
    /// the caller's job (<see cref="ProgramWorkoutExerciseOccurrences.EnsurePersistedAsync"/>),
    /// not this pure builder's.
    /// </summary>
    public class ProgramWorkoutSessionMaterializerOccurrenceKeyTests
    {
        private static ProgramWorkout WorkoutWith(string exercisesJson) => new()
        {
            Id = 1,
            ProgramId = 1,
            WeekNumber = 1,
            DayNumber = 1,
            WorkoutName = "Day 1",
            WorkoutType = "Strength",
            ExercisesJson = exercisesJson,
            Program = new GoHardAPI.Models.Program
            {
                Id = 1,
                UserId = 1,
                Title = "P",
                StartDate = new DateTime(2020, 1, 6, 0, 0, 0, DateTimeKind.Utc),
            },
        };

        [Fact]
        public void Build_CopiesOccurrenceKey_Verbatim_ForEachEntry()
        {
            const string json = """
            [
              { "name": "Bench Press", "exerciseTemplateId": 7, "occurrenceKey": "k-1" },
              { "name": "Bench Press", "exerciseTemplateId": 7, "occurrenceKey": "k-2" }
            ]
            """;

            var session = ProgramWorkoutSessionMaterializer.Build(userId: 1, WorkoutWith(json), requestedProgramId: 1);

            Assert.Equal(2, session.Exercises.Count);
            var keys = session.Exercises.Select(e => e.OccurrenceKey).ToList();
            Assert.Contains("k-1", keys);
            Assert.Contains("k-2", keys);
        }

        [Fact]
        public void Build_EntryMissingOccurrenceKey_MaterializesWithNullOccurrenceKey_NeverInvented()
        {
            const string json = """[ { "name": "Farmers Carry" } ]""";

            var session = ProgramWorkoutSessionMaterializer.Build(userId: 1, WorkoutWith(json), requestedProgramId: 1);

            Assert.Single(session.Exercises);
            Assert.Null(session.Exercises.First().OccurrenceKey);
        }

        [Fact]
        public void Build_TwoSeparateMaterializations_OfTheSameWorkout_ProduceTheSameKeysPerOccurrence()
        {
            // "Two intentional Sessions from the same template remain valid": each
            // materialization is independent, and since Build() only copies whatever is
            // already in the source JSON (never invents), two Sessions built from an
            // already-normalized workout carry identical occurrenceKey values per entry —
            // by design not globally unique, distinguished by SessionId once persisted.
            const string json = """[ { "name": "Row", "occurrenceKey": "k-1" } ]""";
            var workout = WorkoutWith(json);

            var sessionA = ProgramWorkoutSessionMaterializer.Build(userId: 1, workout, requestedProgramId: 1);
            var sessionB = ProgramWorkoutSessionMaterializer.Build(userId: 1, workout, requestedProgramId: 1);

            Assert.Equal("k-1", sessionA.Exercises.First().OccurrenceKey);
            Assert.Equal("k-1", sessionB.Exercises.First().OccurrenceKey);
        }
    }
}
