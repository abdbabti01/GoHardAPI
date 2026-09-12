using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using GoHardAPI.Controllers;
using GoHardAPI.Data;
using GoHardAPI.Models;
using GoHardAPI.Services;
using GoHardAPI.Services.AI;
using GoHardAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Real-PostgreSQL evidence that applying an AI-generated meal-plan suggestion
    /// never destroys actual logged intake: an already-consumed meal entry is never
    /// cleared or reset, unrelated planned food is left alone by default, reapplying
    /// the same suggestion is idempotent (no duplicate accumulation), a multi-day
    /// batch apply is all-or-nothing, and the RepeatableRead isolation the endpoints
    /// use is verified to actually detect a concurrent write conflict on this schema
    /// rather than silently losing it.
    /// </summary>
    [Trait("Category", "PostgresIntegration")]
    [Collection(HistoryPreservationPostgresCollection.Name)]
    public sealed class MealPlanApplyPostgresTests
    {
        private readonly HistoryPreservationPostgresFixture _pg;

        public MealPlanApplyPostgresTests(HistoryPreservationPostgresFixture pg) => _pg = pg;

        private static ChatController Chat(TrainingContext ctx, int userId)
        {
            var providerFactory = new AIProviderFactory(
                new ServiceCollection().BuildServiceProvider(),
                new ConfigurationBuilder().Build(),
                NullLogger<AIProviderFactory>.Instance);
            var aiService = new AIService(
                providerFactory,
                new ConfigurationBuilder().Build(),
                NullLogger<AIService>.Instance);
            var currentMeasurements = new CurrentMeasurementsService(ctx);

            var controller = new ChatController(ctx, aiService, currentMeasurements, NullLogger<ChatController>.Instance);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth"))
                }
            };
            return controller;
        }

        private static FoodItemsController Food(TrainingContext ctx, int userId)
        {
            var controller = new FoodItemsController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth"))
                }
            };
            return controller;
        }

        private static MealEntriesController MealEntries(TrainingContext ctx, int userId)
        {
            var controller = new MealEntriesController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth"))
                }
            };
            return controller;
        }

        private async Task<int> SeedUser()
        {
            await using var ctx = _pg.NewContext();
            var u = new User
            {
                Name = "U",
                Username = $"u{Guid.NewGuid():N}"[..20],
                Email = $"u{Guid.NewGuid():N}@x.com",
                PasswordHash = "h",
                DateCreated = DateTime.UtcNow,
                UnitPreference = "Metric",
            };
            ctx.Users.Add(u);
            await ctx.SaveChangesAsync();
            return u.Id;
        }

        private static ChatMealPlanWeekExtraction OneDayPlan(int day, params (string mealType, string food, decimal calories)[] meals)
        {
            return new ChatMealPlanWeekExtraction
            {
                TargetCalories = 2000,
                Days = new List<ChatMealPlanDayData>
                {
                    new ChatMealPlanDayData
                    {
                        Day = day,
                        TotalCalories = meals.Sum(m => m.calories),
                        Meals = meals.Select(m => new ChatMealPlanMealData
                        {
                            MealType = m.mealType,
                            Foods = new List<ChatMealPlanFoodData>
                            {
                                new ChatMealPlanFoodData
                                {
                                    Name = m.food,
                                    ServingSize = 1,
                                    ServingUnit = "serving",
                                    Calories = m.calories,
                                    Protein = 10,
                                    Carbohydrates = 10,
                                    Fat = 5,
                                },
                            },
                        }).ToList(),
                    },
                },
            };
        }

        private async Task<int> SeedMealPlanConversation(int userId, ChatMealPlanWeekExtraction plan)
        {
            await using var ctx = _pg.NewContext();
            var conversation = new ChatConversation
            {
                UserId = userId,
                Title = "Meal Plan",
                Type = "meal_plan",
                CreatedAt = DateTime.UtcNow,
                MealPlanDataJson = JsonSerializer.Serialize(plan),
            };
            ctx.ChatConversations.Add(conversation);
            await ctx.SaveChangesAsync();
            return conversation.Id;
        }

        /// <summary>Seeds today's MealLog with a Breakfast entry the user already ate.</summary>
        private async Task<(int mealLogId, int breakfastEntryId)> SeedConsumedBreakfast(int userId, DateTime date, string food = "Oatmeal", decimal calories = 300)
        {
            await using var ctx = _pg.NewContext();
            var mealLog = new MealLog { UserId = userId, Date = date.Date, CreatedAt = DateTime.UtcNow };
            ctx.MealLogs.Add(mealLog);
            await ctx.SaveChangesAsync();

            var entry = new MealEntry
            {
                MealLogId = mealLog.Id,
                MealType = "Breakfast",
                IsConsumed = true,
                ConsumedAt = DateTime.UtcNow,
                TotalCalories = calories,
                CreatedAt = DateTime.UtcNow,
            };
            ctx.MealEntries.Add(entry);
            await ctx.SaveChangesAsync();

            ctx.FoodItems.Add(new FoodItem
            {
                MealEntryId = entry.Id,
                Name = food,
                Calories = calories,
                Protein = 10,
                Carbohydrates = 30,
                Fat = 5,
                CreatedAt = DateTime.UtcNow,
            });
            await ctx.SaveChangesAsync();

            return (mealLog.Id, entry.Id);
        }

        [DockerRequiredFact]
        public async Task apply_never_touches_an_already_consumed_meal_entry()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser();
            var today = DateTime.UtcNow.Date;
            var (mealLogId, breakfastId) = await SeedConsumedBreakfast(userId, today);
            var convoId = await SeedMealPlanConversation(userId,
                OneDayPlan(1, ("Breakfast", "AI Pancakes", 500), ("Lunch", "AI Salad", 400)));

            ApplyMealPlanResponse response;
            await using (var ctx = _pg.NewContext())
            {
                var result = await Chat(ctx, userId).ApplyMealPlanToToday(convoId, day: 1, date: null);
                var ok = Assert.IsType<OkObjectResult>(result.Result);
                response = Assert.IsType<ApplyMealPlanResponse>(ok.Value);
            }

            Assert.Contains("Breakfast", response.SkippedMealTypes);

            await using var verify = _pg.NewContext();
            var breakfast = await verify.MealEntries.Include(e => e.FoodItems).FirstAsync(e => e.Id == breakfastId);
            Assert.True(breakfast.IsConsumed);
            Assert.Equal(300, breakfast.TotalCalories);
            Assert.Single(breakfast.FoodItems);
            Assert.Equal("Oatmeal", breakfast.FoodItems.First().Name);

            var lunch = await verify.MealEntries.Include(e => e.FoodItems)
                .FirstAsync(e => e.MealLogId == mealLogId && e.MealType == "Lunch");
            Assert.False(lunch.IsConsumed);
            Assert.Single(lunch.FoodItems);
            Assert.Equal("AI Salad", lunch.FoodItems.First().Name);

            // A meal type the plan never targeted (Dinner/Snack) must not be fabricated.
            Assert.False(await verify.MealEntries.AnyAsync(e => e.MealLogId == mealLogId && e.MealType == "Dinner"));
        }

        [DockerRequiredFact]
        public async Task reapplying_the_same_suggestion_is_idempotent_and_does_not_duplicate()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser();
            var convoId = await SeedMealPlanConversation(userId, OneDayPlan(1, ("Lunch", "AI Salad", 400)));

            await using (var ctx = _pg.NewContext())
            {
                await Chat(ctx, userId).ApplyMealPlanToToday(convoId, day: 1, date: null);
            }

            ApplyMealPlanResponse second;
            await using (var ctx = _pg.NewContext())
            {
                var result = await Chat(ctx, userId).ApplyMealPlanToToday(convoId, day: 1, date: null);
                second = (ApplyMealPlanResponse)((OkObjectResult)result.Result!).Value!;
            }

            Assert.Contains("Lunch", second.ReplacedMealTypes);

            // Scope strictly to this user's meal log — the container is shared across
            // tests in this class, so an unscoped query would see other tests' rows too.
            await using var verify = _pg.NewContext();
            var lunchEntry = await verify.MealEntries
                .Include(me => me.FoodItems)
                .Include(me => me.MealLog)
                .Where(me => me.MealLog!.UserId == userId && me.MealType == "Lunch")
                .FirstAsync();
            Assert.Single(lunchEntry.FoodItems); // not duplicated by the second apply
            Assert.Equal("AI Salad", lunchEntry.FoodItems.Single().Name);
        }

        [DockerRequiredFact]
        public async Task day_apply_leaves_an_unrelated_manually_logged_planned_item_alone_by_default()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser();
            var today = DateTime.UtcNow.Date;

            int mealLogId, lunchEntryId;
            await using (var ctx = _pg.NewContext())
            {
                var mealLog = new MealLog { UserId = userId, Date = today, CreatedAt = DateTime.UtcNow };
                ctx.MealLogs.Add(mealLog);
                await ctx.SaveChangesAsync();
                var lunch = new MealEntry { MealLogId = mealLog.Id, MealType = "Lunch", IsConsumed = false, CreatedAt = DateTime.UtcNow };
                ctx.MealEntries.Add(lunch);
                await ctx.SaveChangesAsync();
                ctx.FoodItems.Add(new FoodItem { MealEntryId = lunch.Id, Name = "Manual Sandwich", Calories = 350, CreatedAt = DateTime.UtcNow });
                await ctx.SaveChangesAsync();
                mealLogId = mealLog.Id;
                lunchEntryId = lunch.Id;
            }

            var convoId = await SeedMealPlanConversation(userId, OneDayPlan(1, ("Lunch", "AI Salad", 400)));
            await using (var ctx = _pg.NewContext())
            {
                await Chat(ctx, userId).ApplyMealPlanToToday(convoId, day: 1, date: null);
            }

            await using var verify = _pg.NewContext();
            var items = await verify.FoodItems.Where(fi => fi.MealEntryId == lunchEntryId).Select(fi => fi.Name).ToListAsync();
            Assert.Contains("Manual Sandwich", items); // untouched
            Assert.Contains("AI Salad", items); // added alongside
        }

        [DockerRequiredFact]
        public async Task week_apply_with_overwrite_true_replaces_other_planned_items_but_still_skips_consumed()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser();
            var start = DateTime.UtcNow.Date;
            var (_, breakfastId) = await SeedConsumedBreakfast(userId, start);

            int lunchEntryId;
            await using (var ctx = _pg.NewContext())
            {
                var mealLog = await ctx.MealLogs.FirstAsync(ml => ml.UserId == userId && ml.Date == start);
                var lunch = new MealEntry { MealLogId = mealLog.Id, MealType = "Lunch", IsConsumed = false, CreatedAt = DateTime.UtcNow };
                ctx.MealEntries.Add(lunch);
                await ctx.SaveChangesAsync();
                ctx.FoodItems.Add(new FoodItem { MealEntryId = lunch.Id, Name = "Old Manual Lunch", Calories = 300, CreatedAt = DateTime.UtcNow });
                await ctx.SaveChangesAsync();
                lunchEntryId = lunch.Id;
            }

            var convoId = await SeedMealPlanConversation(userId, OneDayPlan(1, ("Lunch", "AI Salad", 400)));

            await using (var ctx = _pg.NewContext())
            {
                var result = await Chat(ctx, userId).ApplyMealPlanWeek(convoId, new ApplyMealPlanWeekRequest
                {
                    ApplyAllDays = false,
                    Days = new List<int> { 1 },
                    StartDate = start,
                    OverwriteExisting = true,
                });
                Assert.IsType<OkObjectResult>(result.Result);
            }

            await using var verify = _pg.NewContext();
            var lunchItems = await verify.FoodItems.Where(fi => fi.MealEntryId == lunchEntryId).Select(fi => fi.Name).ToListAsync();
            Assert.DoesNotContain("Old Manual Lunch", lunchItems); // replaced
            Assert.Contains("AI Salad", lunchItems);

            var breakfast = await verify.MealEntries.Include(e => e.FoodItems).FirstAsync(e => e.Id == breakfastId);
            Assert.True(breakfast.IsConsumed);
            Assert.Single(breakfast.FoodItems);
            Assert.Equal("Oatmeal", breakfast.FoodItems.First().Name); // never touched, even with overwrite=true
        }

        [DockerRequiredFact]
        public async Task week_apply_with_overwrite_false_only_replaces_its_own_source()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser();
            var start = DateTime.UtcNow.Date;

            int mealLogId, lunchEntryId;
            await using (var ctx = _pg.NewContext())
            {
                var mealLog = new MealLog { UserId = userId, Date = start, CreatedAt = DateTime.UtcNow };
                ctx.MealLogs.Add(mealLog);
                await ctx.SaveChangesAsync();
                var lunch = new MealEntry { MealLogId = mealLog.Id, MealType = "Lunch", IsConsumed = false, CreatedAt = DateTime.UtcNow };
                ctx.MealEntries.Add(lunch);
                await ctx.SaveChangesAsync();
                ctx.FoodItems.Add(new FoodItem { MealEntryId = lunch.Id, Name = "Manual Lunch", Calories = 300, CreatedAt = DateTime.UtcNow });
                await ctx.SaveChangesAsync();
                mealLogId = mealLog.Id;
                lunchEntryId = lunch.Id;
            }

            var convoId = await SeedMealPlanConversation(userId, OneDayPlan(1, ("Lunch", "AI Salad", 400)));

            await using (var ctx = _pg.NewContext())
            {
                await Chat(ctx, userId).ApplyMealPlanWeek(convoId, new ApplyMealPlanWeekRequest
                {
                    ApplyAllDays = false,
                    Days = new List<int> { 1 },
                    StartDate = start,
                    OverwriteExisting = false,
                });
            }

            await using var verify = _pg.NewContext();
            var lunchItems = await verify.FoodItems.Where(fi => fi.MealEntryId == lunchEntryId).Select(fi => fi.Name).ToListAsync();
            Assert.Contains("Manual Lunch", lunchItems); // preserved
            Assert.Contains("AI Salad", lunchItems); // added alongside
        }

        [DockerRequiredFact]
        public async Task explicit_date_targets_a_different_calendar_date_than_today()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser();
            var future = DateTime.UtcNow.Date.AddDays(5);
            var convoId = await SeedMealPlanConversation(userId, OneDayPlan(1, ("Breakfast", "AI Toast", 250)));

            await using (var ctx = _pg.NewContext())
            {
                var result = await Chat(ctx, userId).ApplyMealPlanToToday(convoId, day: 1, date: future);
                Assert.IsType<OkObjectResult>(result.Result);
            }

            await using var verify = _pg.NewContext();
            Assert.True(await verify.MealLogs.AnyAsync(ml => ml.UserId == userId && ml.Date == future));
            Assert.False(await verify.MealLogs.AnyAsync(ml => ml.UserId == userId && ml.Date == DateTime.UtcNow.Date));
        }

        [DockerRequiredFact]
        public async Task a_failed_multi_day_apply_leaves_nothing_partially_applied()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser();
            var start = DateTime.UtcNow.Date;

            // Day 2's meal type deliberately exceeds MealEntry.MealType's MaxLength(50),
            // so PostgreSQL rejects that INSERT partway through the batch — day 1 would
            // otherwise have succeeded on its own.
            var plan = new ChatMealPlanWeekExtraction
            {
                TargetCalories = 2000,
                Days = new List<ChatMealPlanDayData>
                {
                    new ChatMealPlanDayData
                    {
                        Day = 1,
                        Meals = new List<ChatMealPlanMealData>
                        {
                            new ChatMealPlanMealData
                            {
                                MealType = "Breakfast",
                                Foods = new List<ChatMealPlanFoodData>
                                {
                                    new ChatMealPlanFoodData { Name = "AI Toast", Calories = 250, ServingSize = 1, ServingUnit = "serving" },
                                },
                            },
                        },
                    },
                    new ChatMealPlanDayData
                    {
                        Day = 2,
                        Meals = new List<ChatMealPlanMealData>
                        {
                            new ChatMealPlanMealData
                            {
                                MealType = new string('X', 200),
                                Foods = new List<ChatMealPlanFoodData>
                                {
                                    new ChatMealPlanFoodData { Name = "Bad Entry", Calories = 100, ServingSize = 1, ServingUnit = "serving" },
                                },
                            },
                        },
                    },
                },
            };
            var convoId = await SeedMealPlanConversation(userId, plan);

            await using (var ctx = _pg.NewContext())
            {
                var result = await Chat(ctx, userId).ApplyMealPlanWeek(convoId, new ApplyMealPlanWeekRequest
                {
                    ApplyAllDays = true,
                    StartDate = start,
                    OverwriteExisting = true,
                });

                var objectResult = Assert.IsType<ObjectResult>(result.Result);
                Assert.Equal(500, objectResult.StatusCode);
            }

            await using var verify = _pg.NewContext();
            // Day 1 must NOT have been left applied even though it was valid on its own.
            Assert.False(await verify.MealLogs.AnyAsync(ml => ml.UserId == userId && ml.Date == start));
            Assert.False(await verify.MealLogs.AnyAsync(ml => ml.UserId == userId && ml.Date == start.AddDays(1)));
        }

        [DockerRequiredFact]
        public async Task concurrent_write_to_the_same_meal_entry_under_repeatable_read_is_detected_not_silently_lost()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser();
            var today = DateTime.UtcNow.Date;

            int entryId;
            await using (var seedCtx = _pg.NewContext())
            {
                var mealLog = new MealLog { UserId = userId, Date = today, CreatedAt = DateTime.UtcNow };
                seedCtx.MealLogs.Add(mealLog);
                await seedCtx.SaveChangesAsync();
                var entry = new MealEntry { MealLogId = mealLog.Id, MealType = "Lunch", IsConsumed = false, CreatedAt = DateTime.UtcNow };
                seedCtx.MealEntries.Add(entry);
                await seedCtx.SaveChangesAsync();
                entryId = entry.Id;
            }

            // TxA: opens RepeatableRead and reads the entry (establishes its snapshot),
            // exactly as the apply endpoints do before touching it.
            await using var ctxA = _pg.NewContext();
            await using var txA = await ctxA.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
            var entryInA = await ctxA.MealEntries.FirstAsync(e => e.Id == entryId);

            // TxB: a fully independent transaction (simulating MarkAsConsumed) commits a
            // change to the SAME row while TxA's snapshot is still open.
            await using (var ctxB = _pg.NewContext())
            {
                var entryInB = await ctxB.MealEntries.FirstAsync(e => e.Id == entryId);
                entryInB.IsConsumed = true;
                entryInB.ConsumedAt = DateTime.UtcNow;
                await ctxB.SaveChangesAsync();
            }

            // TxA now tries to write the same row it read earlier — this is exactly what
            // the apply endpoints do (update entry totals) after their own consumed-check.
            // EF Core wraps the underlying PostgreSQL 40001 serialization failure
            // (DbUpdateException -> PostgresException) in an InvalidOperationException
            // because the failure occurred inside a user-managed transaction it can't
            // silently retry — this is standard EF/Npgsql behavior, not a bug. The apply
            // endpoints' outer `catch (Exception ex)` catches this regardless of the
            // exact wrapped type and rolls back, so the guarantee holds either way.
            entryInA.TotalCalories = 999;
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await ctxA.SaveChangesAsync());
            var dbUpdateEx = Assert.IsType<DbUpdateException>(ex.InnerException);
            var pgEx = Assert.IsType<Npgsql.PostgresException>(dbUpdateEx.InnerException);
            Assert.Equal("40001", pgEx.SqlState); // serialization_failure

            // The concurrently-committed "marked consumed" change survives untouched.
            await using var verify = _pg.NewContext();
            var final = await verify.MealEntries.FirstAsync(e => e.Id == entryId);
            Assert.True(final.IsConsumed);
        }

        [DockerRequiredFact]
        public async Task apply_never_touches_another_users_meal_log()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var owner = await SeedUser();
            var stranger = await SeedUser();
            var convoId = await SeedMealPlanConversation(owner, OneDayPlan(1, ("Breakfast", "AI Toast", 250)));

            await using (var ctx = _pg.NewContext())
            {
                var result = await Chat(ctx, stranger).ApplyMealPlanToToday(convoId, day: 1, date: null);
                Assert.IsType<NotFoundObjectResult>(result.Result); // conversation not found for this user
            }

            await using var verify = _pg.NewContext();
            Assert.False(await verify.MealLogs.AnyAsync(ml => ml.UserId == stranger));
        }

        /// <summary>
        /// Deterministically reproduces the EXACT defect a prior recovery pass found and
        /// then papered over with a weakened assertion instead of fixing: two concurrent
        /// recomputations of the SAME meal entry's cached total - one holding a stale,
        /// pre-edit view - can otherwise let the stale one blindly overwrite the other's
        /// already-committed, correct total, with no error on either side.
        ///
        /// This uses held execution checkpoints against two separate, real PostgreSQL
        /// contexts to FORCE the exact "stale read -> competing recompute commits first ->
        /// stale write attempt" order, rather than hoping unforced concurrency happens to
        /// land that way (as the now-corrected end-to-end test below does):
        ///   1. ctxA opens a Serializable transaction and reads the Lunch entry's
        ///      FoodItems, establishing its snapshot BEFORE Grilled Chicken exists. This
        ///      is held open (not committed) past this point - exactly
        ///      <see cref="MealLogTotalsRecalculator"/>'s own read, mid-attempt.
        ///   2. A fully separate, already-committed sequence simulates a CONCURRENT
        ///      recompute for the same entry that DID see Grilled Chicken: it inserts the
        ///      item and writes the resulting (correct) total to the SAME MealEntry row -
        ///      exactly what a real, concurrent meal-plan apply's own recalculation would
        ///      have already durably committed by this point.
        ///   3. ctxA, still unaware of either change, computes a total from its stale
        ///      (Chicken-less) view and tries to persist it to that SAME row.
        /// PostgreSQL must reject ctxA's write in step 3 with the recognized 40001
        /// conflict shape - a genuine concurrent UPDATE to a row already changed since
        /// ctxA's snapshot began - not silently allow the stale overwrite. This is the
        /// same row-level mechanism already proven generically by
        /// <see cref="concurrent_write_to_the_same_meal_entry_under_repeatable_read_is_detected_not_silently_lost"/>;
        /// this test proves it specifically for two competing TOTAL RECOMPUTATIONS of the
        /// same entry, which is the exact shape <see cref="MealLogTotalsRecalculator"/>
        /// depends on to safely retry with a fresh read instead of ever completing a
        /// stale write.
        /// </summary>
        [DockerRequiredFact]
        public async Task a_stale_recompute_attempt_is_rejected_when_a_concurrent_recompute_already_committed_first()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser();
            var today = DateTime.UtcNow.Date;

            int lunchEntryId;
            await using (var seedCtx = _pg.NewContext())
            {
                var mealLog = new MealLog { UserId = userId, Date = today, CreatedAt = DateTime.UtcNow };
                seedCtx.MealLogs.Add(mealLog);
                await seedCtx.SaveChangesAsync();

                var lunch = new MealEntry { MealLogId = mealLog.Id, MealType = "Lunch", IsConsumed = false, CreatedAt = DateTime.UtcNow };
                seedCtx.MealEntries.Add(lunch);
                await seedCtx.SaveChangesAsync();

                seedCtx.FoodItems.Add(new FoodItem
                {
                    MealEntryId = lunch.Id,
                    Name = "Rice",
                    Quantity = 1,
                    Calories = 150,
                    Protein = 3,
                    Carbohydrates = 33,
                    Fat = 0,
                    CreatedAt = DateTime.UtcNow,
                });
                await seedCtx.SaveChangesAsync();

                lunchEntryId = lunch.Id;
            }

            // Checkpoint 1: ctxA opens Serializable and reads the entry - establishing its
            // snapshot - before Grilled Chicken exists. A bare (non-Include) read, exactly
            // matching concurrent_write_to_the_same_meal_entry_under_repeatable_read_is_detected_not_silently_lost's
            // proven shape.
            await using var ctxA = _pg.NewContext();
            await using var txA = await ctxA.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var entryInA = await ctxA.MealEntries.FirstAsync(e => e.Id == lunchEntryId);

            // Checkpoint 2: a fully separate, already-committed sequence simulates a
            // concurrent recompute that DID see Grilled Chicken - inserting it and then
            // writing the SAME MealEntry row's total to reflect it (700 -> here just
            // Rice+Chicken = 550, matching this fixture) - exactly what a real concurrent
            // apply's own MealLogTotalsRecalculator attempt would have already committed.
            await using (var ctxB = _pg.NewContext())
            {
                ctxB.FoodItems.Add(new FoodItem
                {
                    MealEntryId = lunchEntryId,
                    Name = "Grilled Chicken",
                    Quantity = 1,
                    Calories = 400,
                    Protein = 35,
                    Carbohydrates = 0,
                    Fat = 10,
                    CreatedAt = DateTime.UtcNow,
                });
                var entryInB = await ctxB.MealEntries.FirstAsync(e => e.Id == lunchEntryId);
                entryInB.TotalCalories = 550; // correct as of this commit: Rice(150) + Chicken(400)
                await ctxB.SaveChangesAsync();
            }

            // Checkpoint 3: ctxA, still unaware of either change, computes and tries to
            // persist a total from its stale view to the SAME row ctxB already updated -
            // exactly the shape that silently undercounted in the reproduced defect.
            entryInA.TotalCalories = 150; // stale: what a recompute would get from Rice alone
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await ctxA.SaveChangesAsync());
            var dbUpdateEx = Assert.IsType<DbUpdateException>(ex.InnerException);
            var pgEx = Assert.IsType<Npgsql.PostgresException>(dbUpdateEx.InnerException);
            Assert.Equal("40001", pgEx.SqlState); // serialization_failure

            // ctxB's correct, already-committed total survives untouched - ctxA's stale,
            // rejected write never reached the database.
            await using var verify = _pg.NewContext();
            var finalEntry = await verify.MealEntries
                .Include(e => e.FoodItems)
                .FirstAsync(e => e.Id == lunchEntryId);
            Assert.Equal(2, finalEntry.FoodItems.Count);
            Assert.Contains(finalEntry.FoodItems, f => f.Name == "Grilled Chicken");
            Assert.Equal(550m, finalEntry.TotalCalories);
        }

        /// <summary>
        /// Atomicity correction: an earlier version of this fix committed a food-item
        /// content change in one transaction and recomputed its totals in a SEPARATE,
        /// later transaction. That reintroduced the exact failure mode the whole feature
        /// exists to close: if the second step ever failed - here, forced by a real
        /// PostgreSQL conflict landing between the two steps - the food row stayed
        /// committed with a stale total forever, since nothing repairs it and no later
        /// unrelated request is relied on to. This test forces that exact ordering
        /// deterministically (held execution checkpoints, real PostgreSQL, no race) and
        /// proves the CURRENT design has no such gap: content and totals now commit as
        /// ONE transaction (<see cref="MealLogTotalsRecalculator.ExecuteAtomicallyAsync{T}"/>),
        /// so a conflict discovered ANYWHERE in the attempt - including specifically
        /// between the content write and the totals recompute - rolls back the content
        /// too. A fresh context afterward must see either the complete, correct result or
        /// nothing at all from this attempt - never a food row committed without its
        /// corresponding total.
        ///
        /// Sequence:
        ///   1. ctxA opens Serializable, reads the (empty) Lunch entry - establishing its
        ///      snapshot - then inserts "Chicken" and saves (CONTENT WRITTEN, still
        ///      uncommitted - this transaction is held open).
        ///   2. A fully separate, already-committed transaction writes a DIFFERENT total
        ///      to that SAME MealEntry row - simulating a concurrent writer's recompute
        ///      finishing first.
        ///   3. ctxA now runs the real <see cref="MealLogTotalsRecalculator.StageRecalculationAsync"/>
        ///      (production code, not a hypothetical) and tries to commit - its write
        ///      conflicts with step 2's already-committed change to the same row.
        /// </summary>
        [DockerRequiredFact]
        public async Task a_conflict_between_content_write_and_totals_recompute_rolls_back_the_content_too()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser();
            var today = DateTime.UtcNow.Date;

            int lunchEntryId;
            int mealLogId;
            await using (var seedCtx = _pg.NewContext())
            {
                var mealLog = new MealLog { UserId = userId, Date = today, CreatedAt = DateTime.UtcNow };
                seedCtx.MealLogs.Add(mealLog);
                await seedCtx.SaveChangesAsync();

                var lunch = new MealEntry { MealLogId = mealLog.Id, MealType = "Lunch", IsConsumed = false, CreatedAt = DateTime.UtcNow };
                seedCtx.MealEntries.Add(lunch);
                await seedCtx.SaveChangesAsync();

                lunchEntryId = lunch.Id;
                mealLogId = mealLog.Id;
            }

            // Checkpoint 1: content write, held open (not yet committed).
            await using var ctxA = _pg.NewContext();
            await using var txA = await ctxA.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var entryInA = await ctxA.MealEntries
                .Include(e => e.FoodItems)
                .FirstAsync(e => e.Id == lunchEntryId);
            entryInA.FoodItems.Add(new FoodItem
            {
                MealEntryId = lunchEntryId,
                Name = "Chicken",
                Quantity = 1,
                Calories = 400,
                Protein = 35,
                Carbohydrates = 0,
                Fat = 10,
                CreatedAt = DateTime.UtcNow,
            });
            await ctxA.SaveChangesAsync(); // content written within ctxA's still-open transaction

            // Checkpoint 2: a fully separate, already-committed writer changes the SAME
            // MealEntry row's total, landing exactly between ctxA's content write and its
            // (not-yet-attempted) totals recompute.
            await using (var ctxB = _pg.NewContext())
            {
                var entryInB = await ctxB.MealEntries.FirstAsync(e => e.Id == lunchEntryId);
                entryInB.TotalCalories = 999;
                await ctxB.SaveChangesAsync();
            }

            // Checkpoint 3: ctxA now runs the REAL production recompute and tries to
            // commit - it conflicts with checkpoint 2's already-committed write to the
            // same row.
            await MealLogTotalsRecalculator.StageRecalculationAsync(ctxA, mealLogId);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await ctxA.SaveChangesAsync());
            var dbUpdateEx = Assert.IsType<DbUpdateException>(ex.InnerException);
            var pgEx = Assert.IsType<Npgsql.PostgresException>(dbUpdateEx.InnerException);
            Assert.Equal("40001", pgEx.SqlState); // serialization_failure

            // The acceptance bar: a fresh context sees FULL ROLLBACK, not a food row
            // committed without its total. Chicken must NOT exist - ctxA never committed.
            await using var verify = _pg.NewContext();
            var finalEntry = await verify.MealEntries
                .Include(e => e.FoodItems)
                .FirstAsync(e => e.Id == lunchEntryId);
            Assert.Empty(finalEntry.FoodItems);
            Assert.Equal(999m, finalEntry.TotalCalories); // checkpoint 2's write stands, untouched
        }

        /// <summary>
        /// Exhausts <see cref="MealLogTotalsRecalculator.ExecuteAtomicallyAsync{T}"/>'s
        /// retry budget deterministically (a fresh, guaranteed-to-conflict competing
        /// commit is issued from INSIDE the attempt delegate itself, so every single
        /// attempt - not just the first - hits a confirmed PostgreSQL conflict) and
        /// verifies the same invariant as above: when
        /// <see cref="MealLogTotalsRecalculator.ConcurrencyRetriesExhaustedException"/> is
        /// thrown, NOTHING from any attempt was left committed - never content without
        /// its corresponding totals, and the exception itself does not conceal a
        /// content-committed/totals-stale state because no such state exists to conceal.
        /// </summary>
        [DockerRequiredFact]
        public async Task exhausting_the_retry_policy_leaves_no_content_committed_without_its_totals()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser();
            var today = DateTime.UtcNow.Date;

            int lunchEntryId;
            int mealLogId;
            await using (var seedCtx = _pg.NewContext())
            {
                var mealLog = new MealLog { UserId = userId, Date = today, CreatedAt = DateTime.UtcNow };
                seedCtx.MealLogs.Add(mealLog);
                await seedCtx.SaveChangesAsync();

                var lunch = new MealEntry { MealLogId = mealLog.Id, MealType = "Lunch", IsConsumed = false, CreatedAt = DateTime.UtcNow };
                seedCtx.MealEntries.Add(lunch);
                await seedCtx.SaveChangesAsync();

                lunchEntryId = lunch.Id;
                mealLogId = mealLog.Id;
            }

            await using var ctxA = _pg.NewContext();
            var conflictingValue = 1;

            var thrown = await Assert.ThrowsAsync<MealLogTotalsRecalculator.ConcurrencyRetriesExhaustedException>(() =>
                MealLogTotalsRecalculator.ExecuteAtomicallyAsync<bool>(ctxA, async ct =>
                {
                    var entry = await ctxA.MealEntries
                        .Include(e => e.FoodItems)
                        .FirstAsync(e => e.Id == lunchEntryId, ct);

                    entry.FoodItems.Add(new FoodItem
                    {
                        MealEntryId = lunchEntryId,
                        Name = "Chicken",
                        Quantity = 1,
                        Calories = 400,
                        Protein = 35,
                        Carbohydrates = 0,
                        Fat = 10,
                        CreatedAt = DateTime.UtcNow,
                    });
                    await ctxA.SaveChangesAsync(ct);

                    // Guaranteed fresh conflict EVERY attempt: a fully separate,
                    // already-committed writer changes the same row before this attempt's
                    // own recompute-and-write below runs.
                    await using (var ctxB = _pg.NewContext())
                    {
                        var entryInB = await ctxB.MealEntries.FirstAsync(e => e.Id == lunchEntryId);
                        entryInB.TotalCalories = conflictingValue++;
                        await ctxB.SaveChangesAsync();
                    }

                    await MealLogTotalsRecalculator.StageRecalculationAsync(ctxA, mealLogId, ct);
                    return true;
                }));

            Assert.NotNull(thrown.InnerException);

            // The acceptance bar: every attempt rolled back in full - Chicken was never
            // left committed on its own, regardless of which of the (exhausted) attempts
            // is inspected. The row's total reflects only the last successful,
            // unprotected competing writer's own commit - never a stale value paired with
            // orphaned content.
            await using var verify = _pg.NewContext();
            var finalEntry = await verify.MealEntries
                .Include(e => e.FoodItems)
                .FirstAsync(e => e.Id == lunchEntryId);
            Assert.Empty(finalEntry.FoodItems);
        }

        /// <summary>
        /// Gap 3, corrected: a concurrent ORDINARY food edit through the REAL
        /// <c>FoodItemsController.UpdateQuantity</c> endpoint racing a REAL
        /// <c>ApplyMealPlanToToday</c> call must never converge on a WRONG total, and
        /// must never silently lose either side's own edit - it must either (a) both
        /// succeed with the exact, correctly merged total, or (b) apply detects a
        /// genuine write-skew conflict against the concurrent edit's own recomputation
        /// and cleanly refuses (rolls back, reports its own error, no partial content),
        /// leaving the quantity edit's commit intact. This replaces a prior version of
        /// this test that weakened its own assertion in a DIFFERENT, unacceptable way
        /// (silently accepting a WRONG total under a reported SUCCESS) instead of fixing
        /// the production defect that made that undercount possible; see
        /// <see cref="a_stale_recompute_attempt_is_rejected_when_a_concurrent_recompute_already_committed_first"/>
        /// above for the deterministic reproduction of that defect's exact mechanism, and
        /// <see cref="MealLogTotalsRecalculator"/> for the fix: every writer of these
        /// cached totals now recomputes from a genuinely fresh read (never a value an EF
        /// Core identity map cached earlier in the same request) inside its own
        /// Serializable-protected, retried attempt.
        ///
        /// Tolerating outcome (b) for <c>apply</c> specifically (not for the ordinary
        /// edit) mirrors this file's own established tolerance in
        /// <see cref="concurrent_applies_creating_the_same_new_meal_entry_never_duplicate_it"/>
        /// ("at most one concurrent apply may lose the race"). NOTE: after the atomicity
        /// correction, BOTH sides now run their entire operation - content mutation and
        /// totals recompute together - through
        /// <see cref="MealLogTotalsRecalculator.ExecuteAtomicallyAsync{T}"/>'s own
        /// internal retry (up to <see cref="MealLogTotalsRecalculator.DefaultMaxAttempts"/>
        /// attempts each), so a SINGLE conflicted attempt on either side is no longer
        /// enough to surface a failure to its caller - the losing side's own retry, with a
        /// fresh read via `ChangeTracker.Clear()`, resolves the vast majority of such
        /// races silently. The residual reason apply can still, in principle, report its
        /// own conflict is narrower than before: both sides read the same
        /// entry+FoodItems predicate and write the same MealEntry row within their own
        /// Serializable transaction, so a genuine two-transaction write-skew cycle can
        /// still recur across MULTIPLE consecutive retries on both sides (PostgreSQL's
        /// SSI may resolve any single occurrence of the cycle by aborting either side,
        /// not always necessarily the same one) - requiring persistent, repeated bad luck
        /// rather than a single unlucky attempt. This tolerance is kept as a safety net
        /// against that now-much-rarer case, not because apply lacks retry protection.
        /// The ordinary edit's own response is asserted to always succeed unconditionally:
        /// empirically its own retry absorbs this conflict shape before ever returning to
        /// its caller, and it is the structurally simpler, single-row-scoped side of the
        /// two.
        ///
        /// The ordinary edit deliberately targets a manual food item ("Rice") INSIDE the
        /// SAME "Lunch" entry the plan itself applies to - not an unrelated meal type -
        /// so the shared write target is the actual row this mechanism protects.
        ///
        /// Real concurrency: two separate <see cref="TrainingContext"/> instances, with
        /// both controller calls STARTED before either is awaited, so the actual
        /// interleaving against the real PostgreSQL container is genuine and not forced.
        /// </summary>
        [DockerRequiredFact]
        public async Task concurrent_ordinary_quantity_edit_via_the_real_endpoint_converges_to_the_correct_total_after_an_apply()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser();
            var today = DateTime.UtcNow.Date;

            // Lunch: a pre-existing, manually-logged, NOT-consumed entry that the meal
            // plan below ALSO targets - the ordinary edit and apply's own insert+recompute
            // land on the exact same MealEntry row.
            int riceId;
            int mealLogId;
            await using (var seedCtx = _pg.NewContext())
            {
                var mealLog = new MealLog { UserId = userId, Date = today, CreatedAt = DateTime.UtcNow };
                seedCtx.MealLogs.Add(mealLog);
                await seedCtx.SaveChangesAsync();

                var lunch = new MealEntry { MealLogId = mealLog.Id, MealType = "Lunch", IsConsumed = false, CreatedAt = DateTime.UtcNow };
                seedCtx.MealEntries.Add(lunch);
                await seedCtx.SaveChangesAsync();

                var riceItem = new FoodItem
                {
                    MealEntryId = lunch.Id,
                    Name = "Rice",
                    Quantity = 1,
                    Calories = 150,
                    Protein = 3,
                    Carbohydrates = 33,
                    Fat = 0,
                    CreatedAt = DateTime.UtcNow,
                };
                seedCtx.FoodItems.Add(riceItem);
                await seedCtx.SaveChangesAsync();

                riceId = riceItem.Id;
                mealLogId = mealLog.Id;
            }

            var convoId = await SeedMealPlanConversation(userId, OneDayPlan(1, ("Lunch", "Grilled Chicken", 400)));

            await using var editCtx = _pg.NewContext();
            await using var applyCtx = _pg.NewContext();

            // Started together, not awaited individually - real, unforced interleaving.
            var quantityTask = Food(editCtx, userId).UpdateQuantity(riceId, 2m);
            var applyTask = Chat(applyCtx, userId).ApplyMealPlanToToday(convoId, day: 1, date: null);

            var quantityResult = await quantityTask;
            var applyResult = await applyTask;

            // The ordinary edit's own recomputation goes through
            // MealLogTotalsRecalculator's internal retry, which must absorb this exact
            // conflict shape transparently - it should never surface a conflict to its
            // own caller for this race.
            Assert.IsType<OkObjectResult>(quantityResult.Result);

            // Apply DOES retry its whole operation now (see this test's doc comment) - a
            // genuine write-skew conflict would usually be resolved silently by that
            // retry. The residual, much rarer possibility of apply exhausting its own
            // retries against a persistently-conflicting concurrent edit is still
            // tolerated here as a safety net. That is an ACCEPTABLE outcome (a clean,
            // detected refusal, not a silent wrong answer); reporting success with an
            // incorrect total is NOT.
            var applyLostTheRace = applyResult.Result is ObjectResult { StatusCode: 500 };
            if (!applyLostTheRace)
            {
                Assert.IsType<OkObjectResult>(applyResult.Result);
            }

            await using var verify = _pg.NewContext();

            // The quantity edit's own commit is never lost, regardless of which outcome
            // above occurred - Rice's own FoodItem row is never written by apply.
            var rice = await verify.FoodItems.FirstAsync(f => f.Id == riceId);
            Assert.Equal(2m, rice.Quantity);
            Assert.Equal(300m, rice.Calories); // scaled 150 * (2/1)

            var lunchEntry = await verify.MealEntries
                .Include(e => e.FoodItems)
                .FirstAsync(e => e.MealLogId == mealLogId && e.MealType == "Lunch");

            await using var apiReadCtx = _pg.NewContext();

            if (!applyLostTheRace)
            {
                // The fixture's exact final numbers: Rice scaled to quantity 2
                // (150 * 2 = 300) plus Grilled Chicken (400) = 700 - exact, not bounded.
                // Asserted via a genuinely fresh DbContext (never one either request
                // touched) AND via the real GetMealEntry API action, so both the
                // persisted row and what the API would actually return to a client are
                // confirmed - with no further edit of any kind performed between
                // settling and this assertion.
                Assert.Equal(2, lunchEntry.FoodItems.Count);
                Assert.Contains(lunchEntry.FoodItems, f => f.Name == "Grilled Chicken");
                Assert.Equal(700m, lunchEntry.TotalCalories);

                var apiEntry = await Food(apiReadCtx, userId).GetFoodItems(lunchEntry.Id);
                var apiOk = Assert.IsType<OkObjectResult>(apiEntry.Result);
                var apiItems = Assert.IsAssignableFrom<IEnumerable<FoodItem>>(apiOk.Value);
                Assert.Equal(300m, apiItems.Single(f => f.Name == "Rice").Calories);

                var apiEntryRead = await MealEntries(apiReadCtx, userId).GetMealEntry(lunchEntry.Id);
                var apiEntryOk = Assert.IsType<OkObjectResult>(apiEntryRead.Result);
                var apiEntryValue = Assert.IsType<MealEntry>(apiEntryOk.Value);
                Assert.Equal(700m, apiEntryValue.TotalCalories); // API-returned total matches the persisted rows
            }
            else
            {
                // Apply rolled back cleanly - no partially-applied Grilled Chicken. The
                // entry's total must still correctly reflect ONLY Rice's edit (300), via
                // both a fresh context and the real API, never a wrong/corrupted value.
                Assert.Single(lunchEntry.FoodItems);
                Assert.DoesNotContain(lunchEntry.FoodItems, f => f.Name == "Grilled Chicken");
                Assert.Equal(300m, lunchEntry.TotalCalories);

                var apiEntryRead = await MealEntries(apiReadCtx, userId).GetMealEntry(lunchEntry.Id);
                var apiEntryOk = Assert.IsType<OkObjectResult>(apiEntryRead.Result);
                var apiEntryValue = Assert.IsType<MealEntry>(apiEntryOk.Value);
                Assert.Equal(300m, apiEntryValue.TotalCalories);
            }
        }

        /// <summary>
        /// Gap 3 follow-up (from independent review): two concurrent applies for the SAME
        /// not-yet-existing meal type must never both insert a <c>MealEntry</c> - that
        /// would silently duplicate it (the <c>(MealLogId, MealType)</c> index is NOT
        /// unique, and neither apply endpoint re-checks the database immediately before
        /// insert; both endpoints only guard against duplicating a row they can already
        /// see in their OWN snapshot). This is a genuine phantom-read/write-skew shape
        /// that plain snapshot isolation (RepeatableRead) does not catch - which is why
        /// both apply endpoints now run under Serializable instead: PostgreSQL's
        /// predicate-locking SSI detects two transactions that each read "no Lunch entry
        /// exists" and then both try to insert one, and aborts one of them with the same
        /// recognized 40001 conflict shape used everywhere else in this file.
        ///
        /// Real concurrency: two separate <see cref="TrainingContext"/> instances calling
        /// the SAME <c>ApplyMealPlanToToday</c> endpoint for the SAME (new) day/target,
        /// both started before either is awaited.
        /// </summary>
        [DockerRequiredFact]
        public async Task concurrent_applies_creating_the_same_new_meal_entry_never_duplicate_it()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser();

            // No Lunch entry exists yet - BOTH concurrent calls will try to create one.
            var convoId = await SeedMealPlanConversation(userId, OneDayPlan(1, ("Lunch", "Grilled Chicken", 400)));

            await using var ctxA = _pg.NewContext();
            await using var ctxB = _pg.NewContext();

            var taskA = Chat(ctxA, userId).ApplyMealPlanToToday(convoId, day: 1, date: null);
            var taskB = Chat(ctxB, userId).ApplyMealPlanToToday(convoId, day: 1, date: null);

            var resultA = await taskA;
            var resultB = await taskB;

            bool Failed(ActionResult<ApplyMealPlanResponse> r) => r.Result is ObjectResult { StatusCode: 500 };
            var failures = new[] { Failed(resultA), Failed(resultB) }.Count(f => f);

            // At most one side may fail (a genuine conflict is an acceptable outcome); it
            // must never be the case that BOTH sides report success while having produced
            // two Lunch entries between them.
            Assert.True(failures <= 1, "at most one concurrent apply may lose the race");

            await using var verify = _pg.NewContext();
            var lunchEntries = await verify.MealEntries
                .Where(e => e.MealLog.UserId == userId && e.MealType == "Lunch")
                .Include(e => e.FoodItems)
                .ToListAsync();

            // The acceptance bar: never two Lunch entries for the same day, regardless of
            // which side "won".
            Assert.Single(lunchEntries);
            Assert.Contains(lunchEntries[0].FoodItems, f => f.Name == "Grilled Chicken");
        }
    }
}
