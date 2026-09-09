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

        /// <summary>
        /// True when an account already holds <paramref name="username"/>. Pass
        /// <paramref name="excludeUserId"/> (the caller's own id) so a profile
        /// edit that only re-cases the caller's existing username - e.g.
        /// "alice" -&gt; "Alice" on a case-insensitive collation - is not
        /// reported as a collision with the caller's own row. The comparison is
        /// unchanged (ordinal <c>==</c>; the database collation applies),
        /// identical to the signup check.
        /// </summary>
        Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null);
    }
}
