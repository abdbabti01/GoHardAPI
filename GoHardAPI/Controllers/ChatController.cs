using GoHardAPI.Data;
using GoHardAPI.DTOs;
using GoHardAPI.Models;
using GoHardAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace GoHardAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly TrainingContext _context;
        private readonly AIService _aiService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(TrainingContext context, AIService aiService, ILogger<ChatController> logger)
        {
            _context = context;
            _aiService = aiService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("User not authenticated");
            }
            return userId;
        }

        // GET: api/chat/conversations
        [HttpGet("conversations")]
        public async Task<ActionResult<IEnumerable<ConversationResponse>>> GetConversations()
        {
            var userId = GetCurrentUserId();
            var conversations = await _context.ChatConversations
                .Where(c => c.UserId == userId && !c.IsArchived)
                .Include(c => c.Messages)
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .Select(c => new ConversationResponse
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    Title = c.Title,
                    Type = c.Type,
                    CreatedAt = c.CreatedAt,
                    LastMessageAt = c.LastMessageAt,
                    MessageCount = c.Messages.Count,
                    IsArchived = c.IsArchived
                })
                .ToListAsync();

            return Ok(conversations);
        }

        // GET: api/chat/conversations/5
        [HttpGet("conversations/{id}")]
        public async Task<ActionResult<ConversationDetailResponse>> GetConversation(int id)
        {
            var userId = GetCurrentUserId();
            var conversation = await _context.ChatConversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (conversation == null)
            {
                return NotFound();
            }

            var response = new ConversationDetailResponse
            {
                Id = conversation.Id,
                UserId = conversation.UserId,
                Title = conversation.Title,
                Type = conversation.Type,
                CreatedAt = conversation.CreatedAt,
                LastMessageAt = conversation.LastMessageAt,
                IsArchived = conversation.IsArchived,
                Messages = conversation.Messages
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new MessageResponse
                    {
                        Id = m.Id,
                        ConversationId = m.ConversationId,
                        Role = m.Role,
                        Content = m.Content,
                        CreatedAt = m.CreatedAt,
                        InputTokens = m.InputTokens,
                        OutputTokens = m.OutputTokens,
                        Model = m.Model
                    })
                    .ToList()
            };

            return Ok(response);
        }

        // POST: api/chat/conversations
        [HttpPost("conversations")]
        public async Task<ActionResult<ConversationResponse>> CreateConversation(CreateConversationRequest request)
        {
            var userId = GetCurrentUserId();

            var conversation = new ChatConversation
            {
                UserId = userId,
                Title = request.Title,
                Type = request.Type,
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatConversations.Add(conversation);
            await _context.SaveChangesAsync();

            var response = new ConversationResponse
            {
                Id = conversation.Id,
                Title = conversation.Title,
                Type = conversation.Type,
                CreatedAt = conversation.CreatedAt,
                LastMessageAt = conversation.LastMessageAt,
                MessageCount = 0,
                IsArchived = conversation.IsArchived
            };

            return CreatedAtAction(nameof(GetConversation), new { id = conversation.Id }, response);
        }

        // DELETE: api/chat/conversations/5
        [HttpDelete("conversations/{id}")]
        public async Task<IActionResult> DeleteConversation(int id)
        {
            var userId = GetCurrentUserId();
            var conversation = await _context.ChatConversations.FindAsync(id);

            if (conversation == null || conversation.UserId != userId)
            {
                return NotFound();
            }

            _context.ChatConversations.Remove(conversation);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/chat/conversations/5/messages
        [HttpPost("conversations/{id}/messages")]
        public async Task<ActionResult<MessageResponse>> SendMessage(int id, SendMessageRequest request)
        {
            var userId = GetCurrentUserId();
            var conversation = await _context.ChatConversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (conversation == null)
            {
                return NotFound();
            }

            try
            {
                // Save user message
                var userMessage = new ChatMessage
                {
                    ConversationId = id,
                    Role = "user",
                    Content = request.Message,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ChatMessages.Add(userMessage);

                // Get AI response
                var aiResponse = await _aiService.SendMessageAsync(
                    request.Message,
                    conversation.Messages.ToList(),
                    conversation.Type
                );

                // Save AI message
                var aiMessage = new ChatMessage
                {
                    ConversationId = id,
                    Role = "assistant",
                    Content = aiResponse.Content,
                    CreatedAt = DateTime.UtcNow,
                    InputTokens = aiResponse.InputTokens,
                    OutputTokens = aiResponse.OutputTokens,
                    Model = aiResponse.Model
                };

                _context.ChatMessages.Add(aiMessage);

                // Update conversation last message time
                conversation.LastMessageAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new MessageResponse
                {
                    Id = aiMessage.Id,
                    Role = aiMessage.Role,
                    Content = aiMessage.Content,
                    CreatedAt = aiMessage.CreatedAt,
                    InputTokens = aiMessage.InputTokens,
                    OutputTokens = aiMessage.OutputTokens,
                    Model = aiMessage.Model
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to AI");
                return StatusCode(500, new { message = "Failed to get AI response", error = ex.Message });
            }
        }

        // POST: api/chat/conversations/5/messages/stream
        [HttpPost("conversations/{id}/messages/stream")]
        public async Task StreamMessage(int id, SendMessageRequest request)
        {
            var userId = GetCurrentUserId();
            var conversation = await _context.ChatConversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (conversation == null)
            {
                Response.StatusCode = 404;
                return;
            }

            // Set response headers for SSE (Server-Sent Events)
            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";

            try
            {
                // Save user message
                var userMessage = new ChatMessage
                {
                    ConversationId = id,
                    Role = "user",
                    Content = request.Message,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ChatMessages.Add(userMessage);
                await _context.SaveChangesAsync();

                var fullResponse = new StringBuilder();

                // Stream AI response
                await foreach (var chunk in _aiService.StreamMessageAsync(
                    request.Message,
                    conversation.Messages.ToList(),
                    conversation.Type
                ))
                {
                    fullResponse.Append(chunk);
                    await Response.WriteAsync($"data: {chunk}\n\n");
                    await Response.Body.FlushAsync();
                }

                // Send end marker
                await Response.WriteAsync("data: [DONE]\n\n");
                await Response.Body.FlushAsync();

                // Save complete AI message to database
                var aiMessage = new ChatMessage
                {
                    ConversationId = id,
                    Role = "assistant",
                    Content = fullResponse.ToString(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.ChatMessages.Add(aiMessage);
                conversation.LastMessageAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error streaming message");
                await Response.WriteAsync($"data: {{\"error\": \"{ex.Message}\"}}\n\n");
            }
        }

        // POST: api/chat/workout-plan
        [HttpPost("workout-plan")]
        public async Task<ActionResult<ConversationDetailResponse>> GenerateWorkoutPlan(GenerateWorkoutPlanRequest request)
        {
            var userId = GetCurrentUserId();

            // Create conversation
            var conversation = new ChatConversation
            {
                UserId = userId,
                Title = $"Workout Plan - {request.Goal}",
                Type = "workout_plan",
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatConversations.Add(conversation);
            await _context.SaveChangesAsync();

            // Build structured prompt from form data
            var prompt = $@"I need a personalized workout plan with the following details:

Goal: {request.Goal}
Experience Level: {request.ExperienceLevel}
Days Per Week: {request.DaysPerWeek}
Equipment Available: {request.Equipment}
{(!string.IsNullOrEmpty(request.Limitations) ? $"Limitations/Injuries: {request.Limitations}" : "")}

Please create a detailed workout plan that includes:
1. Weekly workout schedule
2. Specific exercises for each day
3. Sets and reps recommendations
4. Rest periods
5. Progression strategy
6. Any important notes or tips";

            try
            {
                // Save user message
                var userMessage = new ChatMessage
                {
                    ConversationId = conversation.Id,
                    Role = "user",
                    Content = prompt,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ChatMessages.Add(userMessage);

                // Get AI response
                var aiResponse = await _aiService.SendMessageAsync(
                    prompt,
                    new List<ChatMessage>(),
                    "workout_plan"
                );

                // Save AI message
                var aiMessage = new ChatMessage
                {
                    ConversationId = conversation.Id,
                    Role = "assistant",
                    Content = aiResponse.Content,
                    CreatedAt = DateTime.UtcNow,
                    InputTokens = aiResponse.InputTokens,
                    OutputTokens = aiResponse.OutputTokens,
                    Model = aiResponse.Model
                };

                _context.ChatMessages.Add(aiMessage);
                conversation.LastMessageAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Return conversation with messages
                return Ok(new ConversationDetailResponse
                {
                    Id = conversation.Id,
                    UserId = conversation.UserId,
                    Title = conversation.Title,
                    Type = conversation.Type,
                    CreatedAt = conversation.CreatedAt,
                    LastMessageAt = conversation.LastMessageAt,
                    IsArchived = conversation.IsArchived,
                    Messages = new List<MessageResponse>
                    {
                        new MessageResponse
                        {
                            Id = userMessage.Id,
                            ConversationId = userMessage.ConversationId,
                            Role = userMessage.Role,
                            Content = userMessage.Content,
                            CreatedAt = userMessage.CreatedAt
                        },
                        new MessageResponse
                        {
                            Id = aiMessage.Id,
                            ConversationId = aiMessage.ConversationId,
                            Role = aiMessage.Role,
                            Content = aiMessage.Content,
                            CreatedAt = aiMessage.CreatedAt,
                            InputTokens = aiMessage.InputTokens,
                            OutputTokens = aiMessage.OutputTokens,
                            Model = aiMessage.Model
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating workout plan");
                return StatusCode(500, new { message = "Failed to generate workout plan", error = ex.Message });
            }
        }

        // POST: api/chat/meal-plan
        [HttpPost("meal-plan")]
        public async Task<ActionResult<ConversationDetailResponse>> GenerateMealPlan(GenerateMealPlanRequest request)
        {
            var userId = GetCurrentUserId();

            // Create conversation
            var conversation = new ChatConversation
            {
                UserId = userId,
                Title = $"Meal Plan - {request.DietaryGoal}",
                Type = "meal_plan",
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatConversations.Add(conversation);
            await _context.SaveChangesAsync();

            // Build structured prompt from form data
            var prompt = $@"I need a personalized meal plan with the following details:

Dietary Goal: {request.DietaryGoal}
{(request.TargetCalories.HasValue ? $"Target Calories: {request.TargetCalories} per day" : "")}
{(!string.IsNullOrEmpty(request.Macros) ? $"Macro Split: {request.Macros}" : "")}
{(!string.IsNullOrEmpty(request.Restrictions) ? $"Dietary Restrictions: {request.Restrictions}" : "")}
{(!string.IsNullOrEmpty(request.Preferences) ? $"Preferences: {request.Preferences}" : "")}

Please create a detailed meal plan that includes:
1. Daily meal schedule (breakfast, lunch, dinner, snacks)
2. Specific meal ideas with approximate calories
3. Macro breakdown for each meal
4. Shopping list
5. Meal prep tips
6. Flexibility/substitution suggestions";

            try
            {
                // Save user message
                var userMessage = new ChatMessage
                {
                    ConversationId = conversation.Id,
                    Role = "user",
                    Content = prompt,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ChatMessages.Add(userMessage);

                // Get AI response
                var aiResponse = await _aiService.SendMessageAsync(
                    prompt,
                    new List<ChatMessage>(),
                    "meal_plan"
                );

                // Save AI message
                var aiMessage = new ChatMessage
                {
                    ConversationId = conversation.Id,
                    Role = "assistant",
                    Content = aiResponse.Content,
                    CreatedAt = DateTime.UtcNow,
                    InputTokens = aiResponse.InputTokens,
                    OutputTokens = aiResponse.OutputTokens,
                    Model = aiResponse.Model
                };

                _context.ChatMessages.Add(aiMessage);
                conversation.LastMessageAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Return conversation with messages
                return Ok(new ConversationDetailResponse
                {
                    Id = conversation.Id,
                    UserId = conversation.UserId,
                    Title = conversation.Title,
                    Type = conversation.Type,
                    CreatedAt = conversation.CreatedAt,
                    LastMessageAt = conversation.LastMessageAt,
                    IsArchived = conversation.IsArchived,
                    Messages = new List<MessageResponse>
                    {
                        new MessageResponse
                        {
                            Id = userMessage.Id,
                            ConversationId = userMessage.ConversationId,
                            Role = userMessage.Role,
                            Content = userMessage.Content,
                            CreatedAt = userMessage.CreatedAt
                        },
                        new MessageResponse
                        {
                            Id = aiMessage.Id,
                            ConversationId = aiMessage.ConversationId,
                            Role = aiMessage.Role,
                            Content = aiMessage.Content,
                            CreatedAt = aiMessage.CreatedAt,
                            InputTokens = aiMessage.InputTokens,
                            OutputTokens = aiMessage.OutputTokens,
                            Model = aiMessage.Model
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating meal plan");
                return StatusCode(500, new { message = "Failed to generate meal plan", error = ex.Message });
            }
        }

        // POST: api/chat/analyze-progress
        [HttpPost("analyze-progress")]
        public async Task<ActionResult<ConversationDetailResponse>> AnalyzeProgress(AnalyzeProgressRequest request)
        {
            var userId = GetCurrentUserId();

            // Fetch user's workout history
            var query = _context.Sessions
                .Where(s => s.UserId == userId)
                .Include(s => s.Exercises)
                .ThenInclude(e => e.ExerciseSets)
                .OrderByDescending(s => s.Date)
                .AsQueryable();

            if (request.StartDate.HasValue)
            {
                query = query.Where(s => s.Date >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                query = query.Where(s => s.Date <= request.EndDate.Value);
            }

            var sessions = await query.Take(50).ToListAsync(); // Limit to last 50 sessions

            // Build progress summary
            var progressSummary = new StringBuilder();
            progressSummary.AppendLine($"Total Sessions: {sessions.Count}");
            progressSummary.AppendLine($"Date Range: {sessions.LastOrDefault()?.Date:yyyy-MM-dd} to {sessions.FirstOrDefault()?.Date:yyyy-MM-dd}");
            progressSummary.AppendLine();

            // Group by exercise name
            var exerciseStats = sessions
                .SelectMany(s => s.Exercises)
                .GroupBy(e => e.Name)
                .Select(g => new
                {
                    Name = g.Key,
                    TotalSets = g.Sum(e => e.ExerciseSets?.Count ?? 0),
                    MaxWeight = g.SelectMany(e => e.ExerciseSets ?? new List<ExerciseSet>()).Max(s => s.Weight),
                    AvgWeight = g.SelectMany(e => e.ExerciseSets ?? new List<ExerciseSet>()).Average(s => s.Weight)
                })
                .OrderByDescending(x => x.TotalSets)
                .Take(10)
                .ToList();

            progressSummary.AppendLine("Top 10 Exercises by Volume:");
            foreach (var stat in exerciseStats)
            {
                progressSummary.AppendLine($"- {stat.Name}: {stat.TotalSets} sets, Max: {stat.MaxWeight}kg, Avg: {stat.AvgWeight:F1}kg");
            }

            // Create conversation
            var conversation = new ChatConversation
            {
                UserId = userId,
                Title = "Progress Analysis",
                Type = "progress_analysis",
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatConversations.Add(conversation);
            await _context.SaveChangesAsync();

            var prompt = $@"Please analyze my workout progress:

{progressSummary}

{(!string.IsNullOrEmpty(request.FocusArea) ? $"Focus Area: {request.FocusArea}" : "")}

Please provide:
1. Overall progress assessment
2. Strengths and areas for improvement
3. Suggestions for breaking through plateaus
4. Recommended focus areas for next phase
5. Any form or technique reminders";

            try
            {
                // Save user message
                var userMessage = new ChatMessage
                {
                    ConversationId = conversation.Id,
                    Role = "user",
                    Content = prompt,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ChatMessages.Add(userMessage);

                // Get AI response
                var aiResponse = await _aiService.SendMessageAsync(
                    prompt,
                    new List<ChatMessage>(),
                    "progress_analysis"
                );

                // Save AI message
                var aiMessage = new ChatMessage
                {
                    ConversationId = conversation.Id,
                    Role = "assistant",
                    Content = aiResponse.Content,
                    CreatedAt = DateTime.UtcNow,
                    InputTokens = aiResponse.InputTokens,
                    OutputTokens = aiResponse.OutputTokens,
                    Model = aiResponse.Model
                };

                _context.ChatMessages.Add(aiMessage);
                conversation.LastMessageAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Return conversation with messages
                return Ok(new ConversationDetailResponse
                {
                    Id = conversation.Id,
                    UserId = conversation.UserId,
                    Title = conversation.Title,
                    Type = conversation.Type,
                    CreatedAt = conversation.CreatedAt,
                    LastMessageAt = conversation.LastMessageAt,
                    IsArchived = conversation.IsArchived,
                    Messages = new List<MessageResponse>
                    {
                        new MessageResponse
                        {
                            Id = userMessage.Id,
                            ConversationId = userMessage.ConversationId,
                            Role = userMessage.Role,
                            Content = userMessage.Content,
                            CreatedAt = userMessage.CreatedAt
                        },
                        new MessageResponse
                        {
                            Id = aiMessage.Id,
                            ConversationId = aiMessage.ConversationId,
                            Role = aiMessage.Role,
                            Content = aiMessage.Content,
                            CreatedAt = aiMessage.CreatedAt,
                            InputTokens = aiMessage.InputTokens,
                            OutputTokens = aiMessage.OutputTokens,
                            Model = aiMessage.Model
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing progress");
                return StatusCode(500, new { message = "Failed to analyze progress", error = ex.Message });
            }
        }
    }
}
