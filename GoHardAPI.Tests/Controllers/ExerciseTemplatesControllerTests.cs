using GoHardAPI.Data;
using GoHardAPI.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    public class ExerciseTemplatesControllerTests
    {
        private TrainingContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<TrainingContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new TrainingContext(options);

            // Seed test data
            context.ExerciseTemplates.AddRange(
                new ExerciseTemplate
                {
                    Id = 1,
                    Name = "Bench Press",
                    Description = "Compound chest exercise",
                    Category = "Strength",
                    MuscleGroup = "Chest",
                    Equipment = "Barbell",
                    Difficulty = "Intermediate"
                },
                new ExerciseTemplate
                {
                    Id = 2,
                    Name = "Pull-ups",
                    Description = "Back exercise",
                    Category = "Strength",
                    MuscleGroup = "Back",
                    Equipment = "Pull-up Bar",
                    Difficulty = "Intermediate"
                }
            );
            context.SaveChanges();

            return context;
        }

        [Fact]
        public async Task GetExerciseTemplates_ReturnsAllTemplates()
        {
            // Arrange
            var context = GetInMemoryContext();

            // Act
            var templates = await context.ExerciseTemplates.ToListAsync();

            // Assert
            Assert.NotNull(templates);
            Assert.Equal(2, templates.Count);
            Assert.Contains(templates, t => t.Name == "Bench Press");
            Assert.Contains(templates, t => t.Name == "Pull-ups");
        }

        [Fact]
        public async Task GetExerciseTemplateById_ReturnsCorrectTemplate()
        {
            // Arrange
            var context = GetInMemoryContext();

            // Act
            var template = await context.ExerciseTemplates.FindAsync(1);

            // Assert
            Assert.NotNull(template);
            Assert.Equal("Bench Press", template.Name);
            Assert.Equal("Chest", template.MuscleGroup);
        }

        [Fact]
        public async Task GetExerciseTemplateByCategory_ReturnsFilteredResults()
        {
            // Arrange
            var context = GetInMemoryContext();

            // Act
            var strengthTemplates = await context.ExerciseTemplates
                .Where(t => t.Category == "Strength")
                .ToListAsync();

            // Assert
            Assert.NotNull(strengthTemplates);
            Assert.Equal(2, strengthTemplates.Count);
            Assert.All(strengthTemplates, t => Assert.Equal("Strength", t.Category));
        }
    }
}
