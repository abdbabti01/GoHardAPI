using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GoHardAPI.Data;
using GoHardAPI.Models;
using GoHardAPI.Services;
using System.Security.Claims;

namespace GoHardAPI.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    public class BodyMetricsController : ControllerBase
    {
        private readonly TrainingContext _context;

        public BodyMetricsController(TrainingContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        /// <summary>
        /// Get body metrics for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BodyMetric>>> GetBodyMetrics([FromQuery] int days = 90)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var startDate = DateTime.UtcNow.AddDays(-days);

            var metrics = await _context.BodyMetrics
                .Where(bm => bm.UserId == userId && bm.RecordedAt >= startDate)
                .OrderByDescending(bm => bm.RecordedAt)
                .ToListAsync();

            return Ok(metrics);
        }

        /// <summary>
        /// Get the latest body metric entry
        /// </summary>
        [HttpGet("latest")]
        public async Task<ActionResult<BodyMetric>> GetLatestMetric()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var latest = await _context.BodyMetrics
                .Where(bm => bm.UserId == userId)
                .OrderByDescending(bm => bm.RecordedAt)
                .FirstOrDefaultAsync();

            if (latest == null)
            {
                return NotFound();
            }

            return Ok(latest);
        }

        /// <summary>
        /// Get a specific body metric by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<BodyMetric>> GetBodyMetric(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var metric = await _context.BodyMetrics
                .FirstOrDefaultAsync(bm => bm.Id == id && bm.UserId == userId);

            if (metric == null)
            {
                return NotFound();
            }

            return Ok(metric);
        }

        /// <summary>
        /// Add a new body metric entry
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<BodyMetric>> CreateBodyMetric(BodyMetric metric)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            metric.UserId = userId;
            metric.RecordedAt = metric.RecordedAt == default ? DateTime.UtcNow : metric.RecordedAt;
            metric.CreatedAt = DateTime.UtcNow;

            _context.BodyMetrics.Add(metric);

            // This row now covers any measurement field it fills. Retire the
            // matching legacy profile scalar (User.X -> null) in the SAME
            // transaction, so User.X stays non-null iff no row has ever filled X
            // (see CurrentMeasurementsService). Current values themselves are
            // derived on read - there is no summary column to keep in step.
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is not null)
            {
                CurrentMeasurementsService.RetireCoveredLegacyScalars(user, metric);
            }

            // AUTO-UPDATE BODY-RELATED GOALS
            await UpdateBodyMetricGoals(userId, metric);

            // One SaveChanges: the row, the legacy-scalar retirement and the goal
            // updates commit together or not at all.
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBodyMetric), new { id = metric.Id }, metric);
        }

        /// <summary>
        /// Update an existing body metric entry
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBodyMetric(int id, BodyMetric metric)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            if (id != metric.Id)
            {
                return BadRequest();
            }

            var existingMetric = await _context.BodyMetrics.FindAsync(id);
            if (existingMetric == null || existingMetric.UserId != userId)
            {
                return NotFound();
            }

            // Whether the row covered a field BEFORE this edit - so an edit that
            // CLEARS the only usable weight/height still retires a still-present
            // pre-change legacy scalar (a delete of the same row would).
            var preImageCovered = new BodyMetric
            {
                Weight = existingMetric.Weight,
                Height = existingMetric.Height,
                BodyFatPercentage = existingMetric.BodyFatPercentage,
            };

            // Update fields
            existingMetric.RecordedAt = metric.RecordedAt;
            existingMetric.Weight = metric.Weight;
            // Height was previously omitted here, which left an edit of a row
            // unable to change the current profile height. It is a measurement
            // field like the rest and is now saved.
            existingMetric.Height = metric.Height;
            existingMetric.BodyFatPercentage = metric.BodyFatPercentage;
            existingMetric.ChestCircumference = metric.ChestCircumference;
            existingMetric.WaistCircumference = metric.WaistCircumference;
            existingMetric.HipCircumference = metric.HipCircumference;
            existingMetric.ArmCircumference = metric.ArmCircumference;
            existingMetric.ThighCircumference = metric.ThighCircumference;
            existingMetric.CalfCircumference = metric.CalfCircumference;
            existingMetric.Notes = metric.Notes;
            existingMetric.PhotoUrl = metric.PhotoUrl;

            // A field this row covers with a usable value EITHER before or after
            // the edit is history-covered - retire that legacy scalar in the same
            // transaction. Retirement is monotonic, so covering it either way is
            // safe and clearing a field never un-retires.
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is not null)
            {
                CurrentMeasurementsService.RetireCoveredLegacyScalars(user, existingMetric);
                CurrentMeasurementsService.RetireCoveredLegacyScalars(user, preImageCovered);
            }

            try
            {
                // One SaveChanges: the edited row and the legacy-scalar
                // retirement commit together or not at all.
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BodyMetricExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        /// <summary>
        /// Delete a body metric entry
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBodyMetric(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var metric = await _context.BodyMetrics.FindAsync(id);
            if (metric == null || metric.UserId != userId)
            {
                return NotFound();
            }

            // The removed row's coverage of a field makes any still-present
            // legacy User.X for that field unprovable (an old "Updated from
            // profile" edit set both User.X and a matching row). Retire it in
            // the same transaction - unconditionally, exactly like create/update:
            // if surviving rows still cover the field the derived read uses them
            // anyway, and if none do NULL is the intended answer. Monotonic +
            // idempotent, so overlapping deletes converge with no lock and no
            // read-then-write race. A field the removed row never covered is
            // untouched, so a genuine legacy value with no history survives.
            if (CurrentMeasurementsService.IsUsable(metric.Weight)
                || CurrentMeasurementsService.IsUsable(metric.Height)
                || CurrentMeasurementsService.IsUsable(metric.BodyFatPercentage))
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user is not null)
                {
                    CurrentMeasurementsService.RetireCoveredLegacyScalars(user, metric);
                }
            }

            _context.BodyMetrics.Remove(metric);

            // One SaveChanges: the removal and any legacy-scalar retirement
            // commit together or not at all.
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Get chart data for a specific metric
        /// </summary>
        [HttpGet("chart")]
        public async Task<ActionResult<IEnumerable<object>>> GetChartData([FromQuery] string metric = "weight", [FromQuery] int days = 90)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var startDate = DateTime.UtcNow.AddDays(-days);

            var metrics = await _context.BodyMetrics
                .Where(bm => bm.UserId == userId && bm.RecordedAt >= startDate)
                .OrderBy(bm => bm.RecordedAt)
                .ToListAsync();

            var chartData = metrics.Select(m => new
            {
                Date = m.RecordedAt,
                Value = metric.ToLower() switch
                {
                    "weight" => m.Weight,
                    "bodyfat" => m.BodyFatPercentage,
                    "chest" => m.ChestCircumference,
                    "waist" => m.WaistCircumference,
                    "hip" => m.HipCircumference,
                    "arm" => m.ArmCircumference,
                    "thigh" => m.ThighCircumference,
                    "calf" => m.CalfCircumference,
                    _ => m.Weight
                }
            }).Where(x => x.Value != null);

            return Ok(chartData);
        }

        private bool BodyMetricExists(int id)
        {
            return _context.BodyMetrics.Any(e => e.Id == id);
        }

        private async Task UpdateBodyMetricGoals(int userId, BodyMetric metric)
        {
            // Get all active body-related goals
            var bodyGoals = await _context.Goals
                .Where(g => g.UserId == userId &&
                            g.IsActive &&
                            !g.IsCompleted)
                .ToListAsync();

            foreach (var goal in bodyGoals)
            {
                decimal? newValue = null;
                string goalTypeLower = goal.GoalType.ToLower().Trim();

                // Match metric type to goal type using explicit matching
                // GoalType values: Weight, BodyFat, Chest, Waist, Hip, Arm, Thigh, Calf
                newValue = goalTypeLower switch
                {
                    "weight" when metric.Weight.HasValue => metric.Weight.Value,
                    "bodyfat" when metric.BodyFatPercentage.HasValue => metric.BodyFatPercentage.Value,
                    "body fat" when metric.BodyFatPercentage.HasValue => metric.BodyFatPercentage.Value,
                    "chest" when metric.ChestCircumference.HasValue => metric.ChestCircumference.Value,
                    "waist" when metric.WaistCircumference.HasValue => metric.WaistCircumference.Value,
                    "hip" when metric.HipCircumference.HasValue => metric.HipCircumference.Value,
                    "arm" when metric.ArmCircumference.HasValue => metric.ArmCircumference.Value,
                    "thigh" when metric.ThighCircumference.HasValue => metric.ThighCircumference.Value,
                    "calf" when metric.CalfCircumference.HasValue => metric.CalfCircumference.Value,
                    _ => null
                };

                if (newValue.HasValue)
                {
                    // Add progress entry
                    var progress = new GoalProgress
                    {
                        GoalId = goal.Id,
                        RecordedAt = DateTime.UtcNow,
                        Value = newValue.Value,
                        Notes = "Auto-tracked from body metric log"
                    };

                    _context.GoalProgressHistory.Add(progress);

                    // Update goal's current value
                    goal.CurrentValue = newValue.Value;

                    // Check if goal is achieved using the model's IsDecreaseGoal property
                    bool goalAchieved;

                    if (goal.IsDecreaseGoal)
                    {
                        // For decrease goals (e.g., lose weight), check if current <= target
                        goalAchieved = goal.CurrentValue <= goal.TargetValue;
                    }
                    else
                    {
                        // For increase goals (e.g., gain muscle), check if current >= target
                        goalAchieved = goal.CurrentValue >= goal.TargetValue;
                    }

                    if (goalAchieved)
                    {
                        goal.IsCompleted = true;
                        goal.CompletedAt = DateTime.UtcNow;
                        goal.IsActive = false;
                    }
                }
            }
        }
    }
}
