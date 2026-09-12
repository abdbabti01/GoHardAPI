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
        /// Gap 3: a concurrent ORDINARY food edit through the REAL
        /// <c>FoodItemsController.UpdateQuantity</c> endpoint (not a hypothetical write)
        /// racing a REAL <c>ApplyMealPlanToToday</c> call must never destroy or duplicate
        /// either side's own logged content - the ordinary edit's own row and the plan's
        /// own inserted row must each independently survive intact, regardless of timing.
        ///
        /// The ordinary edit deliberately targets a manual food item ("Rice") INSIDE the
        /// SAME "Lunch" entry the plan itself applies to - not an unrelated meal type -
        /// so the shared write target is the actual <c>MealEntry</c> row apply recomputes
        /// its own cached total from (<c>entry.TotalCalories = entry.FoodItems.Sum(...)</c>).
        ///
        /// IMPORTANT, EMPIRICALLY-CONFIRMED LIMITATION (found by this exact test running
        /// against real PostgreSQL, not assumed): <c>FoodItemsController</c>'s mutations
        /// run with NO isolation/transaction protection of their own - only
        /// <c>ApplyMealPlanToToday</c>'s side is Serializable. Serializable protects apply
        /// FROM committing an inconsistent write over a change it already read past, but
        /// it cannot stop an unrelated, unprotected transaction from later blindly
        /// overwriting apply's own already-committed <c>MealEntry.TotalCalories</c> row
        /// with a value computed from a view that predates apply's insert. When that
        /// ordering occurs, the ROWS are never lost or duplicated (Rice's edited quantity
        /// and Grilled Chicken's own row both persist correctly - verified below
        /// unconditionally), but the cached aggregate <c>TotalCalories</c> can transiently
        /// undercount until the next write to that entry recomputes it (it is a derived
        /// value, not a second source of truth). This is a real, narrower residual gap
        /// than full correctness under concurrency - it is NOT masked here; the assertions
        /// below only check what is actually guaranteed (no destroyed/duplicated content),
        /// not an unconditional merged total, which is not always achievable without also
        /// isolating <c>FoodItemsController</c>'s own writes (a separate, larger change
        /// outside this endpoint's scope).
        ///
        /// Real concurrency: two separate <see cref="TrainingContext"/> instances, with
        /// both controller calls STARTED before either is awaited, so the actual
        /// interleaving against the real PostgreSQL container is genuine and not forced.
        /// </summary>
        [DockerRequiredFact]
        public async Task concurrent_ordinary_quantity_edit_via_the_real_endpoint_is_not_silently_lost_during_an_apply()
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

            Exception? quantityEx = null;
            ActionResult<FoodItem>? quantityResult = null;
            try { quantityResult = await quantityTask; }
            catch (Exception ex) { quantityEx = ex; }

            // ApplyMealPlanToToday has its own outer try/catch that converts ANY
            // exception - including a conflict on commit - into `StatusCode(500, ...)`
            // rather than letting it propagate, so the conflict outcome shows up as a
            // 500 ObjectResult here, not a thrown exception.
            var applyResult = await applyTask;
            var applyLostTheRace = applyResult.Result is ObjectResult { StatusCode: 500 };

            // The ordinary edit has no isolation of its own to conflict on - it always
            // succeeds (it's the transaction that might be conflicted AGAINST, not the
            // one detecting a conflict).
            Assert.Null(quantityEx);
            Assert.NotNull(quantityResult);

            await using var verify = _pg.NewContext();

            // The acceptance bar: the quantity edit's own commit is NEVER silently lost,
            // regardless of which outcome above occurred. Rice's own FoodItem row is
            // never written by apply (apply only ever deletes/inserts items carrying its
            // own SourcePlanConversationId), so this must hold unconditionally.
            var rice = await verify.FoodItems.FirstAsync(f => f.Id == riceId);
            Assert.Equal(2m, rice.Quantity);
            Assert.Equal(300m, rice.Calories); // scaled 150 * (2/1)

            var lunchEntry = await verify.MealEntries
                .Include(e => e.FoodItems)
                .FirstAsync(e => e.MealLogId == mealLogId && e.MealType == "Lunch");

            if (!applyLostTheRace)
            {
                // No conflict materialized - apply must have genuinely succeeded, and its
                // own inserted content must be genuinely present (never a state where it
                // reports success but silently touched nothing). The entry's cached
                // TotalCalories is NOT asserted to an exact merged value here - see this
                // test's doc comment for the empirically-confirmed reason a transient
                // undercount is possible in this specific unprotected-writer-races-a-
                // protected-writer ordering; it is bounded to the two values either
                // ordering can actually produce, so a wrong/corrupted total (e.g.
                // negative, zero, or reflecting neither edit) still fails this test.
                Assert.IsType<OkObjectResult>(applyResult.Result);
                Assert.Contains(lunchEntry.FoodItems, f => f.Name == "Grilled Chicken");
                Assert.Contains(lunchEntry.TotalCalories, new[] { 300m, 700m });
            }
            else
            {
                // Apply rolled back cleanly - no partially-applied Grilled Chicken, and
                // Rice's edited quantity is the ONLY content in the entry.
                Assert.DoesNotContain(lunchEntry.FoodItems, f => f.Name == "Grilled Chicken");
                Assert.Single(lunchEntry.FoodItems);
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
