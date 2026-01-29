using Asp.Versioning;
using GoHardAPI.Data;
using GoHardAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GoHardAPI.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly TrainingContext _context;

        public UsersController(TrainingContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim!);
        }

        /// <summary>
        /// Search users by username (partial match)
        /// </summary>
        [HttpGet("search")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<UserSearchResultDto>>> SearchUsers([FromQuery] string username)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length < 2)
            {
                return BadRequest(new { message = "Search query must be at least 2 characters" });
            }

            var userId = GetCurrentUserId();
            var searchTerm = username.ToLower();

            var users = await _context.Users
                .Where(u => u.Id != userId && u.IsActive && u.Username.ToLower().Contains(searchTerm))
                .Take(20)
                .Select(u => new UserSearchResultDto
                {
                    UserId = u.Id,
                    Username = u.Username,
                    Name = u.Name,
                    ProfilePhotoUrl = u.ProfilePhotoUrl
                })
                .ToListAsync();

            return Ok(users);
        }

        /// <summary>
        /// Get public profile of a user (limited info for non-friends)
        /// </summary>
        [HttpGet("{id}/public-profile")]
        [Authorize]
        public async Task<ActionResult<PublicProfileDto>> GetPublicProfile(int id)
        {
            var userId = GetCurrentUserId();

            var user = await _context.Users.FindAsync(id);
            if (user == null || !user.IsActive)
            {
                return NotFound(new { message = "User not found" });
            }

            // Check if they're friends
            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    f.Status == "accepted" &&
                    ((f.RequesterId == userId && f.AddresseeId == id) ||
                     (f.RequesterId == id && f.AddresseeId == userId)));

            var isFriend = friendship != null;

            // Get shared workouts count (public)
            var sharedWorkoutsCount = await _context.SharedWorkouts
                .Where(sw => sw.SharedByUserId == id)
                .CountAsync();

            // Get total workouts count
            var totalWorkoutsCount = await _context.Sessions
                .Where(s => s.UserId == id && s.Status == "completed")
                .CountAsync();

            var profile = new PublicProfileDto
            {
                UserId = user.Id,
                Username = user.Username,
                Name = user.Name,
                ProfilePhotoUrl = user.ProfilePhotoUrl,
                Bio = user.Bio,
                ExperienceLevel = user.ExperienceLevel,
                MemberSince = user.DateCreated,
                IsFriend = isFriend,
                SharedWorkoutsCount = sharedWorkoutsCount,
                TotalWorkoutsCount = isFriend ? totalWorkoutsCount : null
            };

            return Ok(profile);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, User user)
        {
            if (id != user.Id)
            {
                return BadRequest();
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class UserSearchResultDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ProfilePhotoUrl { get; set; }
    }

    public class PublicProfileDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ProfilePhotoUrl { get; set; }
        public string? Bio { get; set; }
        public string? ExperienceLevel { get; set; }
        public DateTime MemberSince { get; set; }
        public bool IsFriend { get; set; }
        public int SharedWorkoutsCount { get; set; }
        public int? TotalWorkoutsCount { get; set; } // Only visible to friends
    }
}
