using Asp.Versioning;
using GoHardAPI.Data;
using GoHardAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GoHardAPI.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    public class NutritionAnalyticsController : ControllerBase
    {
        private readonly TrainingContext _context;

        public NutritionAnalyticsController(TrainingContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        /// <summary>
        /// Get daily nutrition summary for a date range
        /// </summary>
        [HttpGet("summary/daily")]
        public async Task<ActionResult<IEnumerable<DailySummary>>> GetDailySummary(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var end = endDate?.Date ?? DateTime.UtcNow.Date;
            var start = startDate?.Date ?? end.AddDays(-7);

            var startUtc = DateTime.SpecifyKind(start, DateTimeKind.Utc);
            var endUtc = DateTime.SpecifyKind(end.AddDays(1), DateTimeKind.Utc);

            var mealLogs = await _context.MealLogs
                .Where(ml => ml.UserId == userId && ml.Date >= startUtc && ml.Date < endUtc)
                .OrderBy(ml => ml.Date)
                .Select(ml => new DailySummary
                {
                    Date = ml.Date,
                    Calories = ml.TotalCalories,
                    Protein = ml.TotalProtein,
                    Carbohydrates = ml.TotalCarbohydrates,
                    Fat = ml.TotalFat,
                    Fiber = ml.TotalFiber,
                    Water = ml.WaterIntake
                })
                .ToListAsync();

            return Ok(mealLogs);
        }

        /// <summary>
        /// Get weekly averages
        /// </summary>
        [HttpGet("summary/weekly")]
        public async Task<ActionResult<WeeklySummary>> GetWeeklySummary([FromQuery] int weeks = 4)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var endDate = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(1), DateTimeKind.Utc);
            var startDate = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(-weeks * 7), DateTimeKind.Utc);

            var mealLogs = await _context.MealLogs
                .Where(ml => ml.UserId == userId && ml.Date >= startDate && ml.Date < endDate)
                .ToListAsync();

            var weeklyData = new List<WeekData>();
            for (int i = 0; i < weeks; i++)
            {
                var weekStart = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(-((i + 1) * 7)), DateTimeKind.Utc);
                var weekEnd = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(-i * 7), DateTimeKind.Utc);

                var weekLogs = mealLogs.Where(ml => ml.Date >= weekStart && ml.Date < weekEnd).ToList();

                if (weekLogs.Any())
                {
                    weeklyData.Add(new WeekData
                    {
                        WeekStart = weekStart,
                        WeekEnd = weekEnd,
                        DaysLogged = weekLogs.Count,
                        AvgCalories = weekLogs.Average(ml => ml.TotalCalories),
                        AvgProtein = weekLogs.Average(ml => ml.TotalProtein),
                        AvgCarbohydrates = weekLogs.Average(ml => ml.TotalCarbohydrates),
                        AvgFat = weekLogs.Average(ml => ml.TotalFat)
                    });
                }
            }

            var summary = new WeeklySummary
            {
                TotalWeeks = weeks,
                WeeklyData = weeklyData,
                OverallAverage = mealLogs.Any() ? new NutritionTotals
                {
                    Calories = mealLogs.Average(ml => ml.TotalCalories),
                    Protein = mealLogs.Average(ml => ml.TotalProtein),
                    Carbohydrates = mealLogs.Average(ml => ml.TotalCarbohydrates),
                    Fat = mealLogs.Average(ml => ml.TotalFat)
                } : null
            };

            return Ok(summary);
        }

        /// <summary>
        /// Get macro breakdown for a date range
        /// </summary>
        [HttpGet("macros/breakdown")]
        public async Task<ActionResult<MacroBreakdown>> GetMacroBreakdown(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var end = endDate?.Date ?? DateTime.UtcNow.Date;
            var start = startDate?.Date ?? end.AddDays(-7);

            var startUtc = DateTime.SpecifyKind(start, DateTimeKind.Utc);
            var endUtc = DateTime.SpecifyKind(end.AddDays(1), DateTimeKind.Utc);

            var mealLogs = await _context.MealLogs
                .Where(ml => ml.UserId == userId && ml.Date >= startUtc && ml.Date < endUtc)
                .ToListAsync();

            if (!mealLogs.Any())
            {
                return Ok(new MacroBreakdown());
            }

            var totalProtein = mealLogs.Sum(ml => ml.TotalProtein);
            var totalCarbs = mealLogs.Sum(ml => ml.TotalCarbohydrates);
            var totalFat = mealLogs.Sum(ml => ml.TotalFat);
            var totalCalories = mealLogs.Sum(ml => ml.TotalCalories);

            // Calculate calories from each macro (protein and carbs = 4 cal/g, fat = 9 cal/g)
            var proteinCalories = totalProtein * 4;
            var carbCalories = totalCarbs * 4;
            var fatCalories = totalFat * 9;
            var totalMacroCalories = proteinCalories + carbCalories + fatCalories;

            var breakdown = new MacroBreakdown
            {
                TotalCalories = totalCalories,
                TotalProtein = totalProtein,
                TotalCarbohydrates = totalCarbs,
                TotalFat = totalFat,
                ProteinPercentage = totalMacroCalories > 0 ? (double)(proteinCalories / totalMacroCalories * 100) : 0,
                CarbohydratesPercentage = totalMacroCalories > 0 ? (double)(carbCalories / totalMacroCalories * 100) : 0,
                FatPercentage = totalMacroCalories > 0 ? (double)(fatCalories / totalMacroCalories * 100) : 0,
                AverageDailyCalories = mealLogs.Average(ml => ml.TotalCalories),
                AverageDailyProtein = mealLogs.Average(ml => ml.TotalProtein),
                AverageDailyCarbohydrates = mealLogs.Average(ml => ml.TotalCarbohydrates),
                AverageDailyFat = mealLogs.Average(ml => ml.TotalFat)
            };

            return Ok(breakdown);
        }

        /// <summary>
        /// Get calorie trend over time
        /// </summary>
        [HttpGet("calories/trend")]
        public async Task<ActionResult<IEnumerable<CalorieTrendPoint>>> GetCalorieTrend(
            [FromQuery] int days = 30)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var endDate = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(1), DateTimeKind.Utc);
            var startDate = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(-days), DateTimeKind.Utc);

            var mealLogs = await _context.MealLogs
                .Where(ml => ml.UserId == userId && ml.Date >= startDate && ml.Date < endDate)
                .OrderBy(ml => ml.Date)
                .Select(ml => new CalorieTrendPoint
                {
                    Date = ml.Date,
                    Calories = ml.TotalCalories
                })
                .ToListAsync();

            // Get active goal for target line
            var goal = await _context.NutritionGoals
                .FirstOrDefaultAsync(ng => ng.UserId == userId && ng.IsActive);

            if (goal != null)
            {
                foreach (var point in mealLogs)
                {
                    point.Target = goal.DailyCalories;
                }
            }

            return Ok(mealLogs);
        }

        /// <summary>
        /// Get logging streak
        /// </summary>
        [HttpGet("streak")]
        public async Task<ActionResult<StreakInfo>> GetStreak()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

            var mealLogs = await _context.MealLogs
                .Where(ml => ml.UserId == userId && ml.TotalCalories > 0)
                .OrderByDescending(ml => ml.Date)
                .Select(ml => ml.Date)
                .ToListAsync();

            int currentStreak = 0;
            int longestStreak = 0;
            int tempStreak = 0;
            DateTime? lastDate = null;

            foreach (var date in mealLogs)
            {
                if (lastDate == null)
                {
                    // First log
                    tempStreak = 1;
                    if (date == today || date == today.AddDays(-1))
                    {
                        currentStreak = 1;
                    }
                }
                else
                {
                    var diff = (lastDate.Value - date).Days;
                    if (diff == 1)
                    {
                        tempStreak++;
                        if (currentStreak > 0)
                        {
                            currentStreak++;
                        }
                    }
                    else
                    {
                        longestStreak = Math.Max(longestStreak, tempStreak);
                        tempStreak = 1;
                        if (currentStreak > 0)
                        {
                            // Break in current streak
                            currentStreak = 0;
                        }
                    }
                }
                lastDate = date;
            }

            longestStreak = Math.Max(longestStreak, tempStreak);

            return Ok(new StreakInfo
            {
                CurrentStreak = currentStreak,
                LongestStreak = longestStreak,
                TotalDaysLogged = mealLogs.Count
            });
        }

        /// <summary>
        /// Get most frequently logged foods
        /// </summary>
        [HttpGet("frequent-foods")]
        public async Task<ActionResult<IEnumerable<FrequentFood>>> GetFrequentFoods(
            [FromQuery] int limit = 10,
            [FromQuery] int days = 30)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var startDate = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(-days), DateTimeKind.Utc);

            var frequentFoods = await _context.FoodItems
                .Include(fi => fi.MealEntry)
                    .ThenInclude(me => me!.MealLog)
                .Where(fi => fi.MealEntry!.MealLog!.UserId == userId &&
                            fi.MealEntry.MealLog.Date >= startDate)
                .GroupBy(fi => new { fi.Name, fi.FoodTemplateId })
                .Select(g => new FrequentFood
                {
                    Name = g.Key.Name,
                    FoodTemplateId = g.Key.FoodTemplateId,
                    Count = g.Count(),
                    TotalCalories = g.Sum(fi => fi.Calories),
                    AverageCalories = g.Average(fi => fi.Calories)
                })
                .OrderByDescending(f => f.Count)
                .Take(limit)
                .ToListAsync();

            return Ok(frequentFoods);
        }
    }

    public class DailySummary
    {
        public DateTime Date { get; set; }
        public decimal Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbohydrates { get; set; }
        public decimal Fat { get; set; }
        public decimal? Fiber { get; set; }
        public decimal? Water { get; set; }
    }

    public class WeeklySummary
    {
        public int TotalWeeks { get; set; }
        public List<WeekData> WeeklyData { get; set; } = new();
        public NutritionTotals? OverallAverage { get; set; }
    }

    public class WeekData
    {
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }
        public int DaysLogged { get; set; }
        public decimal AvgCalories { get; set; }
        public decimal AvgProtein { get; set; }
        public decimal AvgCarbohydrates { get; set; }
        public decimal AvgFat { get; set; }
    }

    public class MacroBreakdown
    {
        public decimal TotalCalories { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TotalCarbohydrates { get; set; }
        public decimal TotalFat { get; set; }
        public double ProteinPercentage { get; set; }
        public double CarbohydratesPercentage { get; set; }
        public double FatPercentage { get; set; }
        public decimal AverageDailyCalories { get; set; }
        public decimal AverageDailyProtein { get; set; }
        public decimal AverageDailyCarbohydrates { get; set; }
        public decimal AverageDailyFat { get; set; }
    }

    public class CalorieTrendPoint
    {
        public DateTime Date { get; set; }
        public decimal Calories { get; set; }
        public decimal? Target { get; set; }
    }

    public class StreakInfo
    {
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public int TotalDaysLogged { get; set; }
    }

    public class FrequentFood
    {
        public string Name { get; set; } = string.Empty;
        public int? FoodTemplateId { get; set; }
        public int Count { get; set; }
        public decimal TotalCalories { get; set; }
        public decimal AverageCalories { get; set; }
    }
}
