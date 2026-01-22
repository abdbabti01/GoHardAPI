using GoHardAPI.Data;
using GoHardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GoHardAPI.Repositories
{
    /// <summary>
    /// Repository implementation for ExerciseTemplate entity.
    /// </summary>
    public class ExerciseTemplateRepository : Repository<ExerciseTemplate>, IExerciseTemplateRepository
    {
        public ExerciseTemplateRepository(TrainingContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ExerciseTemplate>> GetByCategoryAsync(string category)
        {
            return await _dbSet
                .Where(t => t.Category == category)
                .ToListAsync();
        }

        public async Task<IEnumerable<ExerciseTemplate>> GetByMuscleGroupAsync(string muscleGroup)
        {
            return await _dbSet
                .Where(t => t.MuscleGroup == muscleGroup)
                .ToListAsync();
        }

        public async Task<IEnumerable<ExerciseTemplate>> GetByEquipmentAsync(string equipment)
        {
            return await _dbSet
                .Where(t => t.Equipment == equipment)
                .ToListAsync();
        }

        public async Task<IEnumerable<ExerciseTemplate>> SearchAsync(string? category, string? muscleGroup, string? equipment)
        {
            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(t => t.Category == category);

            if (!string.IsNullOrEmpty(muscleGroup))
                query = query.Where(t => t.MuscleGroup == muscleGroup);

            if (!string.IsNullOrEmpty(equipment))
                query = query.Where(t => t.Equipment == equipment);

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<string>> GetDistinctCategoriesAsync()
        {
            return await _dbSet
                .Where(t => t.Category != null)
                .Select(t => t.Category!)
                .Distinct()
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetDistinctMuscleGroupsAsync()
        {
            return await _dbSet
                .Where(t => t.MuscleGroup != null)
                .Select(t => t.MuscleGroup!)
                .Distinct()
                .ToListAsync();
        }

        public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
        {
            if (excludeId.HasValue)
            {
                return await _dbSet.AnyAsync(t => t.Name == name && t.Id != excludeId.Value);
            }
            return await _dbSet.AnyAsync(t => t.Name == name);
        }
    }
}
