using GoHardAPI.Data;
using GoHardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GoHardAPI.Repositories
{
    /// <summary>
    /// Repository implementation for Session entity.
    /// </summary>
    public class SessionRepository : Repository<Session>, ISessionRepository
    {
        public SessionRepository(TrainingContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Session>> GetUserSessionsAsync(int userId)
        {
            return await _dbSet
                .Where(s => s.UserId == userId)
                .ToListAsync();
        }

        public async Task<Session?> GetUserSessionByIdAsync(int userId, int sessionId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);
        }

        public async Task<IEnumerable<Session>> GetUserSessionsWithExercisesAsync(int userId)
        {
            return await _dbSet
                .Where(s => s.UserId == userId)
                .Include(s => s.Exercises)
                    .ThenInclude(e => e.ExerciseSets)
                .Include(s => s.Exercises)
                    .ThenInclude(e => e.ExerciseTemplate)
                .ToListAsync();
        }

        public async Task<Session?> GetSessionWithExercisesAsync(int userId, int sessionId)
        {
            return await _dbSet
                .Include(s => s.Exercises)
                    .ThenInclude(e => e.ExerciseSets)
                .Include(s => s.Exercises)
                    .ThenInclude(e => e.ExerciseTemplate)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);
        }

        public async Task<IEnumerable<Session>> GetCompletedSessionsAsync(int userId)
        {
            return await _dbSet
                .Where(s => s.UserId == userId && s.Status == SessionStatus.Completed)
                .Include(s => s.Exercises)
                    .ThenInclude(e => e.ExerciseSets)
                .Include(s => s.Exercises)
                    .ThenInclude(e => e.ExerciseTemplate)
                .OrderByDescending(s => s.Date)
                .ToListAsync();
        }
    }
}
