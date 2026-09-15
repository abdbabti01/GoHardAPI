using System;
using System.Linq;
using System.Threading.Tasks;
using GoHardAPI.Controllers;
using GoHardAPI.Data;
using GoHardAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// The app's Today date contract for meal logs: <see cref="MealLogsController.GetTodaysMealLog"/>
    /// treats an explicitly supplied <c>date</c> as the caller's LOCAL calendar date - a
    /// label, stamped as UTC-midnight for storage, exactly like every other date-only
    /// field in this API - and never substitutes the server's own UTC date when one is
    /// supplied. Falling back to the server's UTC date is preserved for backward
    /// compatibility only when no date is supplied at all.
    /// </summary>
    public class MealLogsDateContractTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly TrainingContext _context;
        private const int UserId = 1;

        public MealLogsDateContractTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            _context = new TrainingContext(
                new DbContextOptionsBuilder<TrainingContext>().UseSqlite(_connection).Options);
            _context.Database.EnsureCreated();
            _context.Users.Add(new User { Id = UserId, Name = "u", Username = "u", Email = "u@x.com", PasswordHash = "h" });
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        private static MealLogsController Controller(TrainingContext ctx, int userId) =>
            new(ctx)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(
                            new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth")),
                    },
                },
            };

        [Fact]
        public async Task NoDateSupplied_FallsBackToServerUtcToday_BackwardCompatible()
        {
            var before = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

            var result = await Controller(_context, UserId).GetTodaysMealLog();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var mealLog = Assert.IsType<MealLog>(ok.Value);
            Assert.Equal(before, mealLog.Date);
        }

        [Fact]
        public async Task ExplicitDate_AheadOfUtcToday_ResolvesAndCreatesForThatCalendarDate_NotServerUtcToday()
        {
            // The exact scenario a user ahead of UTC (e.g. UTC+13, just after their local
            // midnight while UTC is still "yesterday") produces: their LOCAL calendar date
            // is one day ahead of the server's UTC date.
            var localDate = DateTime.UtcNow.Date.AddDays(1);
            var utcToday = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

            var result = await Controller(_context, UserId).GetTodaysMealLog(localDate);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var mealLog = Assert.IsType<MealLog>(ok.Value);
            Assert.Equal(DateTime.SpecifyKind(localDate, DateTimeKind.Utc), mealLog.Date);
            Assert.NotEqual(utcToday, mealLog.Date);

            // Persisted with that exact date - not silently reassigned to the server's
            // own UTC day.
            var stored = await _context.MealLogs.AsNoTracking().SingleAsync(ml => ml.UserId == UserId);
            Assert.Equal(DateTime.SpecifyKind(localDate, DateTimeKind.Utc), stored.Date);
        }

        [Fact]
        public async Task ExplicitDate_BehindUtcToday_ResolvesAndCreatesForThatCalendarDate()
        {
            // The mirror case: a user behind UTC (e.g. UTC-8, late evening while UTC has
            // already rolled to "tomorrow") - their LOCAL calendar date is one day behind
            // the server's UTC date.
            var localDate = DateTime.UtcNow.Date.AddDays(-1);

            var result = await Controller(_context, UserId).GetTodaysMealLog(localDate);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var mealLog = Assert.IsType<MealLog>(ok.Value);
            Assert.Equal(DateTime.SpecifyKind(localDate, DateTimeKind.Utc), mealLog.Date);
        }

        [Fact]
        public async Task RepeatedCallsWithTheSameExplicitDate_ResolveTheSameRow_NoDuplicateCreated()
        {
            var localDate = DateTime.UtcNow.Date.AddDays(1);

            var first = await Controller(_context, UserId).GetTodaysMealLog(localDate);
            var firstLog = Assert.IsType<MealLog>(Assert.IsType<OkObjectResult>(first.Result).Value);

            var second = await Controller(_context, UserId).GetTodaysMealLog(localDate);
            var secondLog = Assert.IsType<MealLog>(Assert.IsType<OkObjectResult>(second.Result).Value);

            Assert.Equal(firstLog.Id, secondLog.Id);
            Assert.Single(await _context.MealLogs.Where(ml => ml.UserId == UserId).ToListAsync());
        }

        [Fact]
        public async Task DifferentExplicitDates_ForTheSameUser_ResolveDistinctRows()
        {
            var day1 = DateTime.UtcNow.Date;
            var day2 = DateTime.UtcNow.Date.AddDays(1);

            var result1 = await Controller(_context, UserId).GetTodaysMealLog(day1);
            var log1 = Assert.IsType<MealLog>(Assert.IsType<OkObjectResult>(result1.Result).Value);

            var result2 = await Controller(_context, UserId).GetTodaysMealLog(day2);
            var log2 = Assert.IsType<MealLog>(Assert.IsType<OkObjectResult>(result2.Result).Value);

            Assert.NotEqual(log1.Id, log2.Id);
            Assert.Equal(2, await _context.MealLogs.CountAsync(ml => ml.UserId == UserId));
        }
    }
}
