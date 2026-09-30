using System;
using System.Collections.Generic;
using System.Linq;
using GoHardAPI.Models;
using GoHardAPI.Services;
using Xunit;

namespace GoHardAPI.Tests.Services
{
    public class ProgramWorkoutSessionMaterializerTargetsTests
    {
        private static ProgramWorkout WorkoutWith(string json) => new()
        {
            Id = 1, ProgramId = 1, WeekNumber = 1, DayNumber = 1,
            WorkoutName = "Day 1", WorkoutType = "Strength", ExercisesJson = json,
            Program = new GoHardAPI.Models.Program
            {
                Id = 1, UserId = 1, Title = "P",
                StartDate = new DateTime(2020, 1, 6, 0, 0, 0, DateTimeKind.Utc),
            },
        };

        private static List<Exercise> Build(string json) =>
            ProgramWorkoutSessionMaterializer.Build(1, WorkoutWith(json), 1).Exercises.ToList();

        [Fact]
        public void ExactPrescription_3x8_MinEqualsMax()
        {
            var e = Assert.Single(Build("""[{"name":"Bench","sets":3,"reps":8}]"""));
            Assert.Equal<(int?, int?, int?)>((3, 8, 8), (e.TargetSets, e.TargetRepsMin, e.TargetRepsMax));
        }

        [Fact]
        public void Range_3x8to10()
        {
            var e = Assert.Single(Build("""[{"name":"Bench","sets":3,"reps":8,"repsMax":10}]"""));
            Assert.Equal<(int?, int?, int?)>((3, 8, 10), (e.TargetSets, e.TargetRepsMin, e.TargetRepsMax));
        }

        [Theory]
        [InlineData("""{"name":"X","sets":"3","reps":"8-10"}""", null, null, null)]
        [InlineData("""{"name":"X","sets":3.5,"reps":8.0}""", null, null, null)]
        [InlineData("""{"name":"X","sets":0,"reps":-1}""", null, null, null)]
        [InlineData("""{"name":"X","sets":3,"reps":null}""", 3, null, null)]
        [InlineData("""{"name":"X","sets":3,"reps":10,"repsMax":8}""", 3, 10, 10)]
        [InlineData("""{"name":"X","reps":8,"repsMax":"10"}""", null, 8, 8)]
        [InlineData("""{"name":"X"}""", null, null, null)]
        public void InvalidOrPartialValues_NeverParsed(string entry, int? sets, int? min, int? max)
        {
            var e = Assert.Single(Build($"[{entry}]"));
            Assert.Equal<(int?, int?, int?)>((sets, min, max), (e.TargetSets, e.TargetRepsMin, e.TargetRepsMax));
        }

        [Fact]
        public void TwoOccurrencesOfSameExercise_KeepIndependentTargets_AndPositionalSortOrder()
        {
            var list = Build("""
            [
              {"name":"Bench","exerciseTemplateId":7,"sets":3,"reps":5,"occurrenceKey":"a"},
              {"name":"Row","sets":4,"reps":10,"occurrenceKey":"b"},
              {"name":"Bench","exerciseTemplateId":7,"sets":2,"reps":10,"repsMax":12,"occurrenceKey":"c"}
            ]
            """);
            Assert.Equal(new[] { 0, 1, 2 }, list.Select(e => e.SortOrder));
            var heavy = list.Single(e => e.OccurrenceKey == "a");
            var backoff = list.Single(e => e.OccurrenceKey == "c");
            Assert.Equal<(int?, int?, int?)>((3, 5, 5), (heavy.TargetSets, heavy.TargetRepsMin, heavy.TargetRepsMax));
            Assert.Equal<(int?, int?, int?)>((2, 10, 12), (backoff.TargetSets, backoff.TargetRepsMin, backoff.TargetRepsMax));
            Assert.Equal(7, heavy.ExerciseTemplateId);
        }

        [Theory]
        [InlineData("""{"name":"X","exerciseTemplateId":"7"}""")]
        [InlineData("""{"name":"X","exerciseTemplateId":7.5}""")]
        [InlineData("""{"name":"X","exerciseTemplateId":null}""")]
        public void NonIntegerTemplateId_IsNull_NeverThrows(string entry) =>
            Assert.Null(Assert.Single(Build($"[{entry}]")).ExerciseTemplateId);

        [Fact]
        public void PlanEditAfterMaterialization_DoesNotMutateTheBuiltSession()
        {
            var workout = WorkoutWith("""[{"name":"Bench","sets":3,"reps":8}]""");
            var session = ProgramWorkoutSessionMaterializer.Build(1, workout, 1);
            workout.ExercisesJson = """[{"name":"Bench","sets":5,"reps":3}]""";
            var e = Assert.Single(session.Exercises);
            Assert.Equal<(int?, int?, int?)>((3, 8, 8), (e.TargetSets, e.TargetRepsMin, e.TargetRepsMax));
        }
    }
}
