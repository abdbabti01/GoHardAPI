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
    [Route("api/v{version:apiVersion}/dm")]
    [ApiController]
    [Authorize]
    public class DirectMessagesController : ControllerBase
    {
        private readonly TrainingContext _context;

        public DirectMessagesController(TrainingContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim!);
        }

        private (int user1Id, int user2Id) GetConversationUserIds(int userId, int otherUserId)
        {
            // Always store lower ID as User1 for consistent lookups
            return userId < otherUserId ? (userId, otherUserId) : (otherUserId, userId);
        }

        /// <summary>
        /// Get all conversations for the current user
        /// </summary>
        [HttpGet("conversations")]
        public async Task<ActionResult<IEnumerable<ConversationDto>>> GetConversations()
        {
            var userId = GetCurrentUserId();

            var conversations = await _context.DMConversations
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();

            var result = new List<ConversationDto>();

            foreach (var conv in conversations)
            {
                var friend = conv.User1Id == userId ? conv.User2 : conv.User1;
                var unreadCount = conv.User1Id == userId ? conv.UnreadCountUser1 : conv.UnreadCountUser2;

                // Get the last message
                var lastMessage = await _context.DirectMessages
                    .Where(dm => (dm.SenderId == userId && dm.ReceiverId == friend.Id) ||
                                 (dm.SenderId == friend.Id && dm.ReceiverId == userId))
                    .OrderByDescending(dm => dm.SentAt)
                    .FirstOrDefaultAsync();

                result.Add(new ConversationDto
                {
                    FriendId = friend.Id,
                    FriendUsername = friend.Username,
                    FriendName = friend.Name,
                    FriendPhotoUrl = friend.ProfilePhotoUrl,
                    LastMessage = lastMessage?.Content,
                    LastMessageAt = lastMessage?.SentAt,
                    UnreadCount = unreadCount
                });
            }

            return Ok(result);
        }

        /// <summary>
        /// Get messages in a conversation with a friend (paginated)
        /// </summary>
        [HttpGet("conversations/{friendId}/messages")]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessages(
            int friendId,
            [FromQuery] int? beforeId = null,
            [FromQuery] int limit = 50)
        {
            var userId = GetCurrentUserId();

            // Verify friendship exists
            var areFriends = await _context.Friendships
                .AnyAsync(f => f.Status == "accepted" &&
                    ((f.RequesterId == userId && f.AddresseeId == friendId) ||
                     (f.RequesterId == friendId && f.AddresseeId == userId)));

            if (!areFriends)
            {
                return Forbid();
            }

            var query = _context.DirectMessages
                .Where(dm => (dm.SenderId == userId && dm.ReceiverId == friendId) ||
                             (dm.SenderId == friendId && dm.ReceiverId == userId));

            if (beforeId.HasValue)
            {
                query = query.Where(dm => dm.Id < beforeId.Value);
            }

            var messages = await query
                .OrderByDescending(dm => dm.SentAt)
                .Take(Math.Min(limit, 100))
                .Select(dm => new MessageDto
                {
                    Id = dm.Id,
                    SenderId = dm.SenderId,
                    Content = dm.Content,
                    SentAt = dm.SentAt,
                    ReadAt = dm.ReadAt,
                    IsFromMe = dm.SenderId == userId
                })
                .ToListAsync();

            // Return in chronological order
            messages.Reverse();

            return Ok(messages);
        }

        /// <summary>
        /// Send a message to a friend
        /// </summary>
        [HttpPost("conversations/{friendId}/messages")]
        public async Task<ActionResult<MessageDto>> SendMessage(int friendId, [FromBody] SendDMRequest request)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { message = "Message content cannot be empty" });
            }

            // Verify friendship exists
            var areFriends = await _context.Friendships
                .AnyAsync(f => f.Status == "accepted" &&
                    ((f.RequesterId == userId && f.AddresseeId == friendId) ||
                     (f.RequesterId == friendId && f.AddresseeId == userId)));

            if (!areFriends)
            {
                return Forbid();
            }

            // Create the message
            var message = new DirectMessage
            {
                SenderId = userId,
                ReceiverId = friendId,
                Content = request.Content.Trim(),
                SentAt = DateTime.UtcNow
            };

            _context.DirectMessages.Add(message);

            // Get or create conversation
            var (user1Id, user2Id) = GetConversationUserIds(userId, friendId);
            var conversation = await _context.DMConversations
                .FirstOrDefaultAsync(c => c.User1Id == user1Id && c.User2Id == user2Id);

            if (conversation == null)
            {
                conversation = new DMConversation
                {
                    User1Id = user1Id,
                    User2Id = user2Id,
                    LastMessageAt = DateTime.UtcNow
                };
                _context.DMConversations.Add(conversation);
            }
            else
            {
                conversation.LastMessageAt = DateTime.UtcNow;
            }

            // Increment unread count for the recipient
            if (friendId == user1Id)
            {
                conversation.UnreadCountUser1++;
            }
            else
            {
                conversation.UnreadCountUser2++;
            }

            await _context.SaveChangesAsync();

            return Ok(new MessageDto
            {
                Id = message.Id,
                SenderId = message.SenderId,
                Content = message.Content,
                SentAt = message.SentAt,
                ReadAt = message.ReadAt,
                IsFromMe = true
            });
        }

        /// <summary>
        /// Mark messages from a friend as read
        /// </summary>
        [HttpPost("conversations/{friendId}/read")]
        public async Task<ActionResult> MarkAsRead(int friendId)
        {
            var userId = GetCurrentUserId();

            // Update unread messages
            var unreadMessages = await _context.DirectMessages
                .Where(dm => dm.SenderId == friendId && dm.ReceiverId == userId && dm.ReadAt == null)
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var message in unreadMessages)
            {
                message.ReadAt = now;
            }

            // Reset unread count in conversation
            var (user1Id, user2Id) = GetConversationUserIds(userId, friendId);
            var conversation = await _context.DMConversations
                .FirstOrDefaultAsync(c => c.User1Id == user1Id && c.User2Id == user2Id);

            if (conversation != null)
            {
                if (userId == user1Id)
                {
                    conversation.UnreadCountUser1 = 0;
                }
                else
                {
                    conversation.UnreadCountUser2 = 0;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Messages marked as read", count = unreadMessages.Count });
        }

        /// <summary>
        /// Get total unread message count across all conversations
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<ActionResult<UnreadCountDto>> GetUnreadCount()
        {
            var userId = GetCurrentUserId();

            var totalUnread = await _context.DMConversations
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .SumAsync(c => c.User1Id == userId ? c.UnreadCountUser1 : c.UnreadCountUser2);

            return Ok(new UnreadCountDto { UnreadCount = totalUnread });
        }
    }

    public class ConversationDto
    {
        public int FriendId { get; set; }
        public string FriendUsername { get; set; } = string.Empty;
        public string FriendName { get; set; } = string.Empty;
        public string? FriendPhotoUrl { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
    }

    public class MessageDto
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsFromMe { get; set; }
    }

    public class SendDMRequest
    {
        public string Content { get; set; } = string.Empty;
    }

    public class UnreadCountDto
    {
        public int UnreadCount { get; set; }
    }
}
