using GoHardAPI.Models;
using GoHardAPI.Services;
using Xunit;

namespace GoHardAPI.Tests.Services
{
    public class ExerciseTemplateResolverTests
    {
        private static ExerciseTemplate T(int id, string name, bool custom = false, int? owner = null) =>
            new() { Id = id, Name = name, IsCustom = custom, CreatedByUserId = owner };

        private static readonly List<ExerciseTemplate> Catalog = new()
        {
            T(1, "Bench Press"),
            T(2, "Dumbbell Bench Press"),
            T(3, "Pull-ups"),
            T(4, "Romanian Deadlift"),
            T(5, "Overhead Press"),
            T(6, "Leg Curl"),
            T(7, "Hammer Curls"),
            T(8, "Hammer Curls"),          // seed duplicate
            T(9, "Squat"),
            T(10, "Rowing Machine"),
            T(50, "Bench Press", custom: true, owner: 1),
            T(51, "My Special Press", custom: true, owner: 1),
        };

        [Theory]
        [InlineData("Bench Press", 1)]
        [InlineData("  bench   PRESS ", 1)]
        [InlineData("Dumbbell Bench Press", 2)]
        [InlineData("Pull Ups", 3)]
        [InlineData("pull-up", 3)]
        [InlineData("Pullups", 3)]
        [InlineData("RDL", 4)]
        [InlineData("OHP", 5)]
        [InlineData("Military Press", 5)]
        [InlineData("Barbell Bench Press", 1)]
        [InlineData("Back Squat", 9)]
        [InlineData("Leg Curls", 6)]
        public void Resolves_ExactNormalizedNameOrExactAlias(string aiName, int expectedId) =>
            Assert.Equal(expectedId, ExerciseTemplateResolver.Resolve(aiName, Catalog));

        [Fact]
        public void DuplicateSystemNames_ResolveToLowestId() =>
            Assert.Equal(7, ExerciseTemplateResolver.Resolve("Hammer Curl", Catalog));

        [Theory]
        [InlineData("DB Bench Press")]   // legacy matcher mapped this to barbell Bench Press
        [InlineData("Incline Bench")]
        [InlineData("Row")]              // legacy matcher mapped this to Rowing Machine
        [InlineData("Curl")]
        [InlineData("Cable Row")]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void UnresolvedOrAmbiguous_ReturnsNull_NeverSubstringMatch(string? aiName) =>
            Assert.Null(ExerciseTemplateResolver.Resolve(aiName, Catalog));

        [Fact]
        public void CustomTemplates_AreNeverConsidered()
        {
            Assert.Null(ExerciseTemplateResolver.Resolve("My Special Press", Catalog));
            Assert.Equal(1, ExerciseTemplateResolver.Resolve("Bench Press", Catalog)); // not custom 50
        }

        [Theory]
        [InlineData("Push-ups", "push up")]
        [InlineData("Lunges", "lunge")]
        [InlineData("Leg Press", "leg press")]   // "ss" ending kept
        [InlineData("Bent-Over_Row", "bent over row")]
        public void Normalize_IsDeterministic(string input, string expected) =>
            Assert.Equal(expected, ExerciseTemplateResolver.Normalize(input));
    }
}
