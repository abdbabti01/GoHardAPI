using GoHardAPI.Models;

namespace GoHardAPI.Repositories
{
    /// <summary>
    /// Repository interface for Session entity with specialized queries.
    /// </summary>
    public interface ISessionRepository : IRepository<Session>
    {
        Task<IEnumerable<Session>> GetUserSessionsAsync(int userId);
        Task<Session?> GetUserSessionByIdAsync(int userId, int sessionId);
        Task<IEnumerable<Session>> GetUserSessionsWithExercisesAsync(int userId);
        Task<Session?> GetSessionWithExercisesAsync(int userId, int sessionId);
    }
}
