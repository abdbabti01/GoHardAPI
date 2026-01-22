using GoHardAPI.Models;

namespace GoHardAPI.Repositories
{
    /// <summary>
    /// Repository interface for ExerciseTemplate entity with specialized queries.
    /// </summary>
    public interface IExerciseTemplateRepository : IRepository<ExerciseTemplate>
    {
        Task<IEnumerable<ExerciseTemplate>> GetByCategoryAsync(string category);
        Task<IEnumerable<ExerciseTemplate>> GetByMuscleGroupAsync(string muscleGroup);
        Task<IEnumerable<ExerciseTemplate>> GetByEquipmentAsync(string equipment);
        Task<IEnumerable<ExerciseTemplate>> SearchAsync(string? category, string? muscleGroup, string? equipment);
        Task<IEnumerable<string>> GetDistinctCategoriesAsync();
        Task<IEnumerable<string>> GetDistinctMuscleGroupsAsync();
        Task<bool> NameExistsAsync(string name, int? excludeId = null);
    }
}
