using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GoHardAPI.Data;
using GoHardAPI.Models;
using System.Security.Claims;

namespace GoHardAPI.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    public class ProgramsController : ControllerBase
    {
        private readonly TrainingContext _context;

        public ProgramsController(TrainingContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        /// <summary>
        /// Get all programs for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Models.Program>>> GetPrograms([FromQuery] bool? isActive = null)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var query = _context.Programs
                .Where(p => p.UserId == userId);

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            var programs = await query
                .Include(p => p.Workouts.OrderBy(w => w.WeekNumber).ThenBy(w => w.OrderIndex))
                .Include(p => p.Goal)
                .OrderByDescending(p => p.IsActive)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(programs);
        }

        /// <summary>
        /// Get a specific program by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Models.Program>> GetProgram(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var program = await _context.Programs
                .Include(p => p.Workouts.OrderBy(w => w.WeekNumber).ThenBy(w => w.OrderIndex))
                .Include(p => p.Goal)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (program == null)
            {
                return NotFound();
            }

            return Ok(program);
        }

        /// <summary>
        /// Create a new program
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Models.Program>> CreateProgram(Models.Program program)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            program.UserId = userId;
            program.CreatedAt = DateTime.UtcNow;
            program.StartDate = program.StartDate == default ? DateTime.UtcNow : program.StartDate;
            program.IsCompleted = false;
            program.CurrentWeek = 1;
            program.CurrentDay = 1; // Always start at Day 1 (session-based, not calendar)

            // Calculate end date if not provided
            if (program.EndDate == null)
            {
                program.EndDate = program.StartDate.AddDays(program.TotalWeeks * 7);
            }

            _context.Programs.Add(program);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProgram), new { id = program.Id }, program);
        }

        /// <summary>
        /// Update an existing program
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProgram(int id, Models.Program program)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            if (id != program.Id)
            {
                return BadRequest();
            }

            var existingProgram = await _context.Programs.FindAsync(id);
            if (existingProgram == null || existingProgram.UserId != userId)
            {
                return NotFound();
            }

            // Update fields
            existingProgram.Title = program.Title;
            existingProgram.Description = program.Description;
            existingProgram.GoalId = program.GoalId;
            existingProgram.TotalWeeks = program.TotalWeeks;
            existingProgram.CurrentWeek = program.CurrentWeek;
            existingProgram.CurrentDay = program.CurrentDay;
            existingProgram.IsActive = program.IsActive;
            existingProgram.ProgramStructure = program.ProgramStructure;
            existingProgram.EndDate = program.EndDate;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProgramExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        /// <summary>
        /// Get deletion impact for a program (comprehensive impact summary)
        /// </summary>
        [HttpGet("{id}/deletion-impact")]
        public async Task<ActionResult<object>> GetDeletionImpact(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var program = await _context.Programs
                .Include(p => p.Workouts)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (program == null)
            {
                return NotFound();
            }

            // Count sessions linked to this program
            var sessions = await _context.Sessions
                .Where(s => s.ProgramId == id && s.UserId == userId)
                .Include(s => s.Exercises)
                .ThenInclude(e => e.ExerciseSets)
                .ToListAsync();

            var sessionsCount = sessions.Count;
            var completedSessionsCount = sessions.Count(s => s.Status == "completed");
            var exercisesCount = sessions.Sum(s => s.Exercises?.Count ?? 0);
            var setsCount = sessions.Sum(s => s.Exercises?.Sum(e => e.ExerciseSets?.Count ?? 0) ?? 0);

            // Count program workouts
            var workoutsCount = program.Workouts?.Count ?? 0;
            var completedWorkoutsCount = program.Workouts?.Count(w => w.IsCompleted) ?? 0;

            return Ok(new
            {
                programTitle = program.Title,
                sessionsCount,
                completedSessionsCount,
                exercisesCount,
                setsCount,
                workoutsCount,
                completedWorkoutsCount,
                hasData = sessionsCount > 0 || workoutsCount > 0,
                warning = sessionsCount > 0
                    ? $"This will permanently delete {sessionsCount} session(s), including {completedSessionsCount} completed workout(s) with {exercisesCount} exercises and {setsCount} sets."
                    : null
            });
        }

        /// <summary>
        /// Delete a program
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProgram(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var program = await _context.Programs.FindAsync(id);
            if (program == null || program.UserId != userId)
            {
                return NotFound();
            }

            _context.Programs.Remove(program);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Mark a program as completed
        /// </summary>
        [HttpPut("{id}/complete")]
        public async Task<IActionResult> CompleteProgram(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var program = await _context.Programs.FindAsync(id);
            if (program == null || program.UserId != userId)
            {
                return NotFound();
            }

            program.IsCompleted = true;
            program.CompletedAt = DateTime.UtcNow;
            program.IsActive = false;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Advance to next workout (increment day/week)
        /// </summary>
        [HttpPut("{id}/advance")]
        public async Task<IActionResult> AdvanceProgram(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var program = await _context.Programs.FindAsync(id);
            if (program == null || program.UserId != userId)
            {
                return NotFound();
            }

            // Increment day
            program.CurrentDay++;

            // If we've gone past day 7, move to next week
            if (program.CurrentDay > 7)
            {
                program.CurrentDay = 1;
                program.CurrentWeek++;

                // Check if program is completed
                if (program.CurrentWeek > program.TotalWeeks)
                {
                    program.IsCompleted = true;
                    program.CompletedAt = DateTime.UtcNow;
                    program.IsActive = false;
                    program.CurrentWeek = program.TotalWeeks; // Cap at total weeks
                    program.CurrentDay = 7; // Cap at day 7
                }
            }

            await _context.SaveChangesAsync();

            return Ok(program);
        }

        /// <summary>
        /// Get workouts for a specific week
        /// </summary>
        [HttpGet("{id}/weeks/{weekNumber}")]
        public async Task<ActionResult<IEnumerable<ProgramWorkout>>> GetWeekWorkouts(int id, int weekNumber)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var program = await _context.Programs.FindAsync(id);
            if (program == null || program.UserId != userId)
            {
                return NotFound();
            }

            var workouts = await _context.ProgramWorkouts
                .Where(w => w.ProgramId == id && w.WeekNumber == weekNumber)
                .OrderBy(w => w.OrderIndex)
                .ToListAsync();

            return Ok(workouts);
        }

        /// <summary>
        /// Get today's workout
        /// </summary>
        [HttpGet("{id}/today")]
        public async Task<ActionResult<ProgramWorkout>> GetTodaysWorkout(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var program = await _context.Programs.FindAsync(id);
            if (program == null || program.UserId != userId)
            {
                return NotFound();
            }

            var workout = await _context.ProgramWorkouts
                .FirstOrDefaultAsync(w => w.ProgramId == id &&
                                          w.WeekNumber == program.CurrentWeek &&
                                          w.DayNumber == program.CurrentDay);

            if (workout == null)
            {
                return NotFound("No workout scheduled for today");
            }

            return Ok(workout);
        }

        /// <summary>
        /// Add a workout to a program
        /// </summary>
        [HttpPost("{id}/workouts")]
        public async Task<ActionResult<ProgramWorkout>> AddWorkout(int id, [FromBody] ProgramWorkout workout)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var program = await _context.Programs.FindAsync(id);
            if (program == null || program.UserId != userId)
            {
                return NotFound();
            }

            workout.ProgramId = id;
            _context.ProgramWorkouts.Add(workout);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProgram), new { id = program.Id }, workout);
        }

        /// <summary>
        /// Update a program workout
        /// </summary>
        [HttpPut("workouts/{workoutId}")]
        public async Task<IActionResult> UpdateWorkout(int workoutId, [FromBody] ProgramWorkout workout)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            if (workoutId != workout.Id)
            {
                return BadRequest();
            }

            var existingWorkout = await _context.ProgramWorkouts
                .Include(w => w.Program)
                .FirstOrDefaultAsync(w => w.Id == workoutId);

            if (existingWorkout == null || existingWorkout.Program?.UserId != userId)
            {
                return NotFound();
            }

            // Update fields
            existingWorkout.WorkoutName = workout.WorkoutName;
            existingWorkout.WorkoutType = workout.WorkoutType;
            existingWorkout.Description = workout.Description;
            existingWorkout.EstimatedDuration = workout.EstimatedDuration;
            existingWorkout.ExercisesJson = workout.ExercisesJson;
            existingWorkout.WarmUp = workout.WarmUp;
            existingWorkout.CoolDown = workout.CoolDown;
            existingWorkout.IsCompleted = workout.IsCompleted;
            existingWorkout.CompletedAt = workout.CompletedAt;
            existingWorkout.CompletionNotes = workout.CompletionNotes;
            existingWorkout.WeekNumber = workout.WeekNumber;
            existingWorkout.DayNumber = workout.DayNumber;
            existingWorkout.OrderIndex = workout.OrderIndex;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound();
            }

            return NoContent();
        }

        /// <summary>
        /// Mark a workout as completed
        /// </summary>
        [HttpPut("workouts/{workoutId}/complete")]
        public async Task<IActionResult> CompleteWorkout(int workoutId, [FromBody] string? notes = null)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var workout = await _context.ProgramWorkouts
                .Include(w => w.Program)
                .FirstOrDefaultAsync(w => w.Id == workoutId);

            if (workout == null || workout.Program?.UserId != userId)
            {
                return NotFound();
            }

            workout.IsCompleted = true;
            workout.CompletedAt = DateTime.UtcNow;
            workout.CompletionNotes = notes;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Swap two program workouts atomically (exchanges their day numbers and order indexes)
        /// </summary>
        [HttpPost("workouts/swap")]
        public async Task<IActionResult> SwapWorkouts([FromBody] SwapWorkoutsRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            // Begin transaction for atomic swap
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Fetch both workouts
                var workout1 = await _context.ProgramWorkouts
                    .Include(w => w.Program)
                    .FirstOrDefaultAsync(w => w.Id == request.Workout1Id);

                var workout2 = await _context.ProgramWorkouts
                    .Include(w => w.Program)
                    .FirstOrDefaultAsync(w => w.Id == request.Workout2Id);

                // Validate both workouts exist and belong to user
                if (workout1 == null || workout1.Program?.UserId != userId)
                {
                    return NotFound("Workout 1 not found or access denied");
                }

                if (workout2 == null || workout2.Program?.UserId != userId)
                {
                    return NotFound("Workout 2 not found or access denied");
                }

                // Validate both workouts belong to same program
                if (workout1.ProgramId != workout2.ProgramId)
                {
                    return BadRequest("Workouts must belong to the same program");
                }

                // Store original values
                var day1 = workout1.DayNumber;
                var order1 = workout1.OrderIndex;
                var day2 = workout2.DayNumber;
                var order2 = workout2.OrderIndex;

                // Use raw SQL to avoid EF circular dependency with unique constraint
                // Strategy: Use temporary value (-999) to break the circular dependency

                // Step 1: Set workout1 to temporary values (outside valid range)
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"ProgramWorkouts\" SET \"DayNumber\" = {0}, \"OrderIndex\" = {1} WHERE \"Id\" = {2}",
                    -999, -999, workout1.Id);

                // Step 2: Update workout2 to workout1's original values
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"ProgramWorkouts\" SET \"DayNumber\" = {0}, \"OrderIndex\" = {1} WHERE \"Id\" = {2}",
                    day1, order1, workout2.Id);

                // Step 3: Update workout1 to workout2's original values
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"ProgramWorkouts\" SET \"DayNumber\" = {0}, \"OrderIndex\" = {1} WHERE \"Id\" = {2}",
                    day2, order2, workout1.Id);

                // Commit transaction
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Workouts swapped successfully",
                    workout1 = new { Id = workout1.Id, DayNumber = day2, OrderIndex = order2 },
                    workout2 = new { Id = workout2.Id, DayNumber = day1, OrderIndex = order1 }
                });
            }
            catch (Exception)
            {
                // Rollback on error
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Failed to swap workouts. Please try again." });
            }
        }

        /// <summary>
        /// Delete a workout
        /// </summary>
        [HttpDelete("workouts/{workoutId}")]
        public async Task<IActionResult> DeleteWorkout(int workoutId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var workout = await _context.ProgramWorkouts
                .Include(w => w.Program)
                .FirstOrDefaultAsync(w => w.Id == workoutId);

            if (workout == null || workout.Program?.UserId != userId)
            {
                return NotFound();
            }

            _context.ProgramWorkouts.Remove(workout);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProgramExists(int id)
        {
            return _context.Programs.Any(e => e.Id == id);
        }
    }
}
