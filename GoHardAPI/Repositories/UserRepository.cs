using GoHardAPI.Data;
using GoHardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GoHardAPI.Repositories
{
    /// <summary>
    /// Repository implementation for User entity.
    /// </summary>
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(TrainingContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _dbSet.AnyAsync(u => u.Email == email);
        }
    }
}
