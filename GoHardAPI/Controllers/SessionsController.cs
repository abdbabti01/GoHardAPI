using Asp.Versioning;
using GoHardAPI.Data;
using GoHardAPI.DTOs;
using GoHardAPI.Models;
using GoHardAPI.RateLimiting;
using GoHardAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
        /// A keyed operation that was canceled through
        /// <c>DELETE /api/v1/sessions/by-operation/{clientOperationId}</c> returns
        /// <c>409 { code: "operation_canceled" }</c> and creates nothing — cancellation wins
        /// in every accepted create/cancel ordering.
        ///
        /// Rate limited per authenticated user by the <c>session-write</c> token-bucket
        /// policy (see <see cref="RateLimiting.SessionWriteRateLimiterPolicy"/>). An
        /// over-limit request is rejected with <c>429 { "code": "rate_limited" }</c> +
        /// <c>Retry-After</c> before this action runs — no Session or operation row is
        /// written. The only other endpoint on this policy is its cancel counterpart,
        /// <c>DELETE /api/v1/sessions/by-operation/{clientOperationId}</c> (the two share
        /// one per-user bucket because both write to <c>SessionCreateOperations</c>).
        /// </summary>
        [HttpPost]
        [EnableRateLimiting(SessionWriteRateLimiterPolicy.PolicyName)]
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
        /// Cancels a keyed Session CREATE by its <c>clientOperationId</c>.
        ///
        /// This is the server side of delete-during-create convergence: a Flutter Session
        /// can be deleted locally while its keyed CREATE (POST /api/v1/sessions with a
        /// <c>clientOperationId</c>) is still in flight, after which the client has no
        /// <c>serverId</c> with which to delete the remote Session. Cancelling by the
        /// operation key removes that orphan and blocks the key.
        ///
        /// Idempotent. Once accepted:
        /// <list type="bullet">
        ///   <item>no Session created by that operation remains — an already-committed
        ///     Session and its owned children are deleted through the established cascade;</item>
        ///   <item>a concurrent or later CREATE with the same key creates nothing and
        ///     returns <c>409 { code: "operation_canceled" }</c>;</item>
        ///   <item>repeated cancellation succeeds — always <c>204</c>.</item>
        /// </list>
        ///
        /// The authenticated user comes only from the JWT; lookup and mutation are scoped to
        /// that user. A key owned by another user, or one never used, returns the same
        /// <c>204</c> — there is no cross-user existence oracle, and no client-supplied
        /// userId is ever accepted. The empty GUID is rejected with
        /// <c>400 { code: "invalid_operation_key" }</c> (a non-parseable value is a
        /// model-binding <c>400</c> before this action runs).
        ///
        /// Ordinary <c>DELETE /api/v1/sessions/{id}</c> is unaffected.
        ///
        /// Rate limited per authenticated user by the same <c>session-write</c> token-bucket
        /// policy as keyed <c>POST /api/v1/sessions</c> — the two share one per-user bucket
        /// because both write to <c>SessionCreateOperations</c>. An over-limit request is
        /// rejected with <c>429 { "code": "rate_limited" }</c> + <c>Retry-After</c> before
        /// this action runs; cancellation is idempotent, so a client that honors
        /// <c>Retry-After</c> converges.
        /// </summary>
        [HttpDelete("by-operation/{clientOperationId}")]
        [EnableRateLimiting(SessionWriteRateLimiterPolicy.PolicyName)]
        public async Task<IActionResult> CancelSessionCreateByOperation(
            Guid clientOperationId, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            if (clientOperationId == Guid.Empty)
            {
                return BadRequest(new { code = SessionCancelErrorCodes.InvalidOperationKey });
            }

            await _sessionCreateService.CancelCreateAsync(userId, clientOperationId, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Creates a new session from a program workout: copies exercises from the program
        /// workout and links the session to the program.
        ///
        /// <para>Without <c>clientOperationId</c> the behavior is unchanged: a new session
        /// (plus its exercises) is created and returned with <c>201</c>, no operation row.</para>
        ///
        /// <para>With <c>clientOperationId</c> the call joins the <b>same</b> durable
        /// creation/cancellation protocol as keyed <c>POST /api/v1/sessions</c>, idempotent
        /// per <c>(authenticated user, clientOperationId)</c>:</para>
        /// <list type="bullet">
        ///   <item>first call: <c>201</c> with the canonical session and its exercises;</item>
        ///   <item>replay (identical, conflicting, different workout, or a key first used by
        ///     <c>POST /api/v1/sessions</c>): <c>200</c> with the original canonical session,
        ///     unchanged — first writer wins, no duplicate exercises, the source workout is
        ///     never re-read;</item>
        ///   <item>canceled via
        ///     <c>DELETE /api/v1/sessions/by-operation/{clientOperationId}</c>:
        ///     <c>409 { code: "operation_canceled" }</c>, nothing created;</item>
        ///   <item>completed operation whose session was deleted:
        ///     <c>410 { code: "operation_target_deleted" }</c> — never recreated;</item>
        ///   <item>unparseable source <c>ExercisesJson</c>:
        ///     <c>400 { code: "program_workout_data_invalid" }</c> (no tombstone; a corrected
        ///     retry can still succeed);</item>
        ///   <item>empty-GUID <c>clientOperationId</c>:
        ///     <c>400 { code: "invalid_operation_key" }</c>, rejected before any persistence
        ///     (a non-parseable value is a model-binding <c>400</c>). A <c>null</c> key keeps
        ///     the unkeyed path and is never treated as empty.</item>
        /// </list>
        /// The owner always comes from the JWT; a missing OR foreign program / workout returns
        /// the non-disclosing <c>404 { code: "program_not_found" }</c>.
        ///
        /// Rate limited per authenticated user by the shared <c>session-write</c> token-bucket
        /// policy — the same per-user bucket as <c>POST /api/v1/sessions</c> and
        /// <c>DELETE /api/v1/sessions/by-operation/{clientOperationId}</c>, because all three
        /// write to <c>SessionCreateOperations</c> / <c>Sessions</c>.
        /// </summary>
        [HttpPost("from-program-workout")]
        [EnableRateLimiting(SessionWriteRateLimiterPolicy.PolicyName)]
        public async Task<ActionResult<Session>> CreateSessionFromProgramWorkout(
            [FromBody] CreateSessionFromProgramWorkoutDto dto, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            if (dto.ClientOperationId is { })
            {
                return await CreateSessionFromProgramWorkoutKeyedAsync(userId, dto, cancellationToken);
            }

            // ---- legacy unkeyed path: response shapes preserved exactly ----
            var programWorkout = await _context.ProgramWorkouts
                .Include(pw => pw.Program)
                .FirstOrDefaultAsync(pw => pw.Id == dto.ProgramWorkoutId, cancellationToken);

            if (programWorkout == null)
            {
                return NotFound("Program workout not found");
            }

            if (programWorkout.Program.UserId != userId)
            {
                return Unauthorized("You don't have access to this program");
            }

            Session session;
            try
            {
                // Shared materializer: scheduled date, status, and the exercise copy live in
                // one place so this path and the keyed service path cannot drift.
                session = ProgramWorkoutSessionMaterializer.Build(userId, programWorkout, dto.ProgramId);
            }
            catch (ProgramWorkoutSessionMaterializer.ExercisesJsonFormatException ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                Console.WriteLine($"Error parsing exercises JSON: {detail}");
                return BadRequest(new
                {
                    message = "Failed to parse exercises from program workout",
                    error = detail
                });
            }

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync(cancellationToken);

            var createdSession = await _context.Sessions
                .Include(s => s.Exercises)
                    .ThenInclude(e => e.ExerciseSets)
                .Include(s => s.Exercises)
                    .ThenInclude(e => e.ExerciseTemplate)
                .FirstOrDefaultAsync(s => s.Id == session.Id, cancellationToken);

            return CreatedAtAction(nameof(GetSession), new { id = session.Id }, createdSession);
        }

        private async Task<ActionResult<Session>> CreateSessionFromProgramWorkoutKeyedAsync(
            int userId, CreateSessionFromProgramWorkoutDto dto, CancellationToken cancellationToken)
        {
            var outcome = await _sessionCreateService.CreateFromProgramWorkoutAsync(userId, dto, cancellationToken);

            switch (outcome.Result)
            {
                case SessionCreateResult.Created:
                case SessionCreateResult.ReplayedExisting:
                    {
                        // Reload the canonical session with its child graph for the body (the
                        // service returns the entity without children loaded).
                        var canonical = await _context.Sessions
                            .Include(s => s.Exercises.OrderBy(e => e.SortOrder))
                                .ThenInclude(e => e.ExerciseSets)
                            .Include(s => s.Exercises)
                                .ThenInclude(e => e.ExerciseTemplate)
                            .FirstOrDefaultAsync(
                                s => s.Id == outcome.Session!.Id && s.UserId == userId, cancellationToken);

                        // A concurrent DELETE /sessions/{id} or cancel between the service
                        // commit and this reload: treat it exactly like a completed operation
                        // whose Session is gone rather than returning a 200/201 with no body.
                        if (canonical is null)
                        {
                            return StatusCode(StatusCodes.Status410Gone,
                                new { code = SessionCreateErrorCodes.OperationTargetDeleted });
                        }

                        return outcome.Result == SessionCreateResult.Created
                            ? CreatedAtAction(nameof(GetSession), new { id = outcome.Session!.Id }, canonical)
                            : Ok(canonical);
                    }

                case SessionCreateResult.ProgramNotFound:
                    return NotFound(new { code = outcome.ErrorCode });

                case SessionCreateResult.ProgramWorkoutDataInvalid:
                    return BadRequest(new { code = outcome.ErrorCode });

                case SessionCreateResult.InvalidOperationKey:
                    // Empty-GUID key: rejected before persistence, same code the
                    // by-operation cancel endpoint returns for the empty GUID.
                    return BadRequest(new { code = outcome.ErrorCode });

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
