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
        private readonly CurrentMeasurementsService _currentMeasurements;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            TrainingContext context,
            AIService aiService,
            CurrentMeasurementsService currentMeasurements,
            ILogger<ChatController> logger)
        {
            _context = context;
            _aiService = aiService;
            _currentMeasurements = currentMeasurements;
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
                        Model = m.Model,
                        ContentType = m.ContentType,
                        StructuredData = !string.IsNullOrEmpty(m.StructuredData)
                            ? System.Text.Json.JsonSerializer.Deserialize<object>(m.StructuredData)
                            : null
                    })
                    .ToList()
            };

            // Check for draft program linked to this conversation
            var draftProgram = await _context.Programs
                .FirstOrDefaultAsync(p => p.SourceConversationId == id && p.Status == ProgramStatus.Draft.ToApiString());
            if (draftProgram != null)
            {
                response.DraftProgramId = draftProgram.Id;
            }

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

            // Build user profile section if metrics are available. Weight / height
            // / body fat are the user's CURRENT measurements, resolved from Body
            // Metrics history with the same per-field semantics as
            // ProfileController / NutritionController (CurrentMeasurementsService);
            // a since-deleted measurement is never used, an unknown value is just
            // omitted. Target weight / gender / DOB are plain profile fields.
            var userProfileSection = "";
            if (user != null)
            {
                var measurements = await _currentMeasurements.GetForUserAsync(user);
                if (measurements.WeightKg.HasValue || measurements.HeightCm.HasValue || user.DateOfBirth.HasValue)
                {
                    var age = CalculateAge(user.DateOfBirth);
                    var weightLbs = measurements.WeightKg.HasValue ? (measurements.WeightKg.Value * 2.205).ToString("F0") : "Not set";
                    var weightKg = measurements.WeightKg.HasValue ? measurements.WeightKg.Value.ToString("F1") : "Not set";
                    var heightIn = measurements.HeightCm.HasValue ? (measurements.HeightCm.Value / 2.54).ToString("F0") : "Not set";
                    var heightCm = measurements.HeightCm.HasValue ? measurements.HeightCm.Value.ToString("F0") : "Not set";

                    userProfileSection = $@"

**User Profile:**
- Weight: {weightKg}kg ({weightLbs}lbs)
- Height: {heightCm}cm ({heightIn} inches)
- Age: {age} years
- Gender: {user.Gender ?? "Not specified"}
- Body Fat: {(measurements.BodyFatPercentage.HasValue ? $"{measurements.BodyFatPercentage.Value}%" : "Not set")}
- Target Weight: {(user.TargetWeight.HasValue ? $"{user.TargetWeight.Value}kg" : "Not set")}
";
                }
            }

            // Build structured prompt from form data - requests JSON block with summary
            var prompt = $@"I need a personalized workout plan with the following details:
{userProfileSection}
**Training Preferences:**
- Goal: {request.Goal}
- Experience Level: {request.ExperienceLevel}
- Days Per Week: {request.DaysPerWeek}
- Equipment Available: {request.Equipment}
{(!string.IsNullOrEmpty(request.Limitations) ? $"- Limitations/Injuries: {request.Limitations}" : "")}

Please create a detailed workout plan. Your response MUST include:

1. A brief 2-3 sentence summary describing the plan

2. A JSON block wrapped in ```json ... ``` with this exact structure:
```json
{{
  ""programName"": ""Your Program Name"",
  ""splitType"": ""Push/Pull/Legs"",
  ""totalWeeks"": 12,
  ""sessions"": [
    {{
      ""name"": ""Day 1: Push"",
      ""type"": ""strength"",
      ""notes"": ""Focus on chest and triceps"",
      ""exercises"": [
        {{
          ""name"": ""Bench Press"",
          ""sets"": 4,
          ""reps"": 8,
          ""restTime"": 90,
          ""notes"": ""Warm up first""
        }}
      ]
    }}
  ]
}}
```

3. Any additional tips for progression and success

