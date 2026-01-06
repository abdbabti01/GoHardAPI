using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GoHardAPI.Data;
using GoHardAPI.Models;
using System.Security.Claims;

namespace GoHardAPI.Controllers
{
    [Route("api/[controller]")]
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

            // AUTO-UPDATE BODY-RELATED GOALS
            await UpdateBodyMetricGoals(userId, metric);

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

            // Update fields
            existingMetric.RecordedAt = metric.RecordedAt;
            existingMetric.Weight = metric.Weight;
            existingMetric.BodyFatPercentage = metric.BodyFatPercentage;
            existingMetric.ChestCircumference = metric.ChestCircumference;
            existingMetric.WaistCircumference = metric.WaistCircumference;
            existingMetric.HipCircumference = metric.HipCircumference;
            existingMetric.ArmCircumference = metric.ArmCircumference;
            existingMetric.ThighCircumference = metric.ThighCircumference;
            existingMetric.CalfCircumference = metric.CalfCircumference;
            existingMetric.Notes = metric.Notes;
            existingMetric.PhotoUrl = metric.PhotoUrl;

            try
            {
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

            _context.BodyMetrics.Remove(metric);
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
                string goalTypeLower = goal.GoalType.ToLower();

                // Match metric type to goal type
                if (goalTypeLower.Contains("weight") && metric.Weight.HasValue)
                {
                    newValue = metric.Weight.Value;
                }
                else if (goalTypeLower.Contains("bodyfat") || goalTypeLower.Contains("body fat"))
                {
                    if (metric.BodyFatPercentage.HasValue)
                        newValue = metric.BodyFatPercentage.Value;
                }
                else if (goalTypeLower.Contains("chest") && metric.ChestCircumference.HasValue)
                {
                    newValue = metric.ChestCircumference.Value;
                }
                else if (goalTypeLower.Contains("waist") && metric.WaistCircumference.HasValue)
                {
                    newValue = metric.WaistCircumference.Value;
                }
                else if (goalTypeLower.Contains("hip") && metric.HipCircumference.HasValue)
                {
                    newValue = metric.HipCircumference.Value;
                }
                else if (goalTypeLower.Contains("arm") && metric.ArmCircumference.HasValue)
                {
                    newValue = metric.ArmCircumference.Value;
                }

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

                    // Check if goal is achieved
                    bool goalAchieved = false;

                    if (goalTypeLower.Contains("lose") || goalTypeLower.Contains("decrease"))
                    {
                        // For decrease goals (e.g., lose weight), check if current <= target
                        goalAchieved = goal.CurrentValue <= goal.TargetValue;
                    }
                    else if (goalTypeLower.Contains("gain") || goalTypeLower.Contains("increase"))
                    {
                        // For increase goals (e.g., gain muscle), check if current >= target
                        goalAchieved = goal.CurrentValue >= goal.TargetValue;
                    }
                    else
                    {
                        // For absolute goals (e.g., "reach 75kg"), check if close enough (within 0.5)
                        goalAchieved = Math.Abs(goal.CurrentValue - goal.TargetValue) <= 0.5m;
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
