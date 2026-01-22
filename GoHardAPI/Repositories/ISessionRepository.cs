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
        /// <summary>
        /// Gets sessions with Exercises, ExerciseSets, and ExerciseTemplate included.
        /// </summary>
        Task<IEnumerable<Session>> GetUserSessionsWithExercisesAsync(int userId);
        /// <summary>
        /// Gets a single session with Exercises, ExerciseSets, and ExerciseTemplate included.
        /// </summary>
        Task<Session?> GetSessionWithExercisesAsync(int userId, int sessionId);
        /// <summary>
        /// Gets completed sessions with all exercise data included.
        /// </summary>
        Task<IEnumerable<Session>> GetCompletedSessionsAsync(int userId);
    }
}
