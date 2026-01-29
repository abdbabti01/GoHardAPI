using GoHardAPI.Models;

namespace GoHardAPI.Repositories
{
    /// <summary>
    /// Repository interface for User entity with specialized queries.
    /// </summary>
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<bool> EmailExistsAsync(string email);
        Task<User?> GetByUsernameAsync(string username);
        Task<bool> UsernameExistsAsync(string username);
    }
}
