using Asp.Versioning;
using GoHardAPI.Data;
using GoHardAPI.DTOs;
using GoHardAPI.Models;
using GoHardAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace GoHardAPI.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    public class SessionsController : ControllerBase
    {
        private readonly TrainingContext _context;
        private readonly SessionCreateService _sessionCreateService;

        public SessionsController(TrainingContext context, SessionCreateService sessionCreateService)
        {
            _context = context;
            _sessionCreateService = sessionCreateService;
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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Session>>> GetSessions()
        {
            var userId = GetCurrentUserId();
            var sessions = await _context.Sessions
                .Where(s => s.UserId == userId)
                .Include(s => s.Exercises.OrderBy(e => e.SortOrder))
                    .ThenInclude(e => e.ExerciseSets)
                .Include(s => s.Exercises)
                    .ThenInclude(e => e.ExerciseTemplate)
                .ToListAsync();

            return sessions;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Session>> GetSession(int id)
        {
            var userId = GetCurrentUserId();
            var session = await _context.Sessions
                .Include(s => s.Exercises.OrderBy(e => e.SortOrder))
                    .ThenInclude(e => e.ExerciseSets)
                .Include(s => s.Exercises)
                    .ThenInclude(e => e.ExerciseTemplate)
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (session == null)
            {
                return NotFound();
            }

            return session;
        }

        /// <summary>
        /// Creates a workout session.
        ///
        /// Without <c>clientOperationId</c> the behavior is unchanged: a new session is
        /// created and returned with <c>201</c>.
        ///
        /// With <c>clientOperationId</c> the call is idempotent per
        /// <c>(authenticated user, clientOperationId)</c>:
        /// <list type="bullet">
        ///   <item>first call: <c>201</c> with the canonical session;</item>
        ///   <item>replay (identical or conflicting body): <c>200</c> with the original
        ///     canonical session, unchanged — first writer wins;</item>
        ///   <item>completed operation whose session was deleted: <c>410</c>
        ///     <c>operation_target_deleted</c> — never recreated.</item>
        /// </list>
        /// A supplied <c>programId</c> / <c>programWorkoutId</c> must resolve to a resource
        /// owned by the authenticated user (and, if both are given, the workout must belong
        /// to that program); a missing OR foreign resource returns the same non-disclosing
        /// <c>404 { code: "program_not_found" }</c>. This validation runs only for the FIRST
        /// keyed write and for legacy creates — a keyed replay never revalidates.
        ///
        /// The owner always comes from the JWT; the request body cannot set an id, user,
        /// version or child exercises.
        ///
        /// NOTE: <c>operation_canceled</c> (409) is defined but DORMANT in this PR — nothing
        /// writes <c>SessionCreateOperation.CanceledAt</c> until the P2 DELETE-by-operation-key
        /// endpoint lands.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SessionResponseDto>> CreateSession(
            [FromBody] SessionCreateRequestDto request, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            var outcome = await _sessionCreateService.CreateAsync(userId, request, cancellationToken);

            switch (outcome.Result)
            {
                case SessionCreateResult.Created:
                    return CreatedAtAction(
                        nameof(GetSession),
                        new { id = outcome.Session!.Id },
                        SessionResponseDto.FromEntity(outcome.Session));

                case SessionCreateResult.ReplayedExisting:
                    return Ok(SessionResponseDto.FromEntity(outcome.Session!));

                case SessionCreateResult.ProgramNotFound:
                    // Same response for "missing" and "belongs to another user" — no
                    // cross-user existence oracle.
                    return NotFound(new { code = outcome.ErrorCode });

                case SessionCreateResult.Canceled:
                    return Conflict(new { code = outcome.ErrorCode });

                case SessionCreateResult.Gone:
                    return StatusCode(StatusCodes.Status410Gone, new { code = outcome.ErrorCode });

                case SessionCreateResult.Incomplete:
                    return Conflict(new { code = outcome.ErrorCode });

                default:
                    return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Creates a new session from a program workout
        /// Copies exercises from the program workout and links the session to the program
        /// </summary>
        [HttpPost("from-program-workout")]
        public async Task<ActionResult<Session>> CreateSessionFromProgramWorkout([FromBody] CreateSessionFromProgramWorkoutDto dto)
        {
            var userId = GetCurrentUserId();

            // Get the program workout with its program
            var programWorkout = await _context.ProgramWorkouts
                .Include(pw => pw.Program)
                .FirstOrDefaultAsync(pw => pw.Id == dto.ProgramWorkoutId);

            if (programWorkout == null)
            {
                return NotFound("Program workout not found");
            }

            // Verify user owns the program
            if (programWorkout.Program.UserId != userId)
            {
                return Unauthorized("You don't have access to this program");
            }

            // Use the stored ScheduledDate if available, otherwise calculate it
            // ScheduledDate is set when the program is created to avoid timezone issues
            var scheduledDate = programWorkout.ScheduledDate?.Date
                ?? programWorkout.Program.StartDate
                    .AddDays((programWorkout.WeekNumber - 1) * 7 + (programWorkout.DayNumber - 1))
                    .Date;

            var today = DateTime.UtcNow.Date;

            // Determine status based on scheduled date
            // - If scheduled for future: status = 'planned'
            // - If scheduled for today or past: status = 'draft' (user can start immediately)
            var status = scheduledDate > today ? SessionStatus.Planned : SessionStatus.Draft;

            // Create session linked to program workout
            var session = new Session
            {
                UserId = userId,
                Date = scheduledDate, // Use calculated scheduled date
                Name = programWorkout.WorkoutName,
                Type = programWorkout.WorkoutType ?? "Workout",
                Status = status, // Use calculated status
                ProgramId = dto.ProgramId, // Use ProgramId from request (fixes issue with old ProgramWorkout data)
                ProgramWorkoutId = programWorkout.Id
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync(); // Save to get the session ID

            // Parse exercises from JSON and create Exercise records
            try
            {
                var exercisesData = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(programWorkout.ExercisesJson);

                if (exercisesData != null)
                {
                    foreach (var exerciseData in exercisesData)
                    {
                        var exercise = new Exercise
                        {
                            SessionId = session.Id,
                            Name = exerciseData.ContainsKey("name") ? exerciseData["name"].GetString() ?? "Exercise" : "Exercise"
                        };

                        // Copy other fields if they exist
                        if (exerciseData.ContainsKey("exerciseTemplateId") && exerciseData["exerciseTemplateId"].ValueKind != JsonValueKind.Null)
                        {
                            exercise.ExerciseTemplateId = exerciseData["exerciseTemplateId"].GetInt32();
                        }

                        if (exerciseData.ContainsKey("notes"))
                        {
                            exercise.Notes = exerciseData["notes"].GetString();
                        }

                        if (exerciseData.ContainsKey("rest") && exerciseData["rest"].ValueKind != JsonValueKind.Null)
                        {
                            exercise.RestTime = exerciseData["rest"].GetInt32();
                        }

                        _context.Exercises.Add(exercise);
                    }

                    await _context.SaveChangesAsync();
                }
            }
            catch (JsonException ex)
            {
                // If JSON parsing fails, return error instead of silently creating empty session
                Console.WriteLine($"Error parsing exercises JSON: {ex.Message}");

                // Delete the session since we couldn't create exercises
                _context.Sessions.Remove(session);
                await _context.SaveChangesAsync();

                return BadRequest(new
                {
                    message = "Failed to parse exercises from program workout",
                    error = ex.Message
                });
            }

            // Reload session with exercises
            var createdSession = await _context.Sessions
                .Include(s => s.Exercises)
                    .ThenInclude(e => e.ExerciseSets)
                .Include(s => s.Exercises)
                    .ThenInclude(e => e.ExerciseTemplate)
                .FirstOrDefaultAsync(s => s.Id == session.Id);

            return CreatedAtAction(nameof(GetSession), new { id = session.Id }, createdSession);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSession(int id, [FromBody] SessionUpdateRequestDto request)
        {
            var userId = GetCurrentUserId();

            // Verify the session belongs to the current user. The route id identifies
            // the session and the JWT identifies its owner - the request body carries
            // neither Id nor UserId, so there is nothing here for a client to spoof.
            var existingSession = await _context.Sessions.FindAsync(id);
            if (existingSession == null || existingSession.UserId != userId)
            {
                return NotFound();
            }

            // Resolve the submitted version. Missing version (legacy clients that predate
            // version tracking) falls back to 1, which is the version every session starts
            // at - this preserves legacy behavior without bypassing conflict detection:
            // it still has to match the stored version like any explicit value would.
            var submittedVersion = request.Version ?? 1;

            // Check version for conflict detection (Issue #13)
            if (submittedVersion != existingSession.Version)
            {
                return Conflict(new
                {
                    message = "Version conflict - data was modified by another device",
                    currentVersion = existingSession.Version,
                    serverData = SessionResponseDto.FromEntity(existingSession)
                });
            }

            // Increment version on update
            existingSession.Version = submittedVersion + 1;

            // Update the existing tracked entity instead of tracking a new one
            existingSession.Name = request.Name;
            existingSession.Type = request.Type;
            existingSession.Status = request.Status;
            existingSession.Date = request.Date;
            existingSession.Duration = request.Duration;
            existingSession.Notes = request.Notes;
            existingSession.StartedAt = request.StartedAt;
            existingSession.PausedAt = request.PausedAt;
            existingSession.CompletedAt = request.CompletedAt;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Sessions.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(SessionResponseDto.FromEntity(existingSession));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSession(int id)
        {
            var userId = GetCurrentUserId();
            var Session = await _context.Sessions.FindAsync(id);

            if (Session == null || Session.UserId != userId)
            {
                return NotFound();
            }

            _context.Sessions.Remove(Session);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/Sessions/5/start-planned
        // Atomic operation to update both date and status when starting a planned workout
        [HttpPatch("{id}/start-planned")]
        public async Task<IActionResult> StartPlannedWorkout(int id, [FromBody] StartPlannedWorkoutRequest request)
        {
            var userId = GetCurrentUserId();
            var session = await _context.Sessions.FindAsync(id);

            if (session == null || session.UserId != userId)
            {
                return NotFound();
            }

            // Atomic update: date and status together
            session.Date = request.Date ?? DateTime.UtcNow.Date;
            session.Status = SessionStatus.InProgress;
            session.StartedAt = request.StartedAt ?? DateTime.UtcNow;
            session.PausedAt = null; // Clear any pause state

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/Sessions/5/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateSessionStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            var userId = GetCurrentUserId();
            var session = await _context.Sessions.FindAsync(id);

            // DEBUG: Log what we received
            Console.WriteLine($"🔽 SERVER RECEIVED UpdateStatusRequest:");
            Console.WriteLine($"   Status: {request.Status}");
            Console.WriteLine($"   StartedAt.HasValue: {request.StartedAt.HasValue}");
            if (request.StartedAt.HasValue)
            {
                Console.WriteLine($"   StartedAt.Value: {request.StartedAt.Value}");
                Console.WriteLine($"   StartedAt.Kind: {request.StartedAt.Value.Kind}");
                Console.WriteLine($"   StartedAt.Hour: {request.StartedAt.Value.Hour}");
            }
            Console.WriteLine($"   PausedAt.HasValue: {request.PausedAt.HasValue}");
            if (request.PausedAt.HasValue)
            {
                Console.WriteLine($"   PausedAt.Value: {request.PausedAt.Value}");
                Console.WriteLine($"   PausedAt.Kind: {request.PausedAt.Value.Kind}");
            }

            if (session == null || session.UserId != userId)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(request.Status))
            {
                return BadRequest(new { message = "Status cannot be empty." });
            }

            // Validate status value
            if (!SessionStatus.IsValid(request.Status))
            {
                return BadRequest(new { message = $"Invalid status. Must be one of: {string.Join(", ", SessionStatus.ValidStatuses)}" });
            }

            // Validate status transition (Issue #3 - prevent invalid state changes)
            if (!SessionStatus.IsValidTransition(session.Status, request.Status))
            {
                return BadRequest(new
                {
                    message = SessionStatus.GetTransitionError(session.Status, request.Status),
                    currentStatus = session.Status,
                    requestedStatus = request.Status.ToLower()
                });
            }

            session.Status = request.Status.ToLower();

            // Update timestamps - always accept client's timestamps for timer accuracy
            // This is critical for pause/resume sync (Issue #1 - startedAt must be updatable)
            if (request.StartedAt.HasValue)
            {
                Console.WriteLine($"   🕐 Setting session.StartedAt from request: {request.StartedAt.Value} (Kind: {request.StartedAt.Value.Kind})");
                session.StartedAt = request.StartedAt.Value;
                Console.WriteLine($"   🕐 After assignment - session.StartedAt: {session.StartedAt} (Kind: {session.StartedAt?.Kind})");
            }
            else if (request.Status.Equals(SessionStatus.InProgress, StringComparison.OrdinalIgnoreCase) && session.StartedAt == null)
            {
                // Only auto-generate if not provided and session hasn't started
                var utcNow = DateTime.UtcNow;
                Console.WriteLine($"   🕐 Auto-generating session.StartedAt: {utcNow} (Kind: {utcNow.Kind})");
                session.StartedAt = utcNow;
            }

            // Handle completion timestamp
            if (request.CompletedAt.HasValue)
            {
                session.CompletedAt = request.CompletedAt.Value;
            }
            else if (request.Status.Equals(SessionStatus.Completed, StringComparison.OrdinalIgnoreCase) && session.CompletedAt == null)
            {
                session.CompletedAt = DateTime.UtcNow;
            }

            // Auto-update workout goals on completion
            if (request.Status.Equals(SessionStatus.Completed, StringComparison.OrdinalIgnoreCase) && session.CompletedAt != null)
            {
                await UpdateWorkoutGoals(userId, session.CompletedAt.Value);
            }

            // Update paused state (Issue #2 - handle clearing pausedAt on resume)
            if (request.ClearPausedAt)
            {
                session.PausedAt = null;
            }
            else if (request.PausedAt.HasValue)
            {
                session.PausedAt = request.PausedAt;
            }

            // Update duration if provided (from timer elapsed time)
            if (request.Duration.HasValue)
            {
                session.Duration = request.Duration.Value;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // NOTE: Pause/Resume endpoints removed - use PATCH /status endpoint instead
        // The status endpoint handles pause/resume through the pausedAt and startedAt timestamps

        // POST: api/Sessions/5/exercises
        [HttpPost("{id}/exercises")]
        public async Task<ActionResult<Exercise>> AddExerciseToSession(int id, [FromBody] AddExerciseRequest request)
        {
            var userId = GetCurrentUserId();
            var session = await _context.Sessions.FindAsync(id);

            if (session == null || session.UserId != userId)
            {
                return NotFound(new { message = "Session not found" });
            }

            var template = await _context.ExerciseTemplates.FindAsync(request.ExerciseTemplateId);
            if (template == null)
            {
                return NotFound(new { message = "Exercise template not found" });
            }

            // Get the max sort order for existing exercises in this session
            var maxSortOrder = await _context.Exercises
                .Where(e => e.SessionId == id)
                .Select(e => (int?)e.SortOrder)
                .MaxAsync() ?? -1;

            // Create a new exercise instance from the template
            var exercise = new Exercise
            {
                SessionId = id,
                Name = template.Name,
                ExerciseTemplateId = request.ExerciseTemplateId,
                SortOrder = maxSortOrder + 1
            };

            _context.Exercises.Add(exercise);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSession), new { id = session.Id }, exercise);
        }

        /// <summary>
        /// Reorder exercises within a session (drag-and-drop support)
        /// </summary>
        [HttpPatch("{id}/exercises/reorder")]
        public async Task<IActionResult> ReorderExercises(int id, [FromBody] ReorderExercisesRequest request)
        {
            var userId = GetCurrentUserId();
            var session = await _context.Sessions
                .Include(s => s.Exercises)
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (session == null)
            {
                return NotFound(new { message = "Session not found" });
            }

            // Validate that all exercise IDs belong to this session
            var sessionExerciseIds = session.Exercises.Select(e => e.Id).ToHashSet();
            var requestedIds = request.ExerciseIds.ToHashSet();

            if (!requestedIds.SetEquals(sessionExerciseIds))
            {
                return BadRequest(new
                {
                    message = "Exercise IDs must match exactly the exercises in this session",
                    expected = sessionExerciseIds.OrderBy(x => x),
                    received = requestedIds.OrderBy(x => x)
                });
            }

            // Update sort order based on position in the list
            for (int i = 0; i < request.ExerciseIds.Count; i++)
            {
                var exercise = session.Exercises.First(e => e.Id == request.ExerciseIds[i]);
                exercise.SortOrder = i;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task UpdateWorkoutGoals(int userId, DateTime workoutDate)
        {
            // Get active workout frequency goals
            var workoutGoals = await _context.Goals
                .Where(g => g.UserId == userId &&
                            g.IsActive &&
                            !g.IsCompleted &&
                            (g.GoalType.ToLower().Contains("workout") ||
                             g.GoalType.ToLower().Contains("frequency") ||
                             g.GoalType.ToLower().Contains("training")))
                .ToListAsync();

            foreach (var goal in workoutGoals)
            {
                // Determine if this workout counts toward the goal's time frame
                bool countsTowardGoal = ShouldCountWorkout(goal, workoutDate);

                if (countsTowardGoal)
                {
                    // Increment the goal's current value
                    goal.CurrentValue += 1;

                    // Add progress entry for tracking
                    var progress = new GoalProgress
                    {
                        GoalId = goal.Id,
                        RecordedAt = DateTime.UtcNow,
                        Value = goal.CurrentValue,
                        Notes = "Auto-tracked from workout completion"
                    };

                    _context.GoalProgressHistory.Add(progress);

                    // Check if goal is now complete
                    if (goal.CurrentValue >= goal.TargetValue)
                    {
                        goal.IsCompleted = true;
                        goal.CompletedAt = DateTime.UtcNow;
                        goal.IsActive = false;
                    }
                }
            }
        }

        private bool ShouldCountWorkout(Goal goal, DateTime workoutDate)
        {
            var now = DateTime.UtcNow;

            switch (goal.TimeFrame?.ToLower())
            {
                case "daily":
                    return workoutDate.Date == now.Date;

                case "weekly":
                    return GetWeekNumber(workoutDate) == GetWeekNumber(now) &&
                           workoutDate.Year == now.Year;

                case "monthly":
                    return workoutDate.Month == now.Month &&
                           workoutDate.Year == now.Year;

                case "yearly":
                    return workoutDate.Year == now.Year;

                case "total":
                case null:
                    return true;  // All-time goals count any workout

                default:
                    return false;
            }
        }

        private int GetWeekNumber(DateTime date)
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            return culture.Calendar.GetWeekOfYear(
                date,
                System.Globalization.CalendarWeekRule.FirstDay,
                DayOfWeek.Monday
            );
        }
    }
}
