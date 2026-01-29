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
    [Authorize]
    public class FriendsController : ControllerBase
    {
        private readonly TrainingContext _context;

        public FriendsController(TrainingContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim!);
        }

        /// <summary>
        /// Get all accepted friends
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FriendDto>>> GetFriends()
        {
            var userId = GetCurrentUserId();

            var friendships = await _context.Friendships
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .Where(f => f.Status == "accepted" &&
                           (f.RequesterId == userId || f.AddresseeId == userId))
                .ToListAsync();

            var friends = friendships.Select(f =>
            {
                var friend = f.RequesterId == userId ? f.Addressee : f.Requester;
                return new FriendDto
                {
                    UserId = friend.Id,
                    Username = friend.Username,
                    Name = friend.Name,
                    ProfilePhotoUrl = friend.ProfilePhotoUrl,
                    FriendsSince = f.RespondedAt ?? f.RequestedAt
                };
            });

            return Ok(friends);
        }

        /// <summary>
        /// Get incoming friend requests (pending)
        /// </summary>
        [HttpGet("requests/incoming")]
        public async Task<ActionResult<IEnumerable<FriendRequestDto>>> GetIncomingRequests()
        {
            var userId = GetCurrentUserId();

            var requests = await _context.Friendships
                .Include(f => f.Requester)
                .Where(f => f.AddresseeId == userId && f.Status == "pending")
                .OrderByDescending(f => f.RequestedAt)
                .Select(f => new FriendRequestDto
                {
                    FriendshipId = f.Id,
                    UserId = f.Requester.Id,
                    Username = f.Requester.Username,
                    Name = f.Requester.Name,
                    ProfilePhotoUrl = f.Requester.ProfilePhotoUrl,
                    RequestedAt = f.RequestedAt
                })
                .ToListAsync();

            return Ok(requests);
        }

        /// <summary>
        /// Get outgoing friend requests (pending)
        /// </summary>
        [HttpGet("requests/outgoing")]
        public async Task<ActionResult<IEnumerable<FriendRequestDto>>> GetOutgoingRequests()
        {
            var userId = GetCurrentUserId();

            var requests = await _context.Friendships
                .Include(f => f.Addressee)
                .Where(f => f.RequesterId == userId && f.Status == "pending")
                .OrderByDescending(f => f.RequestedAt)
                .Select(f => new FriendRequestDto
                {
                    FriendshipId = f.Id,
                    UserId = f.Addressee.Id,
                    Username = f.Addressee.Username,
                    Name = f.Addressee.Name,
                    ProfilePhotoUrl = f.Addressee.ProfilePhotoUrl,
                    RequestedAt = f.RequestedAt
                })
                .ToListAsync();

            return Ok(requests);
        }

        /// <summary>
        /// Send a friend request to a user
        /// </summary>
        [HttpPost("request/{targetUserId}")]
        public async Task<ActionResult> SendFriendRequest(int targetUserId)
        {
            var userId = GetCurrentUserId();

            if (userId == targetUserId)
            {
                return BadRequest(new { message = "Cannot send friend request to yourself" });
            }

            // Check if target user exists
            var targetUser = await _context.Users.FindAsync(targetUserId);
            if (targetUser == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Check if friendship already exists (in either direction)
            var existingFriendship = await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    (f.RequesterId == userId && f.AddresseeId == targetUserId) ||
                    (f.RequesterId == targetUserId && f.AddresseeId == userId));

            if (existingFriendship != null)
            {
                if (existingFriendship.Status == "accepted")
                {
                    return BadRequest(new { message = "Already friends with this user" });
                }
                if (existingFriendship.Status == "pending")
                {
                    return BadRequest(new { message = "Friend request already pending" });
                }
                if (existingFriendship.Status == "declined")
                {
                    // Update the declined request to pending (allow re-request)
                    existingFriendship.Status = "pending";
                    existingFriendship.RequesterId = userId;
                    existingFriendship.AddresseeId = targetUserId;
                    existingFriendship.RequestedAt = DateTime.UtcNow;
                    existingFriendship.RespondedAt = null;
                    await _context.SaveChangesAsync();
                    return Ok(new { message = "Friend request sent" });
                }
            }

            // Create new friendship request
            var friendship = new Friendship
            {
                RequesterId = userId,
                AddresseeId = targetUserId,
                Status = "pending",
                RequestedAt = DateTime.UtcNow
            };

            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Friend request sent", friendshipId = friendship.Id });
        }

        /// <summary>
        /// Accept a friend request
        /// </summary>
        [HttpPost("accept/{friendshipId}")]
        public async Task<ActionResult> AcceptFriendRequest(int friendshipId)
        {
            var userId = GetCurrentUserId();

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.Id == friendshipId && f.AddresseeId == userId);

            if (friendship == null)
            {
                return NotFound(new { message = "Friend request not found" });
            }

            if (friendship.Status != "pending")
            {
                return BadRequest(new { message = "Friend request is no longer pending" });
            }

            friendship.Status = "accepted";
            friendship.RespondedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Friend request accepted" });
        }

        /// <summary>
        /// Decline a friend request
        /// </summary>
        [HttpPost("decline/{friendshipId}")]
        public async Task<ActionResult> DeclineFriendRequest(int friendshipId)
        {
            var userId = GetCurrentUserId();

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.Id == friendshipId && f.AddresseeId == userId);

            if (friendship == null)
            {
                return NotFound(new { message = "Friend request not found" });
            }

            if (friendship.Status != "pending")
            {
                return BadRequest(new { message = "Friend request is no longer pending" });
            }

            friendship.Status = "declined";
            friendship.RespondedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Friend request declined" });
        }

        /// <summary>
        /// Remove a friend
        /// </summary>
        [HttpDelete("{friendId}")]
        public async Task<ActionResult> RemoveFriend(int friendId)
        {
            var userId = GetCurrentUserId();

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    f.Status == "accepted" &&
                    ((f.RequesterId == userId && f.AddresseeId == friendId) ||
                     (f.RequesterId == friendId && f.AddresseeId == userId)));

            if (friendship == null)
            {
                return NotFound(new { message = "Friendship not found" });
            }

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Friend removed" });
        }

        /// <summary>
        /// Cancel an outgoing friend request
        /// </summary>
        [HttpDelete("request/{friendshipId}")]
        public async Task<ActionResult> CancelFriendRequest(int friendshipId)
        {
            var userId = GetCurrentUserId();

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.Id == friendshipId && f.RequesterId == userId && f.Status == "pending");

            if (friendship == null)
            {
                return NotFound(new { message = "Friend request not found" });
            }

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Friend request cancelled" });
        }

        /// <summary>
        /// Check friendship status with a user
        /// </summary>
        [HttpGet("status/{targetUserId}")]
        public async Task<ActionResult<FriendshipStatusDto>> GetFriendshipStatus(int targetUserId)
        {
            var userId = GetCurrentUserId();

            if (userId == targetUserId)
            {
                return Ok(new FriendshipStatusDto { Status = "self" });
            }

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    (f.RequesterId == userId && f.AddresseeId == targetUserId) ||
                    (f.RequesterId == targetUserId && f.AddresseeId == userId));

            if (friendship == null)
            {
                return Ok(new FriendshipStatusDto { Status = "none" });
            }

            string status;
            if (friendship.Status == "accepted")
            {
                status = "friends";
            }
            else if (friendship.Status == "pending")
            {
                status = friendship.RequesterId == userId ? "pending_outgoing" : "pending_incoming";
            }
            else
            {
                status = "none";
            }

            return Ok(new FriendshipStatusDto
            {
                Status = status,
                FriendshipId = friendship.Id
            });
        }
    }

    public class FriendDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ProfilePhotoUrl { get; set; }
        public DateTime FriendsSince { get; set; }
    }

    public class FriendRequestDto
    {
        public int FriendshipId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ProfilePhotoUrl { get; set; }
        public DateTime RequestedAt { get; set; }
    }

    public class FriendshipStatusDto
    {
        public string Status { get; set; } = "none";
        public int? FriendshipId { get; set; }
    }
}
