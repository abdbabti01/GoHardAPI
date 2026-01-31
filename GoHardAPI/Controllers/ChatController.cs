using Asp.Versioning;
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
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
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
                return StatusCode(500, new { message = "Failed to get AI response. Please try again." });
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
                await Response.WriteAsync($"data: {{\"error\": \"An error occurred while processing your message. Please try again.\"}}\n\n");
            }
        }

        // POST: api/chat/workout-plan
        [HttpPost("workout-plan")]
        public async Task<ActionResult<ConversationDetailResponse>> GenerateWorkoutPlan(GenerateWorkoutPlanRequest request)
        {
            var userId = GetCurrentUserId();

            // Get user metrics for personalized recommendations
            var user = await _context.Users.FindAsync(userId);

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

            // Build user profile section if metrics are available
            var userProfileSection = "";
            if (user != null && (user.Weight.HasValue || user.Height.HasValue || user.DateOfBirth.HasValue))
            {
                var age = CalculateAge(user.DateOfBirth);
                var weightLbs = user.Weight.HasValue ? (user.Weight.Value * 2.205).ToString("F0") : "Not set";
                var weightKg = user.Weight.HasValue ? user.Weight.Value.ToString("F1") : "Not set";
                var heightIn = user.Height.HasValue ? (user.Height.Value / 2.54).ToString("F0") : "Not set";
                var heightCm = user.Height.HasValue ? user.Height.Value.ToString("F0") : "Not set";

                userProfileSection = $@"

**User Profile:**
- Weight: {weightKg}kg ({weightLbs}lbs)
- Height: {heightCm}cm ({heightIn} inches)
- Age: {age} years
- Gender: {user.Gender ?? "Not specified"}
- Body Fat: {(user.BodyFatPercentage.HasValue ? $"{user.BodyFatPercentage.Value}%" : "Not set")}
- Target Weight: {(user.TargetWeight.HasValue ? $"{user.TargetWeight.Value}kg" : "Not set")}
";
            }

            // Build structured prompt from form data
            var prompt = $@"I need a personalized workout plan with the following details:
{userProfileSection}
**Training Preferences:**
- Goal: {request.Goal}
- Experience Level: {request.ExperienceLevel}
- Days Per Week: {request.DaysPerWeek}
- Equipment Available: {request.Equipment}
{(!string.IsNullOrEmpty(request.Limitations) ? $"- Limitations/Injuries: {request.Limitations}" : "")}

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
                return StatusCode(500, new { message = "Failed to generate workout plan. Please try again." });
            }
        }

        // POST: api/chat/meal-plan
        [HttpPost("meal-plan")]
        public async Task<ActionResult<ConversationDetailResponse>> GenerateMealPlan(GenerateMealPlanRequest request)
        {
            var userId = GetCurrentUserId();

            // Get user metrics and nutrition goals for personalized recommendations
            var user = await _context.Users.FindAsync(userId);
            var nutritionGoal = await _context.NutritionGoals
                .Where(ng => ng.UserId == userId && ng.IsActive)
                .FirstOrDefaultAsync();

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

            // Build user profile section if metrics are available
            var userProfileSection = "";
            if (user != null && (user.Weight.HasValue || user.Height.HasValue || user.DateOfBirth.HasValue))
            {
                var age = CalculateAge(user.DateOfBirth);
                var weightLbs = user.Weight.HasValue ? (user.Weight.Value * 2.205).ToString("F0") : "Not set";
                var weightKg = user.Weight.HasValue ? user.Weight.Value.ToString("F1") : "Not set";
                var heightCm = user.Height.HasValue ? user.Height.Value.ToString("F0") : "Not set";

                userProfileSection = $@"

**User Profile:**
- Weight: {weightKg}kg ({weightLbs}lbs)
- Height: {heightCm}cm
- Age: {age} years
- Gender: {user.Gender ?? "Not specified"}
- Activity Level: {user.ActivityLevel ?? "Moderately Active"}
";
            }

            // Build nutrition targets section from active goal or request
            var nutritionTargetsSection = "";
            var targetCalories = request.TargetCalories ?? nutritionGoal?.DailyCalories;
            var targetProtein = nutritionGoal?.DailyProtein;
            var targetCarbs = nutritionGoal?.DailyCarbohydrates;
            var targetFat = nutritionGoal?.DailyFat;

            if (targetCalories.HasValue || targetProtein.HasValue)
            {
                nutritionTargetsSection = $@"

**Daily Nutrition Targets:**
{(targetCalories.HasValue ? $"- Target Calories: {targetCalories:F0} kcal/day" : "")}
{(targetProtein.HasValue ? $"- Target Protein: {targetProtein:F0}g" : "")}
{(targetCarbs.HasValue ? $"- Target Carbohydrates: {targetCarbs:F0}g" : "")}
{(targetFat.HasValue ? $"- Target Fat: {targetFat:F0}g" : "")}
";
            }

            // Build structured prompt from form data
            var prompt = $@"I need a personalized meal plan with the following details:
{userProfileSection}
**Dietary Goal:** {request.DietaryGoal}
{nutritionTargetsSection}
{(!string.IsNullOrEmpty(request.Macros) ? $"**Macro Split:** {request.Macros}" : "")}
{(!string.IsNullOrEmpty(request.Restrictions) ? $"**Dietary Restrictions:** {request.Restrictions}" : "")}
{(!string.IsNullOrEmpty(request.Preferences) ? $"**Preferences:** {request.Preferences}" : "")}

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
                return StatusCode(500, new { message = "Failed to generate meal plan. Please try again." });
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
                .Include(s => s.Exercises)
                    .ThenInclude(e => e.ExerciseTemplate)
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
                return StatusCode(500, new { message = "Failed to analyze progress. Please try again." });
            }
        }

        // GET: api/chat/conversations/5/preview-sessions
        [HttpGet("conversations/{id}/preview-sessions")]
        public async Task<ActionResult<object>> PreviewSessionsFromPlan(int id)
        {
            try
            {
                var userId = GetCurrentUserId();

                // First check if conversation exists
                var conversation = await _context.ChatConversations
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

                if (conversation == null)
                {
                    return NotFound(new { message = "Conversation not found" });
                }

                if (conversation.Type != "workout_plan")
                {
                    return BadRequest(new { message = "This is not a workout plan conversation" });
                }

                var workoutData = await ExtractWorkoutPlanData(id, userId);

                if (workoutData == null)
                {
                    return BadRequest(new { message = "Could not extract workout plan structure" });
                }

                // Return preview without creating sessions
                var preview = workoutData.Sessions?.Select((s, index) => new
                {
                    dayNumber = index + 1,
                    name = CleanWorkoutName(s.Name),
                    type = s.Type ?? "strength",
                    exerciseCount = s.Exercises?.Count ?? 0,
                    exercises = s.Exercises?.Select(e => new
                    {
                        name = e.Name,
                        sets = e.Sets,
                        reps = e.Reps,
                        weight = e.Weight,
                        restTime = e.RestTime
                    }).ToList()
                }).ToList();

                return Ok(new
                {
                    sessionsCount = workoutData.Sessions?.Count ?? 0,
                    sessions = preview
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing sessions from workout plan");
                return StatusCode(500, new { message = "Failed to preview sessions. Please try again." });
            }
        }

        // POST: api/chat/conversations/5/create-sessions
        [HttpPost("conversations/{id}/create-sessions")]
        public async Task<ActionResult<IEnumerable<object>>> CreateSessionsFromPlan(int id, [FromBody] CreateSessionsRequest? request = null)
        {
            try
            {
                var userId = GetCurrentUserId();

                // First check if conversation exists
                var conversation = await _context.ChatConversations
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

                if (conversation == null)
                {
                    return NotFound(new { message = "Conversation not found" });
                }

                if (conversation.Type != "workout_plan")
                {
                    return BadRequest(new { message = "This is not a workout plan conversation" });
                }

                var workoutData = await ExtractWorkoutPlanData(id, userId);

                if (workoutData == null)
                {
                    return BadRequest(new { message = "Could not extract workout plan structure" });
                }

                if (workoutData.Sessions == null || workoutData.Sessions.Count == 0)
                {
                    return BadRequest(new { message = "No workout sessions found in the plan" });
                }

                // Get all exercise templates for matching
                var templates = await _context.ExerciseTemplates.ToListAsync();

                // Use provided start date or default to today
                var baseDate = request?.StartDate?.Date ?? DateTime.UtcNow.Date;

                // Create sessions and exercises
                var createdSessions = new List<object>();
                var matchedTemplates = 0;

                for (int i = 0; i < workoutData.Sessions.Count; i++)
                {
                    var sessionData = workoutData.Sessions[i];

                    var session = new Models.Session
                    {
                        UserId = userId,
                        Name = CleanWorkoutName(sessionData.Name),
                        Type = sessionData.Type ?? "strength",
                        Status = "planned",
                        Date = baseDate.AddDays(i * 2), // Space out sessions every 2 days
                        Notes = sessionData.Notes,
                        Duration = 0
                    };

                    _context.Sessions.Add(session);
                    await _context.SaveChangesAsync(); // Save to get session ID

                    // Create exercises for this session
                    if (sessionData.Exercises != null)
                    {
                        foreach (var exerciseData in sessionData.Exercises)
                        {
                            // Try to match with existing exercise template
                            var matchedTemplate = FindBestMatchingTemplate(exerciseData.Name, templates);

                            var exercise = new Exercise
                            {
                                SessionId = session.Id,
                                Name = exerciseData.Name,
                                ExerciseTemplateId = matchedTemplate?.Id,
                                Notes = exerciseData.Notes,
                                RestTime = exerciseData.RestTime ?? 60,
                                Duration = 0
                            };

                            if (matchedTemplate != null)
                            {
                                matchedTemplates++;
                                _logger.LogInformation($"Matched '{exerciseData.Name}' to template '{matchedTemplate.Name}'");
                            }

                            _context.Exercises.Add(exercise);
                            await _context.SaveChangesAsync(); // Save to get exercise ID

                            // Create exercise sets
                            var sets = exerciseData.Sets ?? 3; // Default to 3 sets if not specified
                            var reps = exerciseData.Reps ?? 10; // Default to 10 reps if not specified

                            if (sets > 0 && reps > 0)
                            {
                                for (int setNum = 1; setNum <= sets; setNum++)
                                {
                                    var exerciseSet = new ExerciseSet
                                    {
                                        ExerciseId = exercise.Id,
                                        SetNumber = setNum,
                                        Reps = reps,
                                        Weight = exerciseData.Weight ?? 0,
                                        IsCompleted = false,
                                        Duration = 0
                                    };

                                    _context.ExerciseSets.Add(exerciseSet);
                                }
                            }
                        }
                    }

                    await _context.SaveChangesAsync();

                    createdSessions.Add(new
                    {
                        id = session.Id,
                        name = session.Name,
                        date = session.Date,
                        exerciseCount = sessionData.Exercises?.Count ?? 0
                    });
                }

                return Ok(new
                {
                    message = $"Successfully created {createdSessions.Count} workout sessions",
                    sessions = createdSessions,
                    matchedTemplates = matchedTemplates,
                    startDate = baseDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sessions from workout plan");
                return StatusCode(500, new { message = "Failed to create sessions. Please try again." });
            }
        }

        // POST: api/chat/conversations/5/create-program
        [HttpPost("conversations/{id}/create-program")]
        public async Task<ActionResult<object>> CreateProgramFromPlan(int id, [FromBody] CreateProgramRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();

                // First check if conversation exists
                var conversation = await _context.ChatConversations
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

                if (conversation == null)
                {
                    return NotFound(new { message = "Conversation not found" });
                }

                if (conversation.Type != "workout_plan")
                {
                    return BadRequest(new { message = "This is not a workout plan conversation" });
                }

                var workoutData = await ExtractWorkoutPlanData(id, userId);

                if (workoutData == null)
                {
                    return BadRequest(new { message = "Could not extract workout plan structure" });
                }

                if (workoutData.Sessions == null || workoutData.Sessions.Count == 0)
                {
                    return BadRequest(new { message = "No workout sessions found in the plan" });
                }

                // Calculate program duration
                var startDate = request.StartDate?.Date ?? DateTime.UtcNow.Date;
                var totalWeeks = request.TotalWeeks ?? CalculateWeeksFromSessions(workoutData.Sessions.Count);
                var endDate = startDate.AddDays(totalWeeks * 7);

                // Always start at Day 1 (session-based, not calendar)
                var currentDay = 1;

                // Create the program
                var program = new Models.Program
                {
                    UserId = userId,
                    Title = request.Title ?? conversation.Title ?? "My Workout Program",
                    Description = request.Description ?? "Generated from AI workout plan",
                    GoalId = request.GoalId,
                    TotalWeeks = totalWeeks,
                    CurrentWeek = 1,
                    CurrentDay = currentDay,
                    StartDate = startDate,
                    EndDate = endDate,
                    IsActive = true,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Programs.Add(program);
                await _context.SaveChangesAsync(); // Save to get program ID

                // Create program workouts from sessions
                var daysPerWeek = request.DaysPerWeek ?? Math.Min(workoutData.Sessions.Count, 5);
                var createdWorkouts = new List<object>();

                // Filter out rest days from AI sessions (sessions with no exercises or explicitly marked as rest)
                var workoutSessions = workoutData.Sessions
                    .Where(s => s.Exercises != null && s.Exercises.Count > 0)
                    .ToList();

                // Calculate total number of workout slots needed
                var totalWorkoutSlots = totalWeeks * daysPerWeek;

                // Cycle through AI sessions to fill all weeks
                var sessionIndex = 0;
                var currentDate = startDate;
                var weekNumber = 1;

                for (int weekDay = 1; weekDay <= totalWeeks * 7; weekDay++)
                {
                    var dayNumber = ((weekDay - 1) % 7) + 1; // 1=Monday, 7=Sunday

                    // Calculate which week we're in
                    weekNumber = ((weekDay - 1) / 7) + 1;

                    // Determine if this should be a workout day or rest day
                    // Distribute workout days evenly across the week
                    var workoutDaysThisWeek = (weekDay - 1) % 7 < daysPerWeek;

                    if (workoutDaysThisWeek && sessionIndex < workoutSessions.Count * totalWeeks)
                    {
                        // This is a workout day - use a session from AI (cycle through them)
                        var sessionData = workoutSessions[sessionIndex % workoutSessions.Count];
                        sessionIndex++;

                        // Convert exercises to JSON
                        string exercisesJson;
                        if (sessionData.Exercises != null && sessionData.Exercises.Count > 0)
                        {
                            var exercisesList = sessionData.Exercises.Select(e => new
                            {
                                name = e.Name,
                                sets = e.Sets,
                                reps = e.Reps,
                                weight = e.Weight,
                                rest = e.RestTime,
                                notes = e.Notes
                            }).ToList();
                            exercisesJson = System.Text.Json.JsonSerializer.Serialize(exercisesList);
                        }
                        else
                        {
                            exercisesJson = "[]";
                        }

                        var programWorkout = new ProgramWorkout
                        {
                            ProgramId = program.Id,
                            WeekNumber = weekNumber,
                            DayNumber = dayNumber,
                            DayName = GetDayName(dayNumber),
                            WorkoutName = CleanWorkoutName(sessionData.Name),
                            WorkoutType = sessionData.Type ?? "Strength",
                            ExercisesJson = exercisesJson,
                            WarmUp = null,
                            CoolDown = null,
                            EstimatedDuration = CalculateEstimatedDuration(sessionData.Exercises),
                            IsCompleted = false,
                            IsRestDay = false
                        };

                        _context.ProgramWorkouts.Add(programWorkout);

                        createdWorkouts.Add(new
                        {
                            weekNumber = weekNumber,
                            dayNumber = dayNumber,
                            name = programWorkout.WorkoutName,
                            exerciseCount = sessionData.Exercises?.Count ?? 0
                        });
                    }
                    else
                    {
                        // This is a rest day
                        var restWorkout = new ProgramWorkout
                        {
                            ProgramId = program.Id,
                            WeekNumber = weekNumber,
                            DayNumber = dayNumber,
                            DayName = GetDayName(dayNumber),
                            WorkoutName = "Rest Day",
                            WorkoutType = "Rest",
                            ExercisesJson = "[]",
                            WarmUp = null,
                            CoolDown = null,
                            EstimatedDuration = null,
                            IsCompleted = false,
                            IsRestDay = true
                        };

                        _context.ProgramWorkouts.Add(restWorkout);
                    }

                    currentDate = currentDate.AddDays(1);
                }

                await _context.SaveChangesAsync();

                // Reload program with workouts
                var createdProgram = await _context.Programs
                    .Include(p => p.Workouts)
                    .Include(p => p.Goal)
                    .FirstOrDefaultAsync(p => p.Id == program.Id);

                return Ok(new
                {
                    message = $"Successfully created program with {createdWorkouts.Count} workouts",
                    program = new
                    {
                        id = createdProgram!.Id,
                        title = createdProgram.Title,
                        totalWeeks = createdProgram.TotalWeeks,
                        startDate = createdProgram.StartDate,
                        workoutCount = createdWorkouts.Count
                    },
                    workouts = createdWorkouts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating program from workout plan");
                return StatusCode(500, new { message = "Failed to create program. Please try again." });
            }
        }

        // Helper method to calculate weeks needed based on number of sessions
        private int CalculateWeeksFromSessions(int sessionCount)
        {
            // Assume 4-5 workouts per week, calculate minimum weeks needed
            var weeksNeeded = (int)Math.Ceiling(sessionCount / 4.0);
            return Math.Max(weeksNeeded, 4); // Minimum 4 weeks
        }

        // Helper method to estimate workout duration
        private int? CalculateEstimatedDuration(List<ExerciseData>? exercises)
        {
            if (exercises == null || exercises.Count == 0)
            {
                return null;
            }

            // Rough estimate: 5 minutes per exercise + rest time
            var baseTime = exercises.Count * 5;
            var restTime = exercises.Sum(e => (e.Sets ?? 3) * (e.RestTime ?? 60)) / 60; // Convert to minutes
            return baseTime + restTime;
        }

        // Helper method to extract workout plan data from conversation
        private async Task<WorkoutPlanData?> ExtractWorkoutPlanData(int conversationId, int userId)
        {
            // Get the conversation
            var conversation = await _context.ChatConversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

            if (conversation == null || conversation.Type != "workout_plan")
            {
                _logger.LogWarning("Conversation not found or not a workout plan. Id: {id}, UserId: {userId}", conversationId, userId);
                return null;
            }

            // Get the AI's workout plan message
            var workoutPlanMessage = conversation.Messages
                .Where(m => m.Role == "assistant")
                .OrderBy(m => m.CreatedAt)
                .FirstOrDefault();

            if (workoutPlanMessage == null)
            {
                _logger.LogWarning("No assistant messages found in conversation {id}", conversationId);
                return null;
            }

            _logger.LogInformation("Found workout plan message with {length} characters", workoutPlanMessage.Content.Length);

            // Ask AI to extract structured workout data
            var extractionPrompt = @"Extract the workout plan from the previous message into structured JSON format.
Return ONLY valid JSON (no markdown, no explanations) with this exact structure:
{
  ""sessions"": [
    {
      ""name"": ""Day 1: Chest & Triceps"",
      ""type"": ""strength"",
      ""notes"": ""Focus on form and progressive overload"",
      ""exercises"": [
        {
          ""name"": ""Bench Press"",
          ""sets"": 4,
          ""reps"": 8,
          ""restTime"": 90,
          ""notes"": ""Warm up first""
        }
      ]
    }
  ]
}

IMPORTANT RULES:
- sets and reps MUST be integers (numbers). Use null if not specified.
- If reps says 'to failure' or similar, use null for reps
- restTime must be an integer (seconds) or null
- Do not use strings for numeric fields";

            var messages = new List<ChatMessage>
            {
                new ChatMessage
                {
                    Role = "assistant",
                    Content = workoutPlanMessage.Content
                }
            };

            var extractionResponse = await _aiService.SendMessageAsync(
                extractionPrompt,
                messages,
                "workout_plan"
            );

            _logger.LogInformation("AI extraction response length: {length}", extractionResponse.Content.Length);
            _logger.LogInformation("AI extraction response: {content}", extractionResponse.Content);

            // Parse the JSON response
            var jsonContent = extractionResponse.Content.Trim();

            // Remove markdown code blocks if present
            if (jsonContent.StartsWith("```"))
            {
                var lines = jsonContent.Split('\n');
                jsonContent = string.Join('\n', lines.Skip(1).Take(lines.Length - 2));
            }

            _logger.LogInformation("Cleaned JSON content: {json}", jsonContent);

            try
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var result = System.Text.Json.JsonSerializer.Deserialize<WorkoutPlanData>(jsonContent, options);
                _logger.LogInformation("Deserialization succeeded. Sessions count: {count}", result?.Sessions?.Count ?? 0);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize workout plan JSON: {json}", jsonContent);
                return null;
            }
        }

        // Helper method to find best matching exercise template using fuzzy matching
        private ExerciseTemplate? FindBestMatchingTemplate(string exerciseName, List<ExerciseTemplate> templates)
        {
            if (string.IsNullOrWhiteSpace(exerciseName))
            {
                return null;
            }

            var normalizedName = NormalizeExerciseName(exerciseName);

            // Try exact match first
            var exactMatch = templates.FirstOrDefault(t =>
                t.Name.Equals(exerciseName, StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
            {
                return exactMatch;
            }

            // Try normalized exact match
            var normalizedExactMatch = templates.FirstOrDefault(t =>
                NormalizeExerciseName(t.Name).Equals(normalizedName, StringComparison.OrdinalIgnoreCase));

            if (normalizedExactMatch != null)
            {
                return normalizedExactMatch;
            }

            // Try partial match (template name contains exercise name or vice versa)
            var partialMatch = templates.FirstOrDefault(t =>
                NormalizeExerciseName(t.Name).Contains(normalizedName) ||
                normalizedName.Contains(NormalizeExerciseName(t.Name)));

            if (partialMatch != null)
            {
                return partialMatch;
            }

            // Try matching common variations
            var variations = new Dictionary<string, string[]>
            {
                // Chest
                { "Bench Press", new[] { "bench", "barbell bench", "flat bench", "flat press", "chest press" } },
                { "Incline Dumbbell Press", new[] { "incline bench", "incline press", "incline db press" } },
                { "Push-ups", new[] { "pushup", "push up", "pushups", "press up", "press ups" } },
                { "Dumbbell Flyes", new[] { "fly", "flies", "chest fly", "pec fly", "db fly" } },
                { "Cable Crossovers", new[] { "cable fly", "cable crossover", "cable chest" } },
                // Back
                { "Deadlift", new[] { "deadlift", "conventional deadlift", "dl" } },
                { "Romanian Deadlift", new[] { "rdl", "stiff leg deadlift", "romanian dl" } },
                { "Pull-ups", new[] { "pullup", "pull up", "pullups", "chin up", "chinup" } },
                { "Bent-Over Row", new[] { "barbell row", "bent over row", "bb row", "bent row" } },
                { "Lat Pulldown", new[] { "lat pull", "pulldown", "pull down" } },
                { "Seated Cable Row", new[] { "cable row", "seated row", "low row" } },
                { "T-Bar Row", new[] { "t bar", "tbar", "landmine row" } },
                // Legs
                { "Squat", new[] { "squat", "back squat", "barbell squat", "bb squat" } },
                { "Leg Press", new[] { "leg press", "legpress" } },
                { "Lunges", new[] { "lunge", "walking lunge", "forward lunge" } },
                { "Bulgarian Split Squat", new[] { "split squat", "rear foot elevated" } },
                { "Leg Curl", new[] { "hamstring curl", "lying leg curl" } },
                { "Leg Extension", new[] { "quad extension", "knee extension" } },
                { "Calf Raises", new[] { "calf raise", "standing calf", "seated calf" } },
                { "Goblet Squat", new[] { "goblet", "db squat" } },
                // Shoulders
                { "Overhead Press", new[] { "ohp", "shoulder press", "military press", "strict press" } },
                { "Lateral Raises", new[] { "lateral raise", "side raise", "side lateral" } },
                { "Arnold Press", new[] { "arnold" } },
                { "Front Raises", new[] { "front raise", "front delt raise" } },
                { "Rear Delt Flyes", new[] { "rear delt", "reverse fly", "rear fly" } },
                { "Face Pulls", new[] { "face pull", "facepull" } },
                // Arms
                { "Bicep Curls", new[] { "bicep curl", "curl", "barbell curl", "db curl" } },
                { "Hammer Curls", new[] { "hammer curl", "neutral grip curl" } },
                { "Preacher Curls", new[] { "preacher curl", "preacher" } },
                { "Tricep Dips", new[] { "dip", "dips", "parallel bar dip" } },
                { "Tricep Pushdown", new[] { "pushdown", "cable pushdown", "tricep extension" } },
                { "Skull Crushers", new[] { "skull crusher", "lying tricep extension", "ez bar extension" } },
                // Core
                { "Plank", new[] { "plank", "front plank" } },
                { "Crunches", new[] { "crunch", "ab crunch" } },
                { "Russian Twists", new[] { "russian twist", "twist" } },
                { "Hanging Leg Raises", new[] { "leg raise", "hanging raise" } },
                // Cardio
                { "Running", new[] { "run", "jog", "jogging", "treadmill" } },
                { "Burpees", new[] { "burpee" } },
                { "Rowing Machine", new[] { "row", "erg", "rowing" } }
            };

            foreach (var (templateName, aliases) in variations)
            {
                if (aliases.Any(alias => normalizedName.Contains(alias) || alias.Contains(normalizedName)))
                {
                    var match = templates.FirstOrDefault(t => t.Name.Equals(templateName, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        return match;
                    }
                }
            }

            // Try word-based matching (if exercise name contains key words from template)
            var bestWordMatch = FindBestWordMatch(normalizedName, templates);
            if (bestWordMatch != null)
            {
                return bestWordMatch;
            }

            return null;
        }

        // Normalize exercise name by removing common equipment prefixes
        private string NormalizeExerciseName(string name)
        {
            var normalized = name.ToLowerInvariant().Trim();

            // Remove common equipment/modifier prefixes
            var prefixesToRemove = new[] {
                "barbell ", "dumbbell ", "db ", "bb ", "cable ", "machine ",
                "seated ", "standing ", "lying ", "incline ", "decline ", "flat ",
                "weighted ", "assisted ", "single arm ", "single leg "
            };

            foreach (var prefix in prefixesToRemove)
            {
                if (normalized.StartsWith(prefix))
                {
                    normalized = normalized.Substring(prefix.Length);
                }
            }

            return normalized;
        }

        // Find best match based on common words
        private ExerciseTemplate? FindBestWordMatch(string normalizedName, List<ExerciseTemplate> templates)
        {
            var inputWords = normalizedName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToHashSet();

            if (inputWords.Count == 0) return null;

            ExerciseTemplate? bestMatch = null;
            int bestScore = 0;

            foreach (var template in templates)
            {
                var templateWords = NormalizeExerciseName(template.Name)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 2)
                    .ToHashSet();

                // Count matching words
                var matchingWords = inputWords.Intersect(templateWords).Count();

                if (matchingWords > bestScore && matchingWords >= 1)
                {
                    bestScore = matchingWords;
                    bestMatch = template;
                }
            }

            // Only return if we have a good match (at least 1 significant word)
            return bestScore >= 1 ? bestMatch : null;
        }

        // Helper method to convert day number to day name
        private string GetDayName(int dayNumber)
        {
            return dayNumber switch
            {
                1 => "Monday",
                2 => "Tuesday",
                3 => "Wednesday",
                4 => "Thursday",
                5 => "Friday",
                6 => "Saturday",
                7 => "Sunday",
                _ => $"Day {dayNumber}"
            };
        }

        // Helper method to clean workout name by removing day prefixes
        // AI often generates names like "Day 1: Chest & Triceps" or "Monday: Upper Body"
        private string CleanWorkoutName(string workoutName)
        {
            if (string.IsNullOrWhiteSpace(workoutName))
            {
                return workoutName;
            }

            var colonIndex = workoutName.IndexOf(':');
            // Only strip if colon is within first 15 chars (likely a day prefix)
            if (colonIndex != -1 && colonIndex < 15)
            {
                var prefix = workoutName.Substring(0, colonIndex).ToLowerInvariant().Trim();

                // Check if prefix is a day name or day number pattern
                var dayPatterns = new[] {
                    "day 1", "day 2", "day 3", "day 4", "day 5", "day 6", "day 7",
                    "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday",
                    "day1", "day2", "day3", "day4", "day5", "day6", "day7"
                };

                if (dayPatterns.Any(pattern => prefix.StartsWith(pattern) || prefix == pattern))
                {
                    return workoutName.Substring(colonIndex + 1).Trim();
                }
            }

            return workoutName;
        }

        // Helper method to calculate age from date of birth
        private int CalculateAge(DateTime? dateOfBirth)
        {
            if (!dateOfBirth.HasValue) return 30; // Default age if not provided

            var today = DateTime.UtcNow;
            var age = today.Year - dateOfBirth.Value.Year;

            if (dateOfBirth.Value.Date > today.AddYears(-age))
                age--;

            return age;
        }

        // POST: api/chat/conversations/{id}/apply-meal-plan
        [HttpPost("conversations/{id}/apply-meal-plan")]
        public async Task<ActionResult<ApplyMealPlanResponse>> ApplyMealPlanToToday(int id)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Get the conversation
                var conversation = await _context.ChatConversations
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

                if (conversation == null)
                {
                    return NotFound(new { message = "Conversation not found" });
                }

                if (conversation.Type != "meal_plan")
                {
                    return BadRequest(new { message = "This is not a meal plan conversation" });
                }

                // Get the AI's meal plan message
                var mealPlanMessage = conversation.Messages
                    .Where(m => m.Role == "assistant")
                    .OrderBy(m => m.CreatedAt)
                    .FirstOrDefault();

                if (mealPlanMessage == null)
                {
                    return BadRequest(new { message = "No meal plan found in conversation" });
                }

                // Get user's nutrition goal for context
                var nutritionGoal = await _context.NutritionGoals
                    .Where(ng => ng.UserId == userId && ng.IsActive)
                    .FirstOrDefaultAsync();
                var targetCalories = nutritionGoal?.DailyCalories ?? 2000m;

                // Ask AI to extract structured meal data
                var extractionPrompt = $@"Extract the meals from the previous meal plan into structured JSON format.
The meal plan was designed for approximately {targetCalories:F0} calories per day.

Return ONLY valid JSON (no markdown, no explanations) with this exact structure:
{{
  ""meals"": [
    {{
      ""mealType"": ""Breakfast"",
      ""foods"": [
        {{
          ""name"": ""Oatmeal with Berries"",
          ""servingSize"": 1,
          ""servingUnit"": ""bowl"",
          ""calories"": 350,
          ""protein"": 12,
          ""carbohydrates"": 55,
          ""fat"": 8
        }}
      ]
    }},
    {{
      ""mealType"": ""Lunch"",
      ""foods"": [...]
    }},
    {{
      ""mealType"": ""Dinner"",
      ""foods"": [...]
    }},
    {{
      ""mealType"": ""Snack"",
      ""foods"": [...]
    }}
  ]
}}

CRITICAL RULES:
- mealType must be exactly: Breakfast, Lunch, Dinner, or Snack
- All numeric values must be numbers (not strings)
- Include all meals from the plan
- Calories are the TOTAL calories for one serving of that food item (NOT per 100g)
- The sum of all food calories should approximately match the daily target of {targetCalories:F0} kcal
- Typical food portions: oatmeal bowl ~300-400 kcal, chicken breast ~250-350 kcal, salad ~150-300 kcal
- If a single food item seems to have more than 800 calories, verify it's correct for a normal portion";

                var messages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        Role = "assistant",
                        Content = mealPlanMessage.Content
                    }
                };

                var extractionResponse = await _aiService.SendMessageAsync(
                    extractionPrompt,
                    messages,
                    "meal_plan"
                );

                // Parse the JSON response
                var jsonContent = extractionResponse.Content.Trim();

                // Remove markdown code blocks if present
                if (jsonContent.StartsWith("```"))
                {
                    var lines = jsonContent.Split('\n');
                    jsonContent = string.Join('\n', lines.Skip(1).Take(lines.Length - 2));
                }

                _logger.LogInformation("Extracted meal plan JSON: {json}", jsonContent);

                ChatMealPlanExtraction? mealPlanData;
                try
                {
                    var options = new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    mealPlanData = System.Text.Json.JsonSerializer.Deserialize<ChatMealPlanExtraction>(jsonContent, options);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse meal plan JSON: {json}", jsonContent);
                    return BadRequest(new { message = "Failed to parse meal plan structure" });
                }

                if (mealPlanData?.Meals == null || mealPlanData.Meals.Count == 0)
                {
                    return BadRequest(new { message = "No meals found in the meal plan" });
                }

                // Validate extracted values - check if total calories are reasonable
                var extractedTotalCalories = mealPlanData.Meals
                    .SelectMany(m => m.Foods ?? new List<ChatMealPlanFoodData>())
                    .Sum(f => f.Calories ?? 0);

                _logger.LogInformation("Extracted meal plan totals: {extractedCalories} kcal (target: {targetCalories} kcal)",
                    extractedTotalCalories, targetCalories);

                // If extracted calories are more than 3x the target, something went wrong
                // (likely AI confused per-100g with per-serving values)
                if (extractedTotalCalories > targetCalories * 3)
                {
                    _logger.LogWarning("Extracted calories ({extracted}) significantly exceed target ({target}). Scaling down values.",
                        extractedTotalCalories, targetCalories);

                    // Calculate scaling factor to bring values in line with target
                    var scaleFactor = targetCalories / extractedTotalCalories;

                    foreach (var meal in mealPlanData.Meals)
                    {
                        if (meal.Foods != null)
                        {
                            foreach (var food in meal.Foods)
                            {
                                food.Calories = food.Calories * scaleFactor;
                                food.Protein = food.Protein * scaleFactor;
                                food.Carbohydrates = food.Carbohydrates * scaleFactor;
                                food.Fat = food.Fat * scaleFactor;
                            }
                        }
                    }

                    _logger.LogInformation("Scaled meal plan values by factor {factor:F2}", scaleFactor);
                }

                // Calculate total macros from the meal plan
                decimal totalCalories = 0;
                decimal totalProtein = 0;
                decimal totalCarbs = 0;
                decimal totalFat = 0;

                foreach (var mealData in mealPlanData.Meals)
                {
                    foreach (var foodData in mealData.Foods ?? new List<ChatMealPlanFoodData>())
                    {
                        totalCalories += foodData.Calories ?? 0;
                        totalProtein += foodData.Protein ?? 0;
                        totalCarbs += foodData.Carbohydrates ?? 0;
                        totalFat += foodData.Fat ?? 0;
                    }
                }

                _logger.LogInformation("Meal plan totals - Calories: {cal}, Protein: {prot}g, Carbs: {carb}g, Fat: {fat}g",
                    totalCalories, totalProtein, totalCarbs, totalFat);

                // 1. UPDATE NUTRITION GOALS (replace, not add)
                if (nutritionGoal == null)
                {
                    // Create new nutrition goal
                    nutritionGoal = new Models.NutritionGoal
                    {
                        UserId = userId,
                        Name = "Meal Plan Goals",
                        DailyCalories = totalCalories,
                        DailyProtein = totalProtein,
                        DailyCarbohydrates = totalCarbs,
                        DailyFat = totalFat,
                        DailyWater = 2000, // Default water goal
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.NutritionGoals.Add(nutritionGoal);
                    _logger.LogInformation("Created new nutrition goal from meal plan for user {userId}", userId);
                }
                else
                {
                    // Update existing nutrition goal - use direct assignment to REPLACE values
                    nutritionGoal.DailyCalories = totalCalories;
                    nutritionGoal.DailyProtein = totalProtein;
                    nutritionGoal.DailyCarbohydrates = totalCarbs;
                    nutritionGoal.DailyFat = totalFat;
                    nutritionGoal.UpdatedAt = DateTime.UtcNow;
                    _context.NutritionGoals.Update(nutritionGoal); // Explicitly mark as updated
                    _logger.LogInformation("Updated nutrition goal from meal plan for user {userId}", userId);
                }

                await _context.SaveChangesAsync();

                // 2. GET OR CREATE TODAY'S MEAL LOG
                var today = DateTime.UtcNow.Date;
                var mealLog = await _context.MealLogs
                    .Include(ml => ml.MealEntries)
                    .ThenInclude(me => me.FoodItems)
                    .FirstOrDefaultAsync(ml => ml.UserId == userId && ml.Date.Date == today);

                if (mealLog == null)
                {
                    mealLog = new Models.MealLog
                    {
                        UserId = userId,
                        Date = today,
                        TotalCalories = 0,
                        TotalProtein = 0,
                        TotalCarbohydrates = 0,
                        TotalFat = 0,
                        WaterIntake = 0,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.MealLogs.Add(mealLog);
                    await _context.SaveChangesAsync();
                }

                // Ensure meal entries exist for each meal type
                var mealTypes = new[] { "Breakfast", "Lunch", "Dinner", "Snack" };
                foreach (var mealType in mealTypes)
                {
                    if (!mealLog.MealEntries.Any(me => me.MealType == mealType))
                    {
                        var mealEntry = new Models.MealEntry
                        {
                            MealLogId = mealLog.Id,
                            MealType = mealType,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.MealEntries.Add(mealEntry);
                        mealLog.MealEntries.Add(mealEntry);
                    }
                }
                await _context.SaveChangesAsync();

                // 3. CLEAR EXISTING FOOD ITEMS (to prevent accumulation on re-apply)
                foreach (var entry in mealLog.MealEntries)
                {
                    if (entry.FoodItems.Any())
                    {
                        _context.FoodItems.RemoveRange(entry.FoodItems);
                    }
                }
                await _context.SaveChangesAsync();

                // Reload meal entries after clearing
                mealLog = await _context.MealLogs
                    .Include(ml => ml.MealEntries)
                    .FirstOrDefaultAsync(ml => ml.Id == mealLog.Id);

                // 4. ADD FOOD ITEMS FROM MEAL PLAN
                var addedFoods = new List<object>();
                decimal totalCaloriesAdded = 0;
                decimal totalProteinAdded = 0;
                decimal totalCarbsAdded = 0;
                decimal totalFatAdded = 0;

                foreach (var mealData in mealPlanData.Meals)
                {
                    var mealEntry = mealLog!.MealEntries.FirstOrDefault(me =>
                        me.MealType.Equals(mealData.MealType, StringComparison.OrdinalIgnoreCase));

                    if (mealEntry == null) continue;

                    foreach (var foodData in mealData.Foods ?? new List<ChatMealPlanFoodData>())
                    {
                        var foodItem = new Models.FoodItem
                        {
                            MealEntryId = mealEntry.Id,
                            Name = foodData.Name ?? "Unknown",
                            Quantity = 1,
                            ServingSize = foodData.ServingSize ?? 1,
                            ServingUnit = foodData.ServingUnit ?? "serving",
                            Calories = foodData.Calories ?? 0,
                            Protein = foodData.Protein ?? 0,
                            Carbohydrates = foodData.Carbohydrates ?? 0,
                            Fat = foodData.Fat ?? 0,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.FoodItems.Add(foodItem);

                        totalCaloriesAdded += foodItem.Calories;
                        totalProteinAdded += foodItem.Protein;
                        totalCarbsAdded += foodItem.Carbohydrates;
                        totalFatAdded += foodItem.Fat;

                        addedFoods.Add(new
                        {
                            mealType = mealData.MealType,
                            name = foodItem.Name,
                            calories = foodItem.Calories
                        });
                    }
                }

                // 5. UPDATE MEAL ENTRY TOTALS (but keep IsConsumed = false, so they're "Planned")
                // Reload meal entries to update their totals
                var updatedMealLog = await _context.MealLogs
                    .Include(ml => ml.MealEntries)
                    .ThenInclude(me => me.FoodItems)
                    .FirstOrDefaultAsync(ml => ml.Id == mealLog!.Id);

                if (updatedMealLog != null)
                {
                    foreach (var entry in updatedMealLog.MealEntries)
                    {
                        // Update entry totals from food items
                        entry.TotalCalories = entry.FoodItems.Sum(f => f.Calories);
                        entry.TotalProtein = entry.FoodItems.Sum(f => f.Protein);
                        entry.TotalCarbohydrates = entry.FoodItems.Sum(f => f.Carbohydrates);
                        entry.TotalFat = entry.FoodItems.Sum(f => f.Fat);
                        // Keep IsConsumed = false (Planned status)
                        entry.IsConsumed = false;
                        entry.ConsumedAt = null;
                    }

                    // MealLog totals should only count CONSUMED meals, so keep at 0
                    // (or recalculate from only consumed entries)
                    updatedMealLog.TotalCalories = updatedMealLog.MealEntries
                        .Where(e => e.IsConsumed)
                        .Sum(e => e.TotalCalories);
                    updatedMealLog.TotalProtein = updatedMealLog.MealEntries
                        .Where(e => e.IsConsumed)
                        .Sum(e => e.TotalProtein);
                    updatedMealLog.TotalCarbohydrates = updatedMealLog.MealEntries
                        .Where(e => e.IsConsumed)
                        .Sum(e => e.TotalCarbohydrates);
                    updatedMealLog.TotalFat = updatedMealLog.MealEntries
                        .Where(e => e.IsConsumed)
                        .Sum(e => e.TotalFat);
                    updatedMealLog.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Ok(new ApplyMealPlanResponse
                {
                    Success = true,
                    Message = $"Nutrition goals updated and {addedFoods.Count} foods added to today's log",
                    FoodsAdded = addedFoods.Count,
                    TotalCaloriesAdded = totalCaloriesAdded,
                    TotalProteinAdded = totalProteinAdded,
                    TotalCarbsAdded = totalCarbsAdded,
                    TotalFatAdded = totalFatAdded,
                    Foods = addedFoods
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying meal plan to today");
                return StatusCode(500, new { message = "Failed to apply meal plan. Please try again." });
            }
        }

        // POST: api/chat/food-suggestion
        [HttpPost("food-suggestion")]
        public async Task<ActionResult<FoodSuggestionResponse>> SuggestFoodAlternatives(FoodSuggestionRequest request)
        {
            try
            {
                var prompt = $@"I need healthy food alternatives for: {request.FoodName}

Current nutritional values (per serving):
- Calories: {request.Calories} kcal
- Protein: {request.Protein}g
- Carbs: {request.Carbohydrates}g
- Fat: {request.Fat}g

Please suggest 3-5 alternative foods that:
1. Have similar macronutrients (within 20% variance)
2. Are healthy and commonly available
3. Could be used as a substitute in similar meals

Respond ONLY with valid JSON (no markdown, no explanation) in this exact format:
{{
  ""alternatives"": [
    {{
      ""name"": ""Food Name"",
      ""servingSize"": 100,
      ""servingUnit"": ""g"",
      ""calories"": 150,
      ""protein"": 10,
      ""carbohydrates"": 15,
      ""fat"": 5,
      ""reason"": ""Brief reason why this is a good alternative""
    }}
  ]
}}";

                // Get AI response without creating a conversation
                var aiResponse = await _aiService.SendMessageAsync(
                    prompt,
                    new List<ChatMessage>(),
                    "nutrition"
                );

                // Parse the JSON response
                var jsonContent = aiResponse.Content.Trim();

                // Remove markdown code blocks if present
                if (jsonContent.StartsWith("```"))
                {
                    var lines = jsonContent.Split('\n');
                    jsonContent = string.Join('\n', lines.Skip(1).Take(lines.Length - 2));
                }

                try
                {
                    var options = new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var result = System.Text.Json.JsonSerializer.Deserialize<FoodSuggestionResponse>(jsonContent, options);
                    return Ok(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse food suggestion JSON: {json}", jsonContent);
                    // Return the raw response as a fallback
                    return Ok(new FoodSuggestionResponse
                    {
                        Alternatives = new List<FoodAlternative>(),
                        RawResponse = aiResponse.Content
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating food suggestions");
                return StatusCode(500, new { message = "Failed to generate food suggestions. Please try again." });
            }
        }
    }

    // Request DTO for food suggestion
    public class FoodSuggestionRequest
    {
        public string FoodName { get; set; } = "";
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Carbohydrates { get; set; }
        public double Fat { get; set; }
    }

    // Response DTO for food suggestion
    public class FoodSuggestionResponse
    {
        public List<FoodAlternative> Alternatives { get; set; } = new();
        public string? RawResponse { get; set; }
    }

    public class FoodAlternative
    {
        public string Name { get; set; } = "";
        public double ServingSize { get; set; }
        public string ServingUnit { get; set; } = "g";
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Carbohydrates { get; set; }
        public double Fat { get; set; }
        public string? Reason { get; set; }
    }

    // Request DTO for creating sessions
    public class CreateSessionsRequest
    {
        public DateTime? StartDate { get; set; }
    }

    // Request DTO for creating programs
    public class CreateProgramRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? GoalId { get; set; }
        public int? TotalWeeks { get; set; }
        public int? DaysPerWeek { get; set; }
        public DateTime? StartDate { get; set; }
    }

    // Helper classes for JSON parsing
    public class WorkoutPlanData
    {
        public List<SessionData>? Sessions { get; set; }
    }

    public class SessionData
    {
        public string Name { get; set; } = "";
        public string? Type { get; set; }
        public string? Notes { get; set; }
        public List<ExerciseData>? Exercises { get; set; }
    }

    public class ExerciseData
    {
        public string Name { get; set; } = "";
        public int? Sets { get; set; }
        public int? Reps { get; set; }
        public double? Weight { get; set; }
        public int? RestTime { get; set; }
        public string? Notes { get; set; }
    }

    // DTOs for meal plan extraction from chat
    public class ChatMealPlanExtraction
    {
        public List<ChatMealPlanMealData> Meals { get; set; } = new();
    }

    public class ChatMealPlanMealData
    {
        public string MealType { get; set; } = "";
        public List<ChatMealPlanFoodData>? Foods { get; set; }
    }

    public class ChatMealPlanFoodData
    {
        public string? Name { get; set; }
        public decimal? ServingSize { get; set; }
        public string? ServingUnit { get; set; }
        public decimal? Calories { get; set; }
        public decimal? Protein { get; set; }
        public decimal? Carbohydrates { get; set; }
        public decimal? Fat { get; set; }
    }

    public class ApplyMealPlanResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int FoodsAdded { get; set; }
        public decimal TotalCaloriesAdded { get; set; }
        public decimal TotalProteinAdded { get; set; }
        public decimal TotalCarbsAdded { get; set; }
        public decimal TotalFatAdded { get; set; }
        public object? Foods { get; set; }
    }
}
