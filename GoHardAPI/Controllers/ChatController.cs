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
                return StatusCode(500, new { message = "Failed to generate workout plan. Please try again." });
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
}