IMPORTANT:
- sets, reps, and restTime MUST be integers (use null if variable)
- Include ALL {request.DaysPerWeek} workout days in the sessions array
- Each session should have 4-8 exercises";

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

                // Parse structured data from response
                var (summaryContent, workoutData) = ParseWorkoutPlanResponse(aiResponse.Content);

                // Save AI message with structured data if parsing succeeded
                var aiMessage = new ChatMessage
                {
                    ConversationId = conversation.Id,
                    Role = "assistant",
                    Content = aiResponse.Content,
                    CreatedAt = DateTime.UtcNow,
                    InputTokens = aiResponse.InputTokens,
                    OutputTokens = aiResponse.OutputTokens,
                    Model = aiResponse.Model,
                    ContentType = workoutData != null ? "workout_plan" : "text",
                    StructuredData = workoutData != null
                        ? System.Text.Json.JsonSerializer.Serialize(workoutData)
                        : null
                };

                _context.ChatMessages.Add(aiMessage);
                conversation.LastMessageAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Auto-create draft program if parsing succeeded
                int? draftProgramId = null;
                if (workoutData?.Sessions != null && workoutData.Sessions.Count > 0)
                {
                    try
                    {
                        var draftProgram = await CreateDraftProgramFromWorkoutData(
                            userId,
                            conversation.Id,
                            workoutData,
                            request.Goal,
                            request.DaysPerWeek
                        );
                        draftProgramId = draftProgram?.Id;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to create draft program, continuing without it");
                    }
                }

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
                    DraftProgramId = draftProgramId,
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
                            Model = aiMessage.Model,
                            ContentType = aiMessage.ContentType,
                            StructuredData = workoutData
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

            // Get user metrics and nutrition goals
            var user = await _context.Users.FindAsync(userId);
            var nutritionGoal = await _context.NutritionGoals
                .Where(ng => ng.UserId == userId && ng.IsActive)
                .FirstOrDefaultAsync();

            var targetCalories = request.TargetCalories ?? nutritionGoal?.DailyCalories ?? 2000m;
            var targetProtein = nutritionGoal?.DailyProtein;
            var targetCarbs = nutritionGoal?.DailyCarbohydrates;
            var targetFat = nutritionGoal?.DailyFat;

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

            try
            {
                // Save user message
                var displayPrompt = $"Generate a {request.DietaryGoal} meal plan for {targetCalories:F0} kcal/day" +
                    (!string.IsNullOrEmpty(request.Restrictions) ? $" with restrictions: {request.Restrictions}" : "") +
                    (!string.IsNullOrEmpty(request.Preferences) ? $", preferences: {request.Preferences}" : "");

                var userMessage = new ChatMessage
                {
                    ConversationId = conversation.Id,
                    Role = "user",
                    Content = displayPrompt,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ChatMessages.Add(userMessage);

                // FOOD DATABASE APPROACH - AI selects from real foods, backend calculates
                var weekData = await GenerateMealPlanFromDatabase(
                    targetCalories,
                    targetProtein,
                    targetCarbs,
                    targetFat,
                    request.DietaryGoal,
                    request.Restrictions,
                    request.Preferences
                );

                string summaryContent;
                int inputTokens = 0, outputTokens = 0;
                string model = "database";

                if (weekData != null && weekData.Days.Count > 0)
                {
                    var avgCalories = weekData.Days.Average(d => d.TotalCalories);
                    _logger.LogInformation("Generated meal plan from database: {DayCount} days, average {AvgCal:F0} kcal/day (target: {Target:F0})",
                        weekData.Days.Count, avgCalories, targetCalories);

                    // Store validated JSON
                    var storeOptions = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
                    conversation.MealPlanDataJson = System.Text.Json.JsonSerializer.Serialize(weekData, storeOptions);

                    summaryContent = BuildMealPlanSummary(weekData, targetCalories, request.DietaryGoal);
                }
                else
                {
                    _logger.LogWarning("Failed to generate meal plan from database for conversation {ConversationId}", conversation.Id);
                    summaryContent = $"I couldn't generate a meal plan. Please try again.";
                }

                // Serialize structured data for preview card
                var structuredDataJson = weekData != null
                    ? System.Text.Json.JsonSerializer.Serialize(new
                    {
                        targetCalories = targetCalories,
                        days = weekData.Days.Select(d => new
                        {
                            day = d.Day,
                            totalCalories = d.TotalCalories,
                            totalProtein = d.TotalProtein,
                            totalCarbs = d.TotalCarbs,
                            totalFat = d.TotalFat,
                            meals = d.Meals.Select(m => new
                            {
                                mealType = m.MealType,
                                totalCalories = m.Foods?.Sum(f => f.Calories ?? 0) ?? 0,
                                foods = m.Foods?.Select(f => new
                                {
                                    name = f.Name,
                                    calories = f.Calories,
                                    protein = f.Protein,
                                    carbohydrates = f.Carbohydrates,
                                    fat = f.Fat,
                                    servingSize = f.ServingSize,
                                    servingUnit = f.ServingUnit
                                })
                            })
                        })
                    })
                    : null;

                var aiMessage = new ChatMessage
                {
                    ConversationId = conversation.Id,
                    Role = "assistant",
                    Content = summaryContent,
                    CreatedAt = DateTime.UtcNow,
                    InputTokens = inputTokens,
                    OutputTokens = outputTokens,
                    Model = model,
                    ContentType = weekData != null ? "meal_plan" : "text",
                    StructuredData = structuredDataJson
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
                            Model = aiMessage.Model,
                            ContentType = aiMessage.ContentType,
                            StructuredData = weekData != null ? System.Text.Json.JsonSerializer.Deserialize<object>(structuredDataJson!) : null
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

                if (conversation.Type != "workout_plan" && conversation.Type != "combined_plan")
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

                if (conversation.Type != "workout_plan" && conversation.Type != "combined_plan")
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

                if (conversation.Type != "workout_plan" && conversation.Type != "combined_plan")
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
                                notes = e.Notes,
                                // Every AI-generated entry is a distinct occurrence, even when
                                // it repeats the same exercise name/template elsewhere in the
                                // workout — see GoHardAPI.Services.ProgramWorkoutExerciseOccurrences.
                                occurrenceKey = Guid.NewGuid().ToString("N")
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

            if (conversation == null || conversation.Type != "workout_plan" && conversation.Type != "combined_plan")
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

        /// <summary>
        /// Parse workout plan response to extract summary and structured JSON data
        /// Attempts multiple parsing strategies for robustness
        /// </summary>
        private (string summary, WorkoutPlanData? data) ParseWorkoutPlanResponse(string content)
        {
            WorkoutPlanData? workoutData = null;
            var summary = content;

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            try
            {
                // Strategy 1: Try to extract JSON from ```json ... ``` block
                var jsonMatch = System.Text.RegularExpressions.Regex.Match(
                    content,
                    @"```json\s*([\s\S]*?)\s*```",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                if (jsonMatch.Success)
                {
                    var jsonContent = jsonMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Found ```json block in response, length: {length}", jsonContent.Length);

                    workoutData = System.Text.Json.JsonSerializer.Deserialize<WorkoutPlanData>(jsonContent, options);
                    _logger.LogInformation("Parsed workout data with {count} sessions", workoutData?.Sessions?.Count ?? 0);

                    // Remove JSON block from summary for cleaner display
                    summary = System.Text.RegularExpressions.Regex.Replace(
                        content,
                        @"```json\s*[\s\S]*?\s*```",
                        "",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    ).Trim();
                }

                // Strategy 2: Try generic ``` ... ``` block if no json-specific block found
                if (workoutData == null)
                {
                    var genericMatch = System.Text.RegularExpressions.Regex.Match(
                        content,
                        @"```\s*([\s\S]*?)\s*```"
                    );

                    if (genericMatch.Success)
                    {
                        var jsonContent = genericMatch.Groups[1].Value.Trim();
                        if (jsonContent.StartsWith("{"))
                        {
                            _logger.LogInformation("Found generic code block with JSON, length: {length}", jsonContent.Length);
                            workoutData = System.Text.Json.JsonSerializer.Deserialize<WorkoutPlanData>(jsonContent, options);

                            summary = System.Text.RegularExpressions.Regex.Replace(
                                content,
                                @"```\s*[\s\S]*?\s*```",
                                "",
                                System.Text.RegularExpressions.RegexOptions.None
                            ).Trim();
                        }
                    }
                }

                // Strategy 3: Try to find raw JSON object in content (no code blocks)
                if (workoutData == null)
                {
                    var rawJsonMatch = System.Text.RegularExpressions.Regex.Match(
                        content,
                        @"\{[\s\S]*""programName""[\s\S]*""sessions""[\s\S]*\}",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    );

                    if (rawJsonMatch.Success)
                    {
                        var jsonContent = rawJsonMatch.Value.Trim();
                        _logger.LogInformation("Found raw JSON in response, length: {length}", jsonContent.Length);

                        try
                        {
                            workoutData = System.Text.Json.JsonSerializer.Deserialize<WorkoutPlanData>(jsonContent, options);
                            summary = content.Replace(jsonContent, "").Trim();
                        }
                        catch (Exception parseEx)
                        {
                            _logger.LogWarning(parseEx, "Failed to parse raw JSON, trying bracket matching");
                        }
                    }
                }

                // Strategy 4: Find JSON by bracket matching (most permissive)
                if (workoutData == null)
                {
                    var startIndex = content.IndexOf('{');
                    if (startIndex >= 0)
                    {
                        var jsonContent = ExtractJsonByBracketMatching(content, startIndex);
                        if (!string.IsNullOrEmpty(jsonContent))
                        {
                            _logger.LogInformation("Extracted JSON by bracket matching, length: {length}", jsonContent.Length);
                            try
                            {
                                workoutData = System.Text.Json.JsonSerializer.Deserialize<WorkoutPlanData>(jsonContent, options);
                                summary = content.Replace(jsonContent, "").Trim();
                            }
                            catch (Exception bracketEx)
                            {
                                _logger.LogWarning(bracketEx, "Failed to parse bracket-matched JSON");
                            }
                        }
                    }
                }

                if (workoutData == null)
                {
                    _logger.LogWarning("All JSON parsing strategies failed for workout plan response");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse workout plan JSON from response");
            }

            return (summary, workoutData);
        }

        /// <summary>
        /// Create a draft program from parsed workout data
        /// Uses transaction to ensure atomic creation of program + workouts
        /// </summary>
        private async Task<Models.Program?> CreateDraftProgramFromWorkoutData(
            int userId,
            int conversationId,
            WorkoutPlanData workoutData,
            string goal,
            int daysPerWeek)
        {
            if (workoutData?.Sessions == null || workoutData.Sessions.Count == 0)
            {
                return null;
            }

            var programName = workoutData.ProgramName ?? $"Workout Plan - {goal}";
            var totalWeeks = workoutData.TotalWeeks ?? 12;

            // Use transaction for atomic creation
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Create draft program
                var program = new Models.Program
                {
                    UserId = userId,
                    Title = programName,
                    Description = $"AI-generated {daysPerWeek}-day workout plan for {goal}",
                    TotalWeeks = totalWeeks,
                    CurrentWeek = 1,
                    CurrentDay = 1,
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = DateTime.UtcNow.Date.AddDays(totalWeeks * 7),
                    IsActive = false,
                    Status = ProgramStatus.Draft.ToApiString(),
                    SourceConversationId = conversationId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Programs.Add(program);
                await _context.SaveChangesAsync();

                // Create program workouts from sessions
                var dayIndex = 0;
                foreach (var session in workoutData.Sessions)
                {
                    dayIndex++;
                    var workout = new ProgramWorkout
                    {
                        ProgramId = program.Id,
                        WeekNumber = 1,
                        DayNumber = dayIndex,
                        DayName = GetDayName(dayIndex),
                        WorkoutName = session.Name ?? $"Day {dayIndex}",
                        WorkoutType = session.Type ?? "strength",
                        Description = session.Notes,
                        OrderIndex = dayIndex,
                        ExercisesJson = session.Exercises != null
                            ? System.Text.Json.JsonSerializer.Serialize(
                                session.Exercises.Select(e => new
                                {
                                    name = e.Name,
                                    sets = e.Sets,
                                    reps = e.Reps,
                                    weight = e.Weight,
                                    rest = e.RestTime,
                                    notes = e.Notes,
                                    // Distinct occurrence per generated entry — see
                                    // GoHardAPI.Services.ProgramWorkoutExerciseOccurrences.
                                    occurrenceKey = Guid.NewGuid().ToString("N")
                                }).ToList())
                            : "[]"
                    };

                    _context.ProgramWorkouts.Add(workout);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Created draft program {programId} with {workoutCount} workouts from conversation {conversationId}",
                    program.Id, workoutData.Sessions.Count, conversationId
                );

                return program;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to create draft program, transaction rolled back");
                throw;
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

        // Helper method to extract JSON by matching brackets
        private string? ExtractJsonByBracketMatching(string content, int startIndex)
        {
            if (startIndex < 0 || startIndex >= content.Length || content[startIndex] != '{')
            {
                return null;
            }

            var depth = 0;
            var inString = false;
            var escapeNext = false;

            for (int i = startIndex; i < content.Length; i++)
            {
                var c = content[i];

                if (escapeNext)
                {
                    escapeNext = false;
                    continue;
                }

                if (c == '\\' && inString)
                {
                    escapeNext = true;
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (!inString)
                {
                    if (c == '{')
                    {
                        depth++;
                    }
                    else if (c == '}')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            return content.Substring(startIndex, i - startIndex + 1);
                        }
                    }
                }
            }

            return null; // Unbalanced brackets
        }

        // GET: api/chat/conversations/{id}/preview-meal-plan
        // Returns all 7 days of the meal plan for user to select which day to apply
        [HttpGet("conversations/{id}/preview-meal-plan")]
        public async Task<ActionResult<MealPlanPreviewResponse>> PreviewMealPlan(int id)
        {
            try
            {
                var userId = GetCurrentUserId();

                var conversation = await _context.ChatConversations
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

                if (conversation == null)
                {
                    return NotFound(new { message = "Conversation not found" });
                }

                if (conversation.Type != "meal_plan" && conversation.Type != "combined_plan")
                {
                    return BadRequest(new { message = "This is not a meal plan conversation" });
                }

                // Get user's nutrition goal for target calories
                var nutritionGoal = await _context.NutritionGoals
                    .Where(ng => ng.UserId == userId && ng.IsActive)
                    .FirstOrDefaultAsync();
                var targetCalories = nutritionGoal?.DailyCalories ?? 2000m;

                // Try to use pre-parsed meal plan JSON (stored at generation time)
                ChatMealPlanWeekExtraction? weekData = null;

                if (!string.IsNullOrEmpty(conversation.MealPlanDataJson))
                {
                    // Use stored JSON for consistency - this is the preferred path
                    try
                    {
                        var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        weekData = System.Text.Json.JsonSerializer.Deserialize<ChatMealPlanWeekExtraction>(conversation.MealPlanDataJson, jsonOptions);
                        _logger.LogInformation("Using stored meal plan JSON for conversation {conversationId}", id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize stored meal plan JSON, falling back to extraction");
                    }
                }

                // Fallback: Extract from message content (for legacy conversations without stored JSON)
                if (weekData == null || weekData.Days.Count == 0)
                {
                    var mealPlanMessage = conversation.Messages
                        .Where(m => m.Role == "assistant")
                        .OrderBy(m => m.CreatedAt)
                        .FirstOrDefault();

                    if (mealPlanMessage == null)
                    {
                        return BadRequest(new { message = "No meal plan found in conversation" });
                    }

                    _logger.LogInformation("Extracting meal plan from message for legacy conversation {conversationId}", id);

                    try
                    {
                        // Don't retry for legacy preview - just get what we can quickly
                        weekData = await ExtractWeekMealPlan(mealPlanMessage.Content, targetCalories, allowRetry: false);

                        // Store the extracted data for future consistency
                        if (weekData != null && weekData.Days.Count > 0)
                        {
                            var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
                            conversation.MealPlanDataJson = System.Text.Json.JsonSerializer.Serialize(weekData, jsonOptions);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Stored extracted meal plan JSON for legacy conversation {conversationId}", id);
                        }
                    }
                    catch (HttpRequestException ex) when (ex.Message.Contains("429"))
                    {
                        _logger.LogWarning(ex, "AI rate limit hit while extracting legacy meal plan for conversation {conversationId}", id);
                        return StatusCode(429, new { message = "AI service rate limit reached. Please try again later or create a new meal plan." });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to extract meal plan for legacy conversation {conversationId}", id);
                        return StatusCode(503, new { message = "Failed to process legacy meal plan. Please create a new meal plan for better reliability." });
                    }
                }

                if (weekData == null || weekData.Days.Count == 0)
                {
                    return BadRequest(new { message = "Could not extract meal plan days" });
                }

                // Build preview response
                var preview = new MealPlanPreviewResponse
                {
                    Success = true,
                    TargetCalories = targetCalories,
                    Days = weekData.Days.Select(d => new MealPlanDayPreview
                    {
                        Day = d.Day,
                        Summary = BuildDaySummary(d.Meals),
                        TotalCalories = d.TotalCalories,
                        TotalProtein = d.TotalProtein,
                        TotalCarbs = d.TotalCarbs,
                        TotalFat = d.TotalFat,
                        IsWithinTarget = Math.Abs(d.TotalCalories - targetCalories) <= targetCalories * 0.15m,
                        Meals = d.Meals.Select(m => new MealPreview
                        {
                            MealType = m.MealType,
                            Foods = m.Foods?.Select(f => f.Name ?? "Unknown").ToList() ?? new List<string>(),
                            Calories = m.Foods?.Sum(f => f.Calories ?? 0) ?? 0
                        }).ToList()
                    }).ToList()
                };

                return Ok(preview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing meal plan");
                return StatusCode(500, new { message = "Failed to preview meal plan" });
            }
        }

        /// <summary>
        /// Generate meal plan using foods from database - 100% reliable nutrition data
        /// AI only selects foods, backend calculates all nutrition values
        /// </summary>
        private async Task<ChatMealPlanWeekExtraction?> GenerateMealPlanFromDatabase(
            decimal targetCalories,
            decimal? targetProtein,
            decimal? targetCarbs,
            decimal? targetFat,
            string dietaryGoal,
            string? restrictions,
            string? preferences)
        {
            // Get all available foods from database
            var allFoods = await _context.FoodTemplates
                .Where(f => !f.IsCustom) // Only system foods for now
                .ToListAsync();

            if (!allFoods.Any())
            {
                _logger.LogWarning("No food templates found in database");
                return null;
            }

            // Group foods by category for AI selection
            var foodsByCategory = allFoods
                .GroupBy(f => f.Category ?? "Other")
                .ToDictionary(g => g.Key, g => g.Select(f => new { f.Id, f.Name, f.Calories, f.Protein, f.Carbohydrates, f.Fat, f.ServingSize, f.ServingUnit }).ToList());

            // Build food list for AI
            var foodListText = string.Join("\n", allFoods.Select(f =>
                $"- {f.Name} ({f.Category}): {f.Calories:F0} kcal, P:{f.Protein:F0}g, C:{f.Carbohydrates:F0}g, F:{f.Fat:F0}g per {f.ServingSize}{f.ServingUnit}"));

            // Calculate calorie distribution
            var breakfastCal = Math.Round(targetCalories * 0.25m);
            var lunchCal = Math.Round(targetCalories * 0.30m);
            var dinnerCal = Math.Round(targetCalories * 0.30m);
            var snackCal = Math.Round(targetCalories * 0.15m);

            var prompt = $@"Create a 1-day meal plan using ONLY foods from this list.
Return JSON with food names and serving multipliers.

TARGET: {targetCalories:F0} kcal/day
Goal: {dietaryGoal}
{(!string.IsNullOrEmpty(restrictions) ? $"Restrictions: {restrictions}" : "")}
{(!string.IsNullOrEmpty(preferences) ? $"Preferences: {preferences}" : "")}

AVAILABLE FOODS:
{foodListText}

Return ONLY this JSON structure (no markdown):
{{
  ""days"": [
    {{
      ""day"": 1,
      ""meals"": [
        {{
          ""mealType"": ""Breakfast"",
          ""foods"": [
            {{ ""name"": ""Oatmeal"", ""servings"": 1.5 }},
            {{ ""name"": ""Banana"", ""servings"": 1 }}
          ]
        }},
        {{ ""mealType"": ""Lunch"", ""foods"": [...] }},
        {{ ""mealType"": ""Dinner"", ""foods"": [...] }},
        {{ ""mealType"": ""Snack"", ""foods"": [...] }}
      ]
    }}
  ]
}}

RULES:
1. Use ONLY foods from the list above (exact names)
2. Adjust servings to reach ~{breakfastCal:F0} kcal breakfast, ~{lunchCal:F0} kcal lunch, ~{dinnerCal:F0} kcal dinner, ~{snackCal:F0} kcal snack
3. Total should be approximately {targetCalories:F0} kcal
4. Include: Breakfast, Lunch, Dinner, Snack";

            try
            {
                var aiResponse = await _aiService.SendMessageAsync(prompt, new List<ChatMessage>(), "meal_plan");
                var jsonContent = aiResponse.Content.Trim();

                // Remove markdown if present
                if (jsonContent.StartsWith("```"))
                {
                    var lines = jsonContent.Split('\n');
                    jsonContent = string.Join('\n', lines.Skip(1).Take(lines.Length - 2));
                }

                // Parse AI selection
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var aiSelection = System.Text.Json.JsonSerializer.Deserialize<AIMealPlanSelection>(jsonContent, options);

                if (aiSelection?.Days == null || !aiSelection.Days.Any())
                {
                    _logger.LogWarning("AI returned invalid meal plan selection");
                    return null;
                }

                // Build final meal plan with REAL nutrition data from database
                var weekData = new ChatMealPlanWeekExtraction { TargetCalories = targetCalories, Days = new List<ChatMealPlanDayData>() };
                var foodLookup = allFoods.ToDictionary(f => f.Name.ToLower(), f => f);

                foreach (var aiDay in aiSelection.Days)
                {
                    var dayData = new ChatMealPlanDayData
                    {
                        Day = aiDay.Day,
                        Meals = new List<ChatMealPlanMealData>()
                    };

                    decimal dayCalories = 0, dayProtein = 0, dayCarbs = 0, dayFat = 0;

                    foreach (var aiMeal in aiDay.Meals ?? new List<AIMealSelection>())
                    {
                        var mealData = new ChatMealPlanMealData
                        {
                            MealType = aiMeal.MealType ?? "Other",
                            Foods = new List<ChatMealPlanFoodData>()
                        };

                        foreach (var aiFood in aiMeal.Foods ?? new List<AIFoodSelection>())
                        {
                            // Look up REAL nutrition from database
                            if (foodLookup.TryGetValue(aiFood.Name?.ToLower() ?? "", out var dbFood))
                            {
                                var servings = aiFood.Servings > 0 ? aiFood.Servings : 1;
                                var foodData = new ChatMealPlanFoodData
                                {
                                    Name = dbFood.Name,
                                    ServingSize = dbFood.ServingSize * servings,
                                    ServingUnit = dbFood.ServingUnit,
                                    Calories = dbFood.Calories * servings,
                                    Protein = dbFood.Protein * servings,
                                    Carbohydrates = dbFood.Carbohydrates * servings,
                                    Fat = dbFood.Fat * servings
                                };

                                mealData.Foods.Add(foodData);
                                dayCalories += foodData.Calories ?? 0;
                                dayProtein += foodData.Protein ?? 0;
                                dayCarbs += foodData.Carbohydrates ?? 0;
                                dayFat += foodData.Fat ?? 0;
                            }
                            else
                            {
                                _logger.LogDebug("Food not found in database: {FoodName}", aiFood.Name);
                            }
                        }

                        dayData.Meals.Add(mealData);
                    }

                    // AUTO-SCALE to hit target calories (both up and down)
                    // Scale if more than 5% off target (either direction)
                    var percentOff = Math.Abs(dayCalories - targetCalories) / targetCalories;
                    if (dayCalories > 0 && percentOff > 0.05m)
                    {
                        var scaleFactor = targetCalories / dayCalories;

                        _logger.LogInformation("Day {Day}: Scaling from {Original:F0} to {Target:F0} kcal (factor: {Factor:F2}, was {Percent:P0} off)",
                            aiDay.Day, dayCalories, targetCalories, scaleFactor, percentOff);

                        // Scale all foods in this day
                        foreach (var meal in dayData.Meals)
                        {
                            foreach (var food in meal.Foods ?? new List<ChatMealPlanFoodData>())
                            {
                                food.ServingSize = (food.ServingSize ?? 1) * scaleFactor;
                                food.Calories = (food.Calories ?? 0) * scaleFactor;
                                food.Protein = (food.Protein ?? 0) * scaleFactor;
                                food.Carbohydrates = (food.Carbohydrates ?? 0) * scaleFactor;
                                food.Fat = (food.Fat ?? 0) * scaleFactor;
                            }
                        }

                        // Recalculate day totals
                        dayCalories *= scaleFactor;
                        dayProtein *= scaleFactor;
                        dayCarbs *= scaleFactor;
                        dayFat *= scaleFactor;
                    }

                    dayData.TotalCalories = dayCalories;
                    dayData.TotalProtein = dayProtein;
                    dayData.TotalCarbs = dayCarbs;
                    dayData.TotalFat = dayFat;
                    weekData.Days.Add(dayData);
                }

                _logger.LogInformation("Generated meal plan from database: {DayCount} days, target: {Target:F0} kcal",
                    weekData.Days.Count, targetCalories);
                return weekData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate meal plan from database");
                return null;
            }
        }

        // AI response models for food database approach
        private class AIMealPlanSelection
        {
            public List<AIDaySelection>? Days { get; set; }
        }

        private class AIDaySelection
        {
            public int Day { get; set; }
            public List<AIMealSelection>? Meals { get; set; }
        }

        private class AIMealSelection
        {
            public string? MealType { get; set; }
            public List<AIFoodSelection>? Foods { get; set; }
        }

        private class AIFoodSelection
        {
            public string? Name { get; set; }
            public decimal Servings { get; set; } = 1;
        }

        /// <summary>
        /// Build a readable meal plan summary for chat display
        /// </summary>
        private string BuildMealPlanSummary(ChatMealPlanWeekExtraction? weekData, decimal targetCalories, string dietaryGoal)
        {
            if (weekData == null || weekData.Days.Count == 0)
            {
                return $"I've created a {dietaryGoal} meal plan targeting {targetCalories:F0} kcal/day. However, I couldn't generate the structured data. Please try again.";
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"# 🍽️ Your {dietaryGoal} Meal Plan");
            sb.AppendLine();
            sb.AppendLine($"**Target:** {targetCalories:F0} kcal/day");
            sb.AppendLine();

            foreach (var day in weekData.Days.OrderBy(d => d.Day))
            {
                sb.AppendLine($"## Day {day.Day} ({day.TotalCalories:F0} kcal)");

                foreach (var meal in day.Meals)
                {
                    var mealCalories = meal.Foods?.Sum(f => f.Calories ?? 0) ?? 0;
                    sb.AppendLine($"**{meal.MealType}** (~{mealCalories:F0} kcal)");

                    if (meal.Foods != null)
                    {
                        foreach (var food in meal.Foods)
                        {
                            sb.AppendLine($"- {food.Name} ({food.Calories:F0} kcal)");
                        }
                    }
                    sb.AppendLine();
                }

                // Macros summary
                sb.AppendLine($"*Macros: P {day.TotalProtein:F0}g | C {day.TotalCarbs:F0}g | F {day.TotalFat:F0}g*");
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();
            }

            sb.AppendLine("### Tips");
            sb.AppendLine("- Prep proteins in bulk on weekends");
            sb.AppendLine("- Keep healthy snacks ready");
            sb.AppendLine("- Stay hydrated throughout the day");
            sb.AppendLine();
            sb.AppendLine("*Click **Apply Meal Plan** to add these meals to your nutrition tracker.*");

            return sb.ToString();
        }

        private string BuildDaySummary(List<ChatMealPlanMealData> meals)
        {
            var foods = meals
                .SelectMany(m => m.Foods ?? new List<ChatMealPlanFoodData>())
                .Take(3)
                .Select(f => f.Name)
                .Where(n => !string.IsNullOrEmpty(n));
            return string.Join(", ", foods) + "...";
        }

        private async Task<ChatMealPlanWeekExtraction?> ExtractWeekMealPlan(string mealPlanContent, decimal targetCalories, bool allowRetry = true, int attempt = 1)
        {
            // Calculate calorie distribution per meal
            var breakfastCal = Math.Round(targetCalories * 0.25m); // 25%
            var lunchCal = Math.Round(targetCalories * 0.30m);     // 30%
            var dinnerCal = Math.Round(targetCalories * 0.30m);    // 30%
            var snackCal = Math.Round(targetCalories * 0.15m);     // 15%

            var extractionPrompt = $@"Create a structured 1-day meal plan in JSON format based on the content provided.

CALORIE TARGET: {targetCalories:F0} kcal per day

REQUIRED CALORIE DISTRIBUTION PER MEAL:
- Breakfast: ~{breakfastCal:F0} kcal (2-3 foods)
- Lunch: ~{lunchCal:F0} kcal (2-4 foods)
- Dinner: ~{dinnerCal:F0} kcal (2-4 foods)
- Snacks: ~{snackCal:F0} kcal (1-2 foods)

EXAMPLE - A {breakfastCal:F0} kcal breakfast:
- 3 eggs scrambled (210 kcal, 18g protein, 1g carbs, 15g fat)
- 2 slices whole wheat toast (160 kcal, 6g protein, 28g carbs, 2g fat)
- 1 tbsp butter (100 kcal, 0g protein, 0g carbs, 11g fat)
Total: ~470 kcal

Return ONLY valid JSON (no markdown, no explanations) with this exact structure:
{{
  ""days"": [
    {{
      ""day"": 1,
      ""meals"": [
        {{
          ""mealType"": ""Breakfast"",
          ""foods"": [
            {{ ""name"": ""Scrambled Eggs"", ""servingSize"": 3, ""servingUnit"": ""eggs"", ""calories"": 210, ""protein"": 18, ""carbohydrates"": 1, ""fat"": 15 }},
            {{ ""name"": ""Whole Wheat Toast"", ""servingSize"": 2, ""servingUnit"": ""slices"", ""calories"": 160, ""protein"": 6, ""carbohydrates"": 28, ""fat"": 2 }}
          ]
        }},
        {{ ""mealType"": ""Lunch"", ""foods"": [...] }},
        {{ ""mealType"": ""Dinner"", ""foods"": [...] }},
        {{ ""mealType"": ""Snack"", ""foods"": [...] }}
      ],
      ""totalCalories"": 0,
      ""totalProtein"": 0,
      ""totalCarbs"": 0,
      ""totalFat"": 0
    }}
  ]
}}

CRITICAL RULES:
1. Generate exactly 1 day only
2. Each food item MUST have realistic calories (100-600 kcal per food item)
3. The SUM of all food calories MUST equal approximately {targetCalories:F0} kcal
4. Use the foods mentioned in the content (proteins, carbs, veggies, etc.)
5. mealType must be exactly: Breakfast, Lunch, Dinner, or Snack
6. All numeric values must be numbers (not strings)
7. Set totalCalories/totalProtein/totalCarbs/totalFat to 0 - they will be calculated by the system
8. VERIFY: Add up all food calories mentally before returning - must be ~{targetCalories:F0}";

            var messages = new List<ChatMessage>
            {
                new ChatMessage { Role = "assistant", Content = mealPlanContent }
            };

            try
            {
                var response = await _aiService.SendMessageAsync(extractionPrompt, messages, "meal_plan");
                var jsonContent = response.Content.Trim();

                // Remove markdown code blocks if present
                if (jsonContent.StartsWith("```"))
                {
                    var lines = jsonContent.Split('\n');
                    jsonContent = string.Join('\n', lines.Skip(1).Take(lines.Length - 2));
                }

                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var weekData = System.Text.Json.JsonSerializer.Deserialize<ChatMealPlanWeekExtraction>(jsonContent, options);

                if (weekData == null) return null;

                // ALWAYS recalculate totals from actual food items (never trust AI totals)
                ValidateAndRecalculateTotals(weekData);

                // Check if we're too far off target (less than 70% of target)
                var avgCalories = weekData.Days.Average(d => d.TotalCalories);
                var percentOfTarget = avgCalories / targetCalories * 100;

                _logger.LogInformation("Meal plan extraction attempt {Attempt}: Average {AvgCal:F0} kcal ({Percent:F0}% of target {Target:F0})",
                    attempt, avgCalories, percentOfTarget, targetCalories);

                // If too far off and this is first attempt and retry is allowed, try once more with feedback
                if (allowRetry && percentOfTarget < 70 && attempt == 1)
                {
                    _logger.LogWarning("Meal plan only has {Percent:F0}% of target calories, regenerating...", percentOfTarget);

                    var feedbackPrompt = $@"The previous meal plan only had {avgCalories:F0} kcal per day, but the target is {targetCalories:F0} kcal.

You need to add MORE FOOD or LARGER PORTIONS. Each day needs approximately:
- Breakfast: {breakfastCal:F0} kcal
- Lunch: {lunchCal:F0} kcal
- Dinner: {dinnerCal:F0} kcal
- Snacks: {snackCal:F0} kcal

Please regenerate with enough food to reach {targetCalories:F0} kcal per day.

{extractionPrompt}";

                    messages = new List<ChatMessage>
                    {
                        new ChatMessage { Role = "assistant", Content = mealPlanContent }
                    };

                    response = await _aiService.SendMessageAsync(feedbackPrompt, messages, "meal_plan");
                    jsonContent = response.Content.Trim();

                    if (jsonContent.StartsWith("```"))
                    {
                        var lines = jsonContent.Split('\n');
                        jsonContent = string.Join('\n', lines.Skip(1).Take(lines.Length - 2));
                    }

                    var retryData = System.Text.Json.JsonSerializer.Deserialize<ChatMealPlanWeekExtraction>(jsonContent, options);
                    if (retryData != null)
                    {
                        ValidateAndRecalculateTotals(retryData);
                        var retryAvg = retryData.Days.Average(d => d.TotalCalories);
                        _logger.LogInformation("Retry meal plan: Average {AvgCal:F0} kcal ({Percent:F0}% of target)",
                            retryAvg, retryAvg / targetCalories * 100);

                        // Use retry if it's better
                        if (retryAvg > avgCalories)
                        {
                            return retryData;
                        }
                    }
                }

                return weekData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract week meal plan");
                return null;
            }
        }

        /// <summary>
        /// Recalculate all totals from actual food items. Never trust AI-provided totals.
        /// </summary>
        private void ValidateAndRecalculateTotals(ChatMealPlanWeekExtraction weekData)
        {
            foreach (var day in weekData.Days)
            {
                decimal totalCalories = 0;
                decimal totalProtein = 0;
                decimal totalCarbs = 0;
                decimal totalFat = 0;

                foreach (var meal in day.Meals)
                {
                    if (meal.Foods == null) continue;

                    foreach (var food in meal.Foods)
                    {
                        totalCalories += food.Calories ?? 0;
                        totalProtein += food.Protein ?? 0;
                        totalCarbs += food.Carbohydrates ?? 0;
                        totalFat += food.Fat ?? 0;
                    }
                }

                // Override AI totals with calculated values
                day.TotalCalories = totalCalories;
                day.TotalProtein = totalProtein;
                day.TotalCarbs = totalCarbs;
                day.TotalFat = totalFat;
            }
        }

        // POST: api/chat/conversations/{id}/apply-meal-plan
        // Accepts optional 'day' (1-7, which day of the plan to apply) and optional
        // 'date' (which calendar date to apply it to; defaults to today) query parameters.
        [HttpPost("conversations/{id}/apply-meal-plan")]
        public async Task<ActionResult<ApplyMealPlanResponse>> ApplyMealPlanToToday(
            int id,
            [FromQuery] int day = 1,
            [FromQuery] DateTime? date = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var targetDate = (date ?? DateTime.UtcNow).Date;

                // Get the conversation
                var conversation = await _context.ChatConversations
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

                if (conversation == null)
                {
                    return NotFound(new { message = "Conversation not found" });
                }

                if (conversation.Type != "meal_plan" && conversation.Type != "combined_plan")
                {
                    return BadRequest(new { message = "This is not a meal plan conversation" });
                }

                // Validate day parameter
                if (day < 1 || day > 7)
                {
                    return BadRequest(new { message = "Day must be between 1 and 7" });
                }

                // Get user's nutrition goal for context
                var nutritionGoal = await _context.NutritionGoals
                    .Where(ng => ng.UserId == userId && ng.IsActive)
                    .FirstOrDefaultAsync();
                var targetCalories = nutritionGoal?.DailyCalories ?? 2000m;

                // Try to use pre-parsed meal plan JSON (stored at generation time)
                ChatMealPlanWeekExtraction? weekData = null;

                if (!string.IsNullOrEmpty(conversation.MealPlanDataJson))
                {
                    // Use stored JSON for consistency - this is the preferred path
                    try
                    {
                        var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        weekData = System.Text.Json.JsonSerializer.Deserialize<ChatMealPlanWeekExtraction>(conversation.MealPlanDataJson, jsonOptions);
                        _logger.LogInformation("Using stored meal plan JSON for apply operation on conversation {conversationId}", id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize stored meal plan JSON, falling back to extraction");
                    }
                }

                // Fallback: Extract from message content (for legacy conversations)
                if (weekData == null || weekData.Days.Count == 0)
                {
                    var mealPlanMessage = conversation.Messages
                        .Where(m => m.Role == "assistant")
                        .OrderBy(m => m.CreatedAt)
                        .FirstOrDefault();

                    if (mealPlanMessage == null)
                    {
                        return BadRequest(new { message = "No meal plan found in conversation" });
                    }

                    _logger.LogInformation("Extracting meal plan from message for legacy conversation {conversationId}", id);

                    try
                    {
                        // Don't retry for legacy extraction - just get what we can quickly
                        weekData = await ExtractWeekMealPlan(mealPlanMessage.Content, targetCalories, allowRetry: false);
                    }
                    catch (HttpRequestException ex) when (ex.Message.Contains("429"))
                    {
                        _logger.LogWarning(ex, "AI rate limit hit while extracting legacy meal plan for conversation {conversationId}", id);
                        return StatusCode(429, new { message = "AI service rate limit reached. Please try again later or create a new meal plan." });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to extract meal plan for legacy conversation {conversationId}", id);
                        return StatusCode(503, new { message = "Failed to process legacy meal plan. Please create a new meal plan for better reliability." });
                    }
                }

                if (weekData == null || weekData.Days.Count == 0)
                {
                    return BadRequest(new { message = "Failed to extract meal plan structure" });
                }

                // Get the selected day
                var selectedDay = weekData.Days.FirstOrDefault(d => d.Day == day);
                if (selectedDay == null)
                {
                    // Fallback to first day if requested day not found
                    selectedDay = weekData.Days.First();
                    _logger.LogWarning("Day {day} not found in meal plan, using day {actualDay}", day, selectedDay.Day);
                }

                _logger.LogInformation("Applying day {day} of meal plan: {calories} kcal", selectedDay.Day, selectedDay.TotalCalories);

                // Convert to the format expected by the rest of the method
                var mealPlanData = new ChatMealPlanExtraction
                {
                    Meals = selectedDay.Meals
                };

                // Validate extracted values - check if total calories are reasonable
                var extractedTotalCalories = selectedDay.TotalCalories > 0
                    ? selectedDay.TotalCalories
                    : mealPlanData.Meals.SelectMany(m => m.Foods ?? new List<ChatMealPlanFoodData>()).Sum(f => f.Calories ?? 0);

                _logger.LogInformation("Selected day {day} totals: {extractedCalories} kcal (target: {targetCalories} kcal)",
                    day, extractedTotalCalories, targetCalories);

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

                // Everything below reads and writes real food-log CONTENT and the cached
                // totals derived from it as ONE atomic, retried unit - see
                // MealLogTotalsRecalculator.ExecuteAtomicallyAsync for the full rationale.
                // Committing the content change and its totals recompute SEPARATELY (an
                // earlier version of this endpoint did exactly that) left a real gap: a
                // crash or an exhausted retry between the two steps left committed food
                // rows with a stale, never-corrected total. Retrying the WHOLE attempt
                // (not just the totals tail) on a confirmed PostgreSQL conflict is safe
                // specifically because a rolled-back transaction leaves zero trace - see
                // that method's doc comment for why. Serializable (not merely
                // RepeatableRead): a concurrent MarkAsConsumed or ordinary food edit on the
                // same row that commits while we're mid-flight causes our own write to that
                // row to fail — we roll back and retry from a fresh read, rather than
                // silently overwriting what they just logged as eaten. RepeatableRead alone
                // is NOT enough here: this method does a read-then-conditionally-insert-a-
                // new-MealEntry for each meal type ("does a Lunch entry already exist? no ->
                // create one") — a classic phantom-read/write-skew shape that RepeatableRead's
                // snapshot isolation does NOT detect (two concurrent transactions can both
                // see "no Lunch entry" and both insert one, silently duplicating it —
                // Postgres only starts rejecting one side of that with Serializable's
                // predicate-locking SSI).
                var attemptResult = await MealLogTotalsRecalculator.ExecuteAtomicallyAsync(_context, async ct =>
                {
                    var addedFoods = new List<object>();
                    var skippedMealTypes = new List<string>();
                    var replacedMealTypes = new List<string>();
                    decimal totalCaloriesAdded = 0;
                    decimal totalProteinAdded = 0;
                    decimal totalCarbsAdded = 0;
                    decimal totalFatAdded = 0;

                    // 1. UPDATE OR CREATE NUTRITION GOALS TO MATCH MEAL PLAN DAY
                    // Re-read fresh within this attempt - the outer `nutritionGoal` read
                    // (used above only to compute targetCalories for AI-output scaling) may
                    // be a detached, stale instance from an earlier attempt by the time a
                    // retry runs here.
                    var goalToUpdate = await _context.NutritionGoals
                        .Where(ng => ng.UserId == userId && ng.IsActive)
                        .FirstOrDefaultAsync(ct);

                    if (goalToUpdate == null)
                    {
                        // Create new nutrition goal from meal plan
                        goalToUpdate = new Models.NutritionGoal
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
                        _context.NutritionGoals.Add(goalToUpdate);
                        _logger.LogInformation("Created new nutrition goal from meal plan for user {userId}: {cal} kcal", userId, totalCalories);
                    }
                    else
                    {
                        // UPDATE existing nutrition goal to match the meal plan day
                        goalToUpdate.DailyCalories = totalCalories;
                        goalToUpdate.DailyProtein = totalProtein;
                        goalToUpdate.DailyCarbohydrates = totalCarbs;
                        goalToUpdate.DailyFat = totalFat;
                        goalToUpdate.Name = "Meal Plan Goals";
                        goalToUpdate.UpdatedAt = DateTime.UtcNow;
                        _logger.LogInformation("Updated nutrition goal for user {userId}: {cal} kcal, {prot}g protein, {carb}g carbs, {fat}g fat",
                            userId, totalCalories, totalProtein, totalCarbs, totalFat);
                    }
                    await _context.SaveChangesAsync(ct);

                    // 2. GET OR CREATE THE TARGET DATE'S MEAL LOG
                    var mealLog = await _context.MealLogs
                        .Include(ml => ml.MealEntries)
                        .ThenInclude(me => me.FoodItems)
                        .FirstOrDefaultAsync(ml => ml.UserId == userId && ml.Date.Date == targetDate, ct);

                    if (mealLog == null)
                    {
                        mealLog = new Models.MealLog
                        {
                            UserId = userId,
                            Date = targetDate,
                            TotalCalories = 0,
                            TotalProtein = 0,
                            TotalCarbohydrates = 0,
                            TotalFat = 0,
                            WaterIntake = 0,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.MealLogs.Add(mealLog);
                        await _context.SaveChangesAsync(ct);
                    }

                    // Ensure meal entries exist only for the meal types this plan actually targets
                    var mealTypesInPlan = mealPlanData.Meals
                        .Select(m => m.MealType)
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    foreach (var mealType in mealTypesInPlan)
                    {
                        if (!mealLog.MealEntries.Any(me => me.MealType.Equals(mealType, StringComparison.OrdinalIgnoreCase)))
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
                    await _context.SaveChangesAsync(ct);

                    // 3-4. FOR EACH TARGETED, NOT-YET-CONSUMED MEAL: replace only this exact
                    // suggestion's own prior output (idempotent reapply), then add the new
                    // items. A meal entry the user has already marked consumed is skipped
                    // entirely — never cleared, never touched — and food items belonging to
                    // any other source (manual entries, a different AI plan) are left alone.
                    foreach (var mealData in mealPlanData.Meals)
                    {
                        var mealEntry = mealLog.MealEntries.FirstOrDefault(me =>
                            me.MealType.Equals(mealData.MealType, StringComparison.OrdinalIgnoreCase));

                        if (mealEntry == null) continue;

                        if (mealEntry.IsConsumed)
                        {
                            skippedMealTypes.Add(mealEntry.MealType);
                            continue;
                        }

                        var ownPriorItems = mealEntry.FoodItems
                            .Where(fi => fi.SourcePlanConversationId == conversation.Id && fi.SourcePlanDay == selectedDay.Day)
                            .ToList();
                        if (ownPriorItems.Count > 0)
                        {
                            _context.FoodItems.RemoveRange(ownPriorItems);
                            replacedMealTypes.Add(mealEntry.MealType);
                        }

                        var foods = mealData.Foods ?? new List<ChatMealPlanFoodData>();
                        foreach (var foodData in foods)
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
                                SourcePlanConversationId = conversation.Id,
                                SourcePlanDay = selectedDay.Day,
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
                    await _context.SaveChangesAsync(ct);

                    // 5. UPDATE NUTRITION PROGRESS (planned values) for the target date
                    var nutritionProgress = await _context.NutritionProgresses
                        .FirstOrDefaultAsync(np => np.UserId == userId && np.Date.Date == targetDate, ct);

                    if (nutritionProgress == null)
                    {
                        var activeGoal = await _context.NutritionGoals
                            .FirstOrDefaultAsync(ng => ng.UserId == userId && ng.IsActive, ct);

                        nutritionProgress = new Models.NutritionProgress
                        {
                            UserId = userId,
                            Date = targetDate,
                            NutritionGoalId = activeGoal?.Id,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.NutritionProgresses.Add(nutritionProgress);
                    }

                    // Add planned values from the applied meal
                    nutritionProgress.PlannedCalories += totalCaloriesAdded;
                    nutritionProgress.PlannedProtein += totalProteinAdded;
                    nutritionProgress.PlannedCarbohydrates += totalCarbsAdded;
                    nutritionProgress.PlannedFat += totalFatAdded;
                    nutritionProgress.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync(ct);

                    // 6. Recompute this meal log's cached totals from the FoodItems just
                    // written above, staged into the SAME transaction as everything else in
                    // this attempt - see MealLogTotalsRecalculator.
                    await MealLogTotalsRecalculator.StageRecalculationAsync(_context, mealLog.Id, ct);

                    return (addedFoods, skippedMealTypes, replacedMealTypes, totalCaloriesAdded, totalProteinAdded, totalCarbsAdded, totalFatAdded);
                });

                var (addedFoods, skippedMealTypes, replacedMealTypes, totalCaloriesAdded, totalProteinAdded, totalCarbsAdded, totalFatAdded) = attemptResult;

                _logger.LogInformation(
                    "Updated NutritionProgress for user {UserId}: +{Calories} planned calories",
                    userId, totalCaloriesAdded);

                return Ok(new ApplyMealPlanResponse
                {
                    Success = true,
                    Message = $"Nutrition goal updated to {totalCalories:F0} kcal and {addedFoods.Count} foods added to your log",
                    FoodsAdded = addedFoods.Count,
                    TotalCaloriesAdded = totalCaloriesAdded,
                    TotalProteinAdded = totalProteinAdded,
                    TotalCarbsAdded = totalCarbsAdded,
                    TotalFatAdded = totalFatAdded,
                    Foods = addedFoods,
                    GoalUpdated = true,
                    NewDailyCalorieGoal = totalCalories,
                    NewDailyProteinGoal = totalProtein,
                    NewDailyCarbsGoal = totalCarbs,
                    NewDailyFatGoal = totalFat,
                    SkippedMealTypes = skippedMealTypes,
                    ReplacedMealTypes = replacedMealTypes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying meal plan to today");
                return StatusCode(500, new { message = "Failed to apply meal plan. Please try again." });
            }
        }

        // POST: api/chat/conversations/{id}/apply-meal-plan-week
        // Applies multiple days of the meal plan starting from a specified date
        [HttpPost("conversations/{id}/apply-meal-plan-week")]
        public async Task<ActionResult<ApplyMealPlanWeekResponse>> ApplyMealPlanWeek(int id, [FromBody] ApplyMealPlanWeekRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();

                var conversation = await _context.ChatConversations
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

                if (conversation == null)
                {
                    return NotFound(new { message = "Conversation not found" });
                }

                if (conversation.Type != "meal_plan" && conversation.Type != "combined_plan")
                {
                    return BadRequest(new { message = "This is not a meal plan conversation" });
                }

                // Get user's nutrition goal for context
                var nutritionGoal = await _context.NutritionGoals
                    .Where(ng => ng.UserId == userId && ng.IsActive)
                    .FirstOrDefaultAsync();
                var targetCalories = nutritionGoal?.DailyCalories ?? 2000m;

                // Get the stored meal plan data
                ChatMealPlanWeekExtraction? weekData = null;

                if (!string.IsNullOrEmpty(conversation.MealPlanDataJson))
                {
                    try
                    {
                        var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        weekData = System.Text.Json.JsonSerializer.Deserialize<ChatMealPlanWeekExtraction>(conversation.MealPlanDataJson, jsonOptions);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize meal plan JSON");
                    }
                }

                if (weekData == null || weekData.Days.Count == 0)
                {
                    return BadRequest(new { message = "No meal plan data found. Please regenerate the meal plan." });
                }

                // Determine which days to apply
                var daysToApply = request.ApplyAllDays
                    ? weekData.Days.OrderBy(d => d.Day).ToList()
                    : weekData.Days.Where(d => request.Days?.Contains(d.Day) ?? false).OrderBy(d => d.Day).ToList();

                if (!daysToApply.Any())
                {
                    return BadRequest(new { message = "No days selected to apply" });
                }

                var startDate = request.StartDate?.Date ?? DateTime.UtcNow.Date;

                // The whole batch, INCLUDING the resulting cached totals, commits as ONE
                // atomic, retried unit - see MealLogTotalsRecalculator.ExecuteAtomicallyAsync
                // for the full rationale (committing content and totals separately left a
                // real gap: a crash or exhausted retry between the two steps left committed
                // food rows with a stale, never-corrected total). Either every selected day
                // applies with its totals correctly recomputed, or nothing does — a failure
                // partway through never leaves some days applied and others half-done, and
                // never leaves a day's food committed without its corresponding totals.
                // Serializable (see ApplyMealPlanToToday's doc comment for the full
                // rationale) means a concurrent MarkAsConsumed on any touched entry aborts
                // our write to that row instead of silently overwriting it, AND a concurrent
                // create of the same not-yet-existing MealEntry (this method also does
                // read-then-conditionally-insert per meal type, per day) can no longer
                // silently duplicate that row the way RepeatableRead alone would allow.
                var attemptResult = await MealLogTotalsRecalculator.ExecuteAtomicallyAsync(_context, async ct =>
                {
                    var touchedMealLogIds = new HashSet<int>();
                    var results = new List<DayApplyResult>();
                    var totalFoodsAdded = 0;
                    decimal grandTotalCalories = 0;
                    decimal grandTotalProtein = 0;
                    decimal grandTotalCarbs = 0;
                    decimal grandTotalFat = 0;

                    foreach (var dayData in daysToApply)
                    {
                        var targetDate = startDate.AddDays(daysToApply.IndexOf(dayData));

                        // Get or create meal log for this date
                        var mealLog = await _context.MealLogs
                            .Include(ml => ml.MealEntries)
                            .ThenInclude(me => me.FoodItems)
                            .FirstOrDefaultAsync(ml => ml.UserId == userId && ml.Date.Date == targetDate);

                        if (mealLog == null)
                        {
                            mealLog = new Models.MealLog
                            {
                                UserId = userId,
                                Date = targetDate,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.MealLogs.Add(mealLog);
                            await _context.SaveChangesAsync();
                        }
                        touchedMealLogIds.Add(mealLog.Id);

                        // Ensure meal entries exist only for the meal types this day's plan targets
                        var mealTypesInDay = dayData.Meals
                            .Select(m => m.MealType)
                            .Where(t => !string.IsNullOrWhiteSpace(t))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();
                        foreach (var mealType in mealTypesInDay)
                        {
                            if (!mealLog.MealEntries.Any(me => me.MealType.Equals(mealType, StringComparison.OrdinalIgnoreCase)))
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

                        // Add food items for each targeted, not-yet-consumed entry. A consumed
                        // entry is skipped entirely. Within a non-consumed entry, this exact
                        // suggestion's own prior output (same conversation + day) is replaced
                        // (idempotent reapply); when OverwriteExisting is set, other-sourced
                        // planned items are also replaced (the old client's "full day" intent,
                        // now safe) — but a consumed entry is excluded either way.
                        decimal dayCalories = 0;
                        decimal dayProtein = 0;
                        decimal dayCarbs = 0;
                        decimal dayFat = 0;
                        int dayFoodsAdded = 0;
                        var daySkipped = new List<string>();
                        var dayReplaced = new List<string>();

                        foreach (var mealData in dayData.Meals)
                        {
                            var mealEntry = mealLog.MealEntries.FirstOrDefault(me =>
                                me.MealType.Equals(mealData.MealType, StringComparison.OrdinalIgnoreCase));

                            if (mealEntry == null) continue;

                            if (mealEntry.IsConsumed)
                            {
                                daySkipped.Add(mealEntry.MealType);
                                continue;
                            }

                            var itemsToRemove = request.OverwriteExisting
                                ? mealEntry.FoodItems.ToList()
                                : mealEntry.FoodItems
                                    .Where(fi => fi.SourcePlanConversationId == conversation.Id && fi.SourcePlanDay == dayData.Day)
                                    .ToList();
                            if (itemsToRemove.Count > 0)
                            {
                                _context.FoodItems.RemoveRange(itemsToRemove);
                                dayReplaced.Add(mealEntry.MealType);
                            }

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
                                    SourcePlanConversationId = conversation.Id,
                                    SourcePlanDay = dayData.Day,
                                    CreatedAt = DateTime.UtcNow
                                };

                                _context.FoodItems.Add(foodItem);
                                dayCalories += foodItem.Calories;
                                dayProtein += foodItem.Protein;
                                dayCarbs += foodItem.Carbohydrates;
                                dayFat += foodItem.Fat;
                                dayFoodsAdded++;
                            }
                        }
                        await _context.SaveChangesAsync();

                        // Update NutritionProgress for this day
                        var nutritionProgress = await _context.NutritionProgresses
                            .FirstOrDefaultAsync(np => np.UserId == userId && np.Date.Date == targetDate.Date);

                        if (nutritionProgress == null)
                        {
                            var activeGoal = await _context.NutritionGoals
                                .FirstOrDefaultAsync(ng => ng.UserId == userId && ng.IsActive);

                            nutritionProgress = new Models.NutritionProgress
                            {
                                UserId = userId,
                                Date = targetDate.Date,
                                NutritionGoalId = activeGoal?.Id,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.NutritionProgresses.Add(nutritionProgress);
                        }

                        nutritionProgress.PlannedCalories += dayCalories;
                        nutritionProgress.PlannedProtein += dayProtein;
                        nutritionProgress.PlannedCarbohydrates += dayCarbs;
                        nutritionProgress.PlannedFat += dayFat;
                        nutritionProgress.UpdatedAt = DateTime.UtcNow;

                        await _context.SaveChangesAsync();

                        results.Add(new DayApplyResult
                        {
                            Day = dayData.Day,
                            Date = targetDate,
                            FoodsAdded = dayFoodsAdded,
                            Calories = dayCalories,
                            Protein = dayProtein,
                            Carbs = dayCarbs,
                            Fat = dayFat,
                            SkippedMealTypes = daySkipped,
                            ReplacedMealTypes = dayReplaced
                        });

                        totalFoodsAdded += dayFoodsAdded;
                        grandTotalCalories += dayCalories;
                        grandTotalProtein += dayProtein;
                        grandTotalCarbs += dayCarbs;
                        grandTotalFat += dayFat;
                    }

                    // Recompute each touched meal log's cached totals from the FoodItems just
                    // written above, staged into the SAME transaction/attempt as everything
                    // else in this batch - see MealLogTotalsRecalculator.
                    foreach (var touchedMealLogId in touchedMealLogIds)
                    {
                        await MealLogTotalsRecalculator.StageRecalculationAsync(_context, touchedMealLogId, ct);
                    }

                    return (results, totalFoodsAdded, grandTotalCalories, grandTotalProtein, grandTotalCarbs, grandTotalFat);
                });

                var (dayResults, totalFoodsAdded, grandTotalCalories, grandTotalProtein, grandTotalCarbs, grandTotalFat) = attemptResult;

                _logger.LogInformation("Applied {dayCount} days of meal plan for user {userId}, {foodCount} total foods",
                    daysToApply.Count, totalFoodsAdded, userId);

                return Ok(new ApplyMealPlanWeekResponse
                {
                    Success = true,
                    Message = $"Applied {daysToApply.Count} days of meals ({totalFoodsAdded} foods total)",
                    DaysApplied = daysToApply.Count,
                    TotalFoodsAdded = totalFoodsAdded,
                    TotalCalories = grandTotalCalories,
                    TotalProtein = grandTotalProtein,
                    TotalCarbs = grandTotalCarbs,
                    TotalFat = grandTotalFat,
                    DayResults = dayResults
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying meal plan week");
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
        public string? ProgramName { get; set; }
        public string? SplitType { get; set; }
        public int? TotalWeeks { get; set; }
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
    // 7-day meal plan extraction structure
    public class ChatMealPlanWeekExtraction
    {
        public decimal TargetCalories { get; set; }
        public List<ChatMealPlanDayData> Days { get; set; } = new();
    }

    public class ChatMealPlanDayData
    {
        public int Day { get; set; }
        public List<ChatMealPlanMealData> Meals { get; set; } = new();
        public decimal TotalCalories { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TotalCarbs { get; set; }
        public decimal TotalFat { get; set; }
    }

    // Legacy single-day structure (kept for compatibility)
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

    // Response for previewing 7-day meal plan
    public class MealPlanPreviewResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public decimal TargetCalories { get; set; }
        public List<MealPlanDayPreview> Days { get; set; } = new();
    }

    public class MealPlanDayPreview
    {
        public int Day { get; set; }
        public string Summary { get; set; } = "";
        public decimal TotalCalories { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TotalCarbs { get; set; }
        public decimal TotalFat { get; set; }
        public bool IsWithinTarget { get; set; }
        public List<MealPreview> Meals { get; set; } = new();
    }

    public class MealPreview
    {
        public string MealType { get; set; } = "";
        public List<string> Foods { get; set; } = new();
        public decimal Calories { get; set; }
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
        /// <summary>
        /// Indicates that the nutrition goal was updated to match this day's plan
        /// </summary>
        public bool GoalUpdated { get; set; }
        /// <summary>
        /// The new daily calorie goal (after update)
        /// </summary>
        public decimal? NewDailyCalorieGoal { get; set; }
        /// <summary>
        /// The new daily protein goal (after update)
        /// </summary>
        public decimal? NewDailyProteinGoal { get; set; }
        /// <summary>
        /// The new daily carbs goal (after update)
        /// </summary>
        public decimal? NewDailyCarbsGoal { get; set; }
        /// <summary>
        /// The new daily fat goal (after update)
        /// </summary>
        public decimal? NewDailyFatGoal { get; set; }
        /// <summary>
        /// Meal types left untouched because they were already marked consumed (actually eaten).
        /// </summary>
        public List<string> SkippedMealTypes { get; set; } = new();
        /// <summary>
        /// Meal types where a previously-applied planned suggestion was replaced by this apply.
        /// </summary>
        public List<string> ReplacedMealTypes { get; set; } = new();
    }

    // Request for applying multiple days of meal plan
    public class ApplyMealPlanWeekRequest
    {
        /// <summary>
        /// If true, applies all 7 days. If false, uses the Days list.
        /// </summary>
        public bool ApplyAllDays { get; set; } = false;

        /// <summary>
        /// Specific days to apply (1-7). Ignored if ApplyAllDays is true.
        /// </summary>
        public List<int>? Days { get; set; }

        /// <summary>
        /// The date to start applying from. Defaults to today.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// If true, replaces previously-applied PLANNED suggestions in the targeted meal
        /// entries (any source, not just this conversation) before adding the new ones.
        /// If false, only this exact suggestion's own previously-applied items (same
        /// conversation + day) are replaced, and other planned items are left alone.
        /// Either way, a meal entry the user has already marked consumed (actually eaten)
        /// is never touched, regardless of this flag.
        /// </summary>
        public bool OverwriteExisting { get; set; } = true;
    }

    // Response for applying multiple days
    public class ApplyMealPlanWeekResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int DaysApplied { get; set; }
        public int TotalFoodsAdded { get; set; }
        public decimal TotalCalories { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TotalCarbs { get; set; }
        public decimal TotalFat { get; set; }
        public List<DayApplyResult> DayResults { get; set; } = new();
    }

    // Result for each day applied
    public class DayApplyResult
    {
        public int Day { get; set; }
        public DateTime Date { get; set; }
        public int FoodsAdded { get; set; }
        public decimal Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbs { get; set; }
        public decimal Fat { get; set; }
        /// <summary>
        /// Meal types left untouched for this day because they were already consumed.
        /// </summary>
        public List<string> SkippedMealTypes { get; set; } = new();
        /// <summary>
        /// Meal types where a previously-applied planned suggestion was replaced for this day.
        /// </summary>
        public List<string> ReplacedMealTypes { get; set; } = new();
    }
}
