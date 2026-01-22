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
    public class AnalyticsController : ControllerBase
    {
        private readonly TrainingContext _context;

        public AnalyticsController(TrainingContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get current user ID from JWT token
        /// </summary>
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim ?? "0");
        }

        /// <summary>
        /// Get overall workout statistics for current user
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<WorkoutStats>> GetWorkoutStats()
        {
            var userId = GetCurrentUserId();

            var completedSessions = await _context.Sessions
                .Where(s => s.UserId == userId && s.Status == SessionStatus.Completed)
                .OrderBy(s => s.Date)
                .ToListAsync();

            if (!completedSessions.Any())
            {
                return Ok(new WorkoutStats
                {
                    TotalWorkouts = 0,
                    TotalDuration = 0,
                    AverageDuration = 0,
                    CurrentStreak = 0,
                    LongestStreak = 0,
                    WorkoutsThisWeek = 0,
                    WorkoutsThisMonth = 0,
                    TotalSets = 0,
                    TotalReps = 0,
                    TotalVolume = 0
                });
            }

            // Calculate basic stats
            var totalWorkouts = completedSessions.Count;
            var totalDuration = completedSessions.Sum(s => s.Duration ?? 0);
            var averageDuration = totalDuration / totalWorkouts;

            // Calculate streaks
            var (currentStreak, longestStreak) = CalculateStreaks(completedSessions);

            // This week and month
            var now = DateTime.UtcNow;
            // Calculate start of week (Monday) - consistent with Program model
            // DayOfWeek: Sunday=0, Monday=1, ..., Saturday=6
            // We want to go back to Monday
            var daysSinceMonday = ((int)now.DayOfWeek + 6) % 7; // Sunday=6, Monday=0
            var startOfWeek = now.Date.AddDays(-daysSinceMonday);
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            var workoutsThisWeek = completedSessions.Count(s => s.Date >= startOfWeek);
            var workoutsThisMonth = completedSessions.Count(s => s.Date >= startOfMonth);

            // Get all exercises and sets for these sessions
            var sessionIds = completedSessions.Select(s => s.Id).ToList();

            var allExercises = await _context.Exercises
                .Where(e => sessionIds.Contains(e.SessionId))
                .Include(e => e.ExerciseSets)
                .ToListAsync();

            var totalSets = allExercises.Sum(e => e.ExerciseSets.Count);
            var totalReps = allExercises
                .SelectMany(e => e.ExerciseSets)
                .Sum(s => s.Reps ?? 0);

            var totalVolume = allExercises
                .SelectMany(e => e.ExerciseSets)
                .Sum(s => (s.Reps ?? 0) * (s.Weight ?? 0));

            return Ok(new WorkoutStats
            {
                TotalWorkouts = totalWorkouts,
                TotalDuration = totalDuration,
                AverageDuration = averageDuration,
                CurrentStreak = currentStreak,
                LongestStreak = longestStreak,
                WorkoutsThisWeek = workoutsThisWeek,
                WorkoutsThisMonth = workoutsThisMonth,
                TotalSets = totalSets,
                TotalReps = totalReps,
                TotalVolume = totalVolume,
                FirstWorkoutDate = completedSessions.First().Date,
                LastWorkoutDate = completedSessions.Last().Date
            });
        }

        /// <summary>
        /// Get progress for all exercises
        /// </summary>
        [HttpGet("exercise-progress")]
        public async Task<ActionResult<List<ExerciseProgress>>> GetExerciseProgress()
        {
            var userId = GetCurrentUserId();

            var exercises = await _context.Exercises
                .Where(e => e.Session.UserId == userId && e.Session.Status == SessionStatus.Completed)
                .Include(e => e.ExerciseSets)
                .Include(e => e.ExerciseTemplate)
                .Include(e => e.Session)
                .ToListAsync();

            var grouped = exercises
                .Where(e => e.ExerciseTemplateId.HasValue)
                .GroupBy(e => new
                {
                    e.ExerciseTemplateId,
                    ExerciseName = e.ExerciseTemplate?.Name ?? e.Name
                })
                .Select(g =>
                {
                    // Get first and last exercises ordered by session date
                    var lastExercise = g.OrderByDescending(e => e.Session.Date).First();

                    // Get the first exercise that has weight data (skip exercises with no weight)
                    var firstExerciseWithWeight = g
                        .OrderBy(e => e.Session.Date)
                        .FirstOrDefault(e => e.ExerciseSets.Any() && e.ExerciseSets.Average(s => s.Weight ?? 0) > 0);

                    var allSets = g.SelectMany(e => e.ExerciseSets).ToList();
                    var maxWeightSet = allSets.OrderByDescending(s => s.Weight).FirstOrDefault();

                    var firstWeight = firstExerciseWithWeight != null && firstExerciseWithWeight.ExerciseSets.Any()
                        ? firstExerciseWithWeight.ExerciseSets.Average(s => s.Weight ?? 0)
                        : 0;
                    var lastWeight = lastExercise.ExerciseSets.Any()
                        ? lastExercise.ExerciseSets.Average(s => s.Weight ?? 0)
                        : 0;

                    // Calculate progress from first instance to most recent instance
                    double? progressPercentage = null;
                    var exerciseCount = g.Count();

                    // Only calculate if performed more than once and has weight data
                    if (exerciseCount > 1 && firstWeight > 0)
                    {
                        progressPercentage = ((lastWeight - firstWeight) / firstWeight) * 100;
                    }

                    return new ExerciseProgress
                    {
                        ExerciseTemplateId = g.Key.ExerciseTemplateId ?? 0,
                        ExerciseName = g.Key.ExerciseName,
                        TimesPerformed = g.Count(),
                        TotalVolume = allSets.Sum(s => (s.Reps ?? 0) * (s.Weight ?? 0)),
                        PersonalRecord = maxWeightSet?.Weight,
                        PersonalRecordDate = maxWeightSet != null
                            ? g.First(e => e.ExerciseSets.Any(s => s.Id == maxWeightSet.Id)).Session.Date
                            : null,
                        LastWeight = lastWeight > 0 ? lastWeight : null,
                        LastPerformedDate = lastExercise.Session.Date,
                        ProgressPercentage = progressPercentage
                    };
                })
                .OrderByDescending(p => p.TimesPerformed)
                .ToList();

            return Ok(grouped);
        }

        /// <summary>
        /// Get progress over time for a specific exercise
        /// </summary>
        [HttpGet("exercise-progress/{exerciseTemplateId}")]
        public async Task<ActionResult<List<ProgressDataPoint>>> GetExerciseProgressOverTime(
            int exerciseTemplateId,
            [FromQuery] int days = 90)
        {
            var userId = GetCurrentUserId();
            var startDate = DateTime.UtcNow.AddDays(-days);

            var exercises = await _context.Exercises
                .Where(e => e.Session.UserId == userId &&
                           e.Session.Status == SessionStatus.Completed &&
                           e.ExerciseTemplateId == exerciseTemplateId &&
                           e.Session.Date >= startDate)
                .Include(e => e.ExerciseSets)
                .Include(e => e.Session)
                .OrderBy(e => e.Session.Date)
                .ToListAsync();

            var dataPoints = exercises
                .Select(e => new ProgressDataPoint
                {
                    Date = e.Session.Date,
                    Value = e.ExerciseSets.Any()
                        ? e.ExerciseSets.Max(s => s.Weight ?? 0)
                        : 0,
                    Label = e.ExerciseSets.Any()
                        ? $"{e.ExerciseSets.Max(s => s.Weight ?? 0):F1} kg"
                        : "0 kg"
                })
                .Where(dp => dp.Value > 0)
                .ToList();

            return Ok(dataPoints);
        }

        /// <summary>
        /// Get volume by muscle group
        /// </summary>
        [HttpGet("muscle-group-volume")]
        public async Task<ActionResult<List<MuscleGroupVolume>>> GetMuscleGroupVolume(
            [FromQuery] int days = 30)
        {
            var userId = GetCurrentUserId();
            var startDate = DateTime.UtcNow.AddDays(-days);

            var exercises = await _context.Exercises
                .Where(e => e.Session.UserId == userId &&
                           e.Session.Status == SessionStatus.Completed &&
                           e.Session.Date >= startDate)
                .Include(e => e.ExerciseSets)
                .Include(e => e.ExerciseTemplate)
                .ToListAsync();

            var grouped = exercises
                .Where(e => e.ExerciseTemplate != null)
                .GroupBy(e => e.ExerciseTemplate!.MuscleGroup ?? "Unknown")
                .Select(g => new
                {
                    MuscleGroup = g.Key,
                    Volume = g.SelectMany(e => e.ExerciseSets)
                        .Sum(s => (s.Reps ?? 0) * (s.Weight ?? 0)),
                    ExerciseCount = g.Count()
                })
                .ToList();

            var totalVolume = grouped.Sum(g => g.Volume);

            var result = grouped
                .Select(g => new MuscleGroupVolume
                {
                    MuscleGroup = g.MuscleGroup,
                    Volume = g.Volume,
                    ExerciseCount = g.ExerciseCount,
                    Percentage = totalVolume > 0 ? (g.Volume / totalVolume) * 100 : 0
                })
                .OrderByDescending(m => m.Volume)
                .ToList();

            return Ok(result);
        }

        /// <summary>
        /// Get personal records for all exercises
        /// </summary>
        [HttpGet("personal-records")]
        public async Task<ActionResult<List<PersonalRecord>>> GetPersonalRecords()
        {
            var userId = GetCurrentUserId();

            var exercises = await _context.Exercises
                .Where(e => e.Session.UserId == userId && e.Session.Status == SessionStatus.Completed)
                .Include(e => e.ExerciseSets)
                .Include(e => e.ExerciseTemplate)
                .Include(e => e.Session)
                .ToListAsync();

            var records = exercises
                .Where(e => e.ExerciseTemplateId.HasValue && e.ExerciseSets.Any())
                .GroupBy(e => new
                {
                    e.ExerciseTemplateId,
                    ExerciseName = e.ExerciseTemplate?.Name ?? e.Name
                })
                .Select(g =>
                {
                    // Find the set with highest weight
                    var allSets = g.SelectMany(e => e.ExerciseSets
                        .Select(s => new
                        {
                            Set = s,
                            Exercise = e,
                            Date = e.Session.Date
                        }))
                        .Where(x => x.Set.Weight.HasValue && x.Set.Weight > 0)
                        .ToList();

                    if (!allSets.Any()) return null;

                    var maxWeightEntry = allSets.OrderByDescending(x => x.Set.Weight).First();

                    // Calculate 1RM using Brzycki formula: weight / (1.0278 - 0.0278 × reps)
                    var weight = maxWeightEntry.Set.Weight ?? 0;
                    var reps = maxWeightEntry.Set.Reps ?? 1;
                    var oneRepMax = reps == 1
                        ? weight
                        : weight / (1.0278 - (0.0278 * reps));

                    return new PersonalRecord
                    {
                        ExerciseName = g.Key.ExerciseName,
                        ExerciseTemplateId = g.Key.ExerciseTemplateId ?? 0,
                        Weight = weight,
                        Reps = reps,
                        DateAchieved = maxWeightEntry.Date,
                        EstimatedOneRepMax = oneRepMax,
                        DaysSincePR = (DateTime.UtcNow - maxWeightEntry.Date).Days
                    };
                })
                .Where(pr => pr != null)
                .OrderByDescending(pr => pr!.Weight)
                .ToList();

            return Ok(records!);
        }

        /// <summary>
        /// Get workout volume over time (for charts)
        /// </summary>
        [HttpGet("volume-over-time")]
        public async Task<ActionResult<List<ProgressDataPoint>>> GetVolumeOverTime(
            [FromQuery] int days = 90)
        {
            var userId = GetCurrentUserId();
            var startDate = DateTime.UtcNow.AddDays(-days);

            var sessions = await _context.Sessions
                .Where(s => s.UserId == userId &&
                           s.Status == SessionStatus.Completed &&
                           s.Date >= startDate)
                .Include(s => s.Exercises)
                .ThenInclude(e => e.ExerciseSets)
                .OrderBy(s => s.Date)
                .ToListAsync();

            var dataPoints = sessions
                .Select(s =>
                {
                    var volume = s.Exercises
                        .SelectMany(e => e.ExerciseSets)
                        .Sum(set => (set.Reps ?? 0) * (set.Weight ?? 0));

                    return new ProgressDataPoint
                    {
                        Date = s.Date,
                        Value = volume,
                        Label = $"{volume:F0} kg"
                    };
                })
                .ToList();

            return Ok(dataPoints);
        }

        /// <summary>
        /// Calculate workout streaks
        /// </summary>
        private (int currentStreak, int longestStreak) CalculateStreaks(List<Session> sessions)
        {
            if (!sessions.Any()) return (0, 0);

            var dates = sessions.Select(s => s.Date.Date).Distinct().OrderBy(d => d).ToList();

            int currentStreak = 0;
            int longestStreak = 0;
            int tempStreak = 1;

            // Calculate current streak
            var yesterday = DateTime.UtcNow.Date.AddDays(-1);
            var today = DateTime.UtcNow.Date;

            if (dates.Contains(today) || dates.Contains(yesterday))
            {
                currentStreak = 1;
                var checkDate = dates.Contains(today) ? today : yesterday;

                for (int i = dates.Count - 2; i >= 0; i--)
                {
                    var expectedDate = checkDate.AddDays(-1);
                    if (dates[i] == expectedDate)
                    {
                        currentStreak++;
                        checkDate = expectedDate;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            // Calculate longest streak
            for (int i = 1; i < dates.Count; i++)
            {
                if (dates[i] == dates[i - 1].AddDays(1))
                {
                    tempStreak++;
                }
                else
                {
                    longestStreak = Math.Max(longestStreak, tempStreak);
                    tempStreak = 1;
                }
            }

            longestStreak = Math.Max(longestStreak, tempStreak);

            return (currentStreak, longestStreak);
        }
    }
}
