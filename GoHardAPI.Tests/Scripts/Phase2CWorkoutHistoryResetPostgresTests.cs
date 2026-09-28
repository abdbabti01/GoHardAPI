using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GoHardAPI.Data;
using GoHardAPI.Models;
using GoHardAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace GoHardAPI.Tests.Scripts
{
    /// <summary>
    /// Executes the committed manual Phase 2C reset script
    /// (<c>GoHardAPI/Scripts/Phase2C_WorkoutHistoryReset.sql</c>) section by section against a
    /// DISPOSABLE PostgreSQL 16 container. This is the only place the script is ever executed by
    /// code: nothing in the API references it (see <see cref="Phase2CWorkoutHistoryResetScriptIsolationTests"/>).
    ///
    /// Schema: built from the current EF model with <c>EnsureCreated</c>, like
    /// <see cref="HistoryPreservationPostgresFixture"/>. A from-empty <c>Database.Migrate()</c> is
    /// not possible on PostgreSQL (the early migrations emit SQL Server types such as
    /// <c>nvarchar</c>); production itself was created with EnsureCreated and then pre-stamped
    /// (see Program.cs), so the model-derived schema is the faithful stand-in. The FK inventory
    /// assertion below pins the exact constraints the script relies on.
    /// </summary>
    [Trait("Category", "PostgresIntegration")]
    public sealed class Phase2CWorkoutHistoryResetPostgresTests : IAsyncLifetime
    {
        private const string ConfirmSql = "SET gohard.confirm_reset = 'ERASE-WORKOUT-HISTORY';";

        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

        private bool _available;
        private string _cs = string.Empty;

        public async Task InitializeAsync()
        {
            try { await _container.StartAsync(); }
            catch (Exception ex)
            {
                PostgresRequirement.ThrowIfRequired(ex);
                _available = false;
                return;
            }
            _cs = _container.GetConnectionString();
            await using var ctx = NewContext();
            await ctx.Database.EnsureCreatedAsync();
            _available = true;
        }

        public async Task DisposeAsync()
        {
            try { await _container.DisposeAsync(); }
            catch { /* never started */ }
        }

        private TrainingContext NewContext() =>
            new(new DbContextOptionsBuilder<TrainingContext>().UseNpgsql(_cs).Options);

        // ---------------------------------------------------------------- tests

        [DockerRequiredFact]
        public async Task preview_is_read_only_reset_erases_only_workout_history_and_verify_is_all_zero()
        {
            Assert.True(_available, "PostgreSQL container must be available in CI");
            var (u1, u2) = await SeedTwoUsers();
            var before = await Snapshot();

            // PREVIEW: returns counts, mutates nothing.
            var preview = await Rows(Section("PREVIEW"));
            Assert.Equal(4, preview["erase:Sessions"]);
            Assert.Equal(4, preview["erase:Exercises"]);
            Assert.Equal(8, preview["erase:ExerciseSets"]);
            Assert.Equal(2, preview["erase:ChatConversations(progress_analysis)"]);
            Assert.Equal(4, preview["erase:ChatMessages(progress_analysis)"]);
            Assert.Equal(2, preview["erase:SharedWorkouts"]);
            Assert.Equal(2, preview["erase:SharedWorkoutLikes"]);
            Assert.Equal(2, preview["erase:SharedWorkoutSaves"]);
            Assert.Equal(2, preview["detach:SessionCreateOperations(SessionId)"]);
            Assert.Equal(4, preview["reset:ProgramWorkouts(progress)"]);
            Assert.Equal(2, preview["reset:Programs(progress)"]);
            Assert.Equal(2, preview["reset:Programs(completed->active)"]);
            Assert.Equal(0, preview["blocker:Programs.SourceConversationId->progress_analysis"]);
            Assert.Equal(0, preview["blocker:FoodItems.SourcePlanConversationId->progress_analysis"]);
            Assert.Equal(before, await Snapshot());

            // The PREVIEW also lists every FK that points at an affected table; it must match
            // the table documented in the runbook.
            var fks = await FkInventory(Section("PREVIEW"));
            var expectedFks = new[]
            {
                "ChatMessages.FK_ChatMessages_ChatConversations_ConversationId->ChatConversations:CASCADE",
                "Exercises.FK_Exercises_Sessions_SessionId->Sessions:CASCADE",
                "ExerciseSets.FK_ExerciseSets_Exercises_ExerciseId->Exercises:CASCADE",
                "FoodItems.FK_FoodItems_ChatConversations_SourcePlanConversationId->ChatConversations:SET NULL",
                "Programs.FK_Programs_ChatConversations_SourceConversationId->ChatConversations:NO ACTION",
                "ProgramWorkouts.FK_ProgramWorkouts_Programs_ProgramId->Programs:CASCADE",
                "SessionCreateOperations.FK_SessionCreateOperations_Sessions_SessionId->Sessions:SET NULL",
                "Sessions.FK_Sessions_ProgramWorkouts_ProgramWorkoutId->ProgramWorkouts:CASCADE",
                "Sessions.FK_Sessions_Programs_ProgramId->Programs:CASCADE",
                "SharedWorkoutLikes.FK_SharedWorkoutLikes_SharedWorkouts_SharedWorkoutId->SharedWorkouts:CASCADE",
                "SharedWorkoutSaves.FK_SharedWorkoutSaves_SharedWorkouts_SharedWorkoutId->SharedWorkouts:CASCADE",
            };
            Assert.Equal(expectedFks.OrderBy(x => x, StringComparer.Ordinal), fks.OrderBy(x => x, StringComparer.Ordinal));

            // RESET
            await Exec(ConfirmSql + Section("RESET"));

            await using (var ctx = NewContext())
            {
                Assert.Equal(0, await ctx.Sessions.CountAsync());
                Assert.Equal(0, await ctx.Exercises.CountAsync());
                Assert.Equal(0, await ctx.ExerciseSets.CountAsync());
                Assert.Equal(0, await ctx.ChatConversations.CountAsync(c => c.Type == "progress_analysis"));
                Assert.Equal(0, await ctx.SharedWorkouts.CountAsync());
                Assert.Equal(0, await ctx.SharedWorkoutLikes.CountAsync());
                Assert.Equal(0, await ctx.SharedWorkoutSaves.CountAsync());

                // Only the progress_analysis messages are gone.
                Assert.Equal(before["ChatMessages(other)"], await ctx.ChatMessages.CountAsync());
                Assert.Equal(before["ChatConversations(other)"], await ctx.ChatConversations.CountAsync());
                Assert.Equal(2, await ctx.ChatConversations.CountAsync(c => c.Type == "workout_plan"));

                // SessionCreateOperations survive as tombstones.
                var ops = await ctx.SessionCreateOperations.ToListAsync();
                Assert.Equal(2, ops.Count);
                Assert.All(ops, o => Assert.Null(o.SessionId));
                Assert.All(ops, o => Assert.NotNull(o.CompletedAt));

                // Programs + workouts kept, progress reset.
                var programs = await ctx.Programs.Include(p => p.Workouts).OrderBy(p => p.Id).ToListAsync();
                Assert.Equal(before["Programs"], programs.Count);
                Assert.All(programs, p =>
                {
                    Assert.Equal(1, p.CurrentWeek);
                    Assert.Equal(1, p.CurrentDay);
                    Assert.False(p.IsCompleted);
                    Assert.Null(p.CompletedAt);
                });
                foreach (var uid in new[] { u1, u2 })
                {
                    var userPrograms = programs.Where(p => p.UserId == uid).ToList();
                    var completed = userPrograms.Single(p => p.Title == "Completed");
                    Assert.Equal("active", completed.Status);
                    Assert.True(completed.IsActive);
                    Assert.Equal("draft", userPrograms.Single(p => p.Title == "Draft").Status);
                    Assert.False(userPrograms.Single(p => p.Title == "Draft").IsActive);
                    Assert.Equal("archived", userPrograms.Single(p => p.Title == "Archived").Status);
                    Assert.False(userPrograms.Single(p => p.Title == "Archived").IsActive);
                    Assert.Equal("[{\"name\":\"Squat\",\"weight\":100}]", completed.Workouts.Single(w => w.WorkoutName == "A").ExercisesJson);
                }
                Assert.All(programs.SelectMany(p => p.Workouts), w =>
                {
                    Assert.False(w.IsCompleted);
                    Assert.Null(w.CompletedAt);
                    Assert.False(w.IsSkipped);
                    Assert.Null(w.SkippedAt);
                });

                // User-written completion notes are preserved (owner ruling).
                Assert.Equal(
                    new[] { "good", "good" },
                    programs.SelectMany(p => p.Workouts).Where(w => w.CompletionNotes != null).Select(w => w.CompletionNotes!).ToArray());

                // Meal-plan provenance on food items was not detached.
                Assert.Equal(2, await ctx.FoodItems.CountAsync(f => f.SourcePlanConversationId != null));
            }

            var after = await Snapshot();
            foreach (var key in new[]
            {
                "Users", "Goals", "GoalProgressHistory", "BodyMetrics", "RunSessions", "MealLogs",
                "MealEntries", "FoodItems", "ExerciseTemplates", "Programs", "ProgramWorkouts",
                "SessionCreateOperations", "ChatConversations(other)", "ChatMessages(other)",
            })
            {
                Assert.Equal(before[key], after[key]);
            }
            Assert.Equal(before["GoalCurrentValues"], after["GoalCurrentValues"]);

            // VERIFY: every must-be-zero row is zero; preserved rows match the PREVIEW.
            var verify = await Rows(Section("VERIFY"));
            var zeros = verify.Where(kv => kv.Key.StartsWith("must_be_zero:")).ToList();
            Assert.True(zeros.Count >= 10);
            Assert.All(zeros, kv => Assert.True(kv.Value == 0, kv.Key));
            var preserved = verify.Where(kv => kv.Key.StartsWith("preserved:")).ToList();
            Assert.NotEmpty(preserved);
            Assert.All(preserved, kv => Assert.Equal(preview[kv.Key], kv.Value));
        }

        [DockerRequiredFact]
        public async Task reset_without_the_session_confirmation_aborts_and_changes_nothing()
        {
            Assert.True(_available, "PostgreSQL container must be available in CI");
            await SeedTwoUsers();
            var before = await Snapshot();

            var ex = await Assert.ThrowsAsync<PostgresException>(() => Exec(Section("RESET")));
            Assert.Contains("gohard.confirm_reset", ex.MessageText);

            Assert.Equal(before, await Snapshot());
        }

        [DockerRequiredFact]
        public async Task reset_refuses_when_a_preserved_row_references_a_progress_analysis_conversation()
        {
            Assert.True(_available, "PostgreSQL container must be available in CI");
            await SeedTwoUsers();
            await using (var ctx = NewContext())
            {
                var conv = await ctx.ChatConversations.FirstAsync(c => c.Type == "progress_analysis");
                var item = await ctx.FoodItems.FirstAsync();
                item.SourcePlanConversationId = conv.Id;
                await ctx.SaveChangesAsync();
            }
            var before = await Snapshot();

            var preview = await Rows(Section("PREVIEW"));
            Assert.Equal(1, preview["blocker:FoodItems.SourcePlanConversationId->progress_analysis"]);

            var ex = await Assert.ThrowsAsync<PostgresException>(() => Exec(ConfirmSql + Section("RESET")));
            Assert.Contains("progress_analysis", ex.MessageText);

            Assert.Equal(before, await Snapshot());
        }

        /// <summary>
        /// Simulates psql's ON_ERROR_ROLLBACK: the RESET section is run statement by statement
        /// with a SAVEPOINT around each one, a failing statement is rolled back to its savepoint
        /// and execution continues, then COMMIT. A failed guard must still leave the database
        /// unchanged; with the guards satisfied the same mode must perform the full reset.
        /// </summary>
        [DockerRequiredFact]
        public async Task failed_guards_change_nothing_even_when_each_statement_runs_in_a_savepoint()
        {
            Assert.True(_available, "PostgreSQL container must be available in CI");
            await SeedTwoUsers();
            var before = await Snapshot();

            // 1. No confirmation.
            var errors = await ExecWithSavepointPerStatement(null, Section("RESET"));
            Assert.Contains(errors, e => e.Contains("gohard.confirm_reset"));
            Assert.Equal(before, await Snapshot());

            // 2. Confirmed, but a blocker is present.
            int itemId, originalSource;
            await using (var ctx = NewContext())
            {
                var conv = await ctx.ChatConversations.FirstAsync(c => c.Type == "progress_analysis");
                var item = await ctx.FoodItems.FirstAsync();
                (itemId, originalSource) = (item.Id, item.SourcePlanConversationId!.Value);
                item.SourcePlanConversationId = conv.Id;
                await ctx.SaveChangesAsync();
            }
            var blocked = await Snapshot();
            errors = await ExecWithSavepointPerStatement(ConfirmSql, Section("RESET"));
            Assert.Contains(errors, e => e.Contains("progress_analysis"));
            Assert.Equal(blocked, await Snapshot());

            // 3. Guards satisfied: the same execution mode performs the reset.
            await Exec($"UPDATE \"FoodItems\" SET \"SourcePlanConversationId\" = {originalSource} WHERE \"Id\" = {itemId};");
            errors = await ExecWithSavepointPerStatement(ConfirmSql, Section("RESET"));
            Assert.Empty(errors);
            var after = await Snapshot();
            Assert.Equal(0, after["Sessions"]);
            Assert.Equal(0, after["ChatConversations(progress)"]);
            Assert.Equal(0, after["SharedWorkouts"]);
            Assert.Equal(0, after["ProgramWorkoutsMarked"]);
            Assert.Equal(before["Users"], after["Users"]);
        }

        // ---------------------------------------------------------------- seed

        private async Task<(int, int)> SeedTwoUsers()
        {
            await using var ctx = NewContext();
            var ids = new List<int>();
            foreach (var n in new[] { "a", "b" })
            {
                var user = new User { Name = n, Username = n, Email = $"{n}@example.test", PasswordHash = "x" };
                ctx.Users.Add(user);
                await ctx.SaveChangesAsync();
                ids.Add(user.Id);

                var workoutPlanConv = new ChatConversation { UserId = user.Id, Type = "workout_plan", Title = "plan" };
                var mealPlanConv = new ChatConversation { UserId = user.Id, Type = "meal_plan", Title = "meals" };
                var progressConv = new ChatConversation { UserId = user.Id, Type = "progress_analysis", Title = "progress" };
                workoutPlanConv.Messages.Add(new ChatMessage { Role = "user", Content = "plan me" });
                mealPlanConv.Messages.Add(new ChatMessage { Role = "user", Content = "feed me" });
                progressConv.Messages.Add(new ChatMessage { Role = "user", Content = "how am I doing" });
                progressConv.Messages.Add(new ChatMessage { Role = "assistant", Content = "you lifted 100 lbs" });
                ctx.ChatConversations.AddRange(workoutPlanConv, mealPlanConv, progressConv);
                await ctx.SaveChangesAsync();

                var completed = new Models.Program
                {
                    UserId = user.Id, Title = "Completed", CurrentWeek = 4, CurrentDay = 7, IsActive = false,
                    IsCompleted = true, CompletedAt = DateTime.UtcNow, Status = "completed",
                    SourceConversationId = workoutPlanConv.Id,
                };
                completed.Workouts.Add(new ProgramWorkout
                {
                    WeekNumber = 1, DayNumber = 1, WorkoutName = "A", ExercisesJson = "[{\"name\":\"Squat\",\"weight\":100}]",
                    IsCompleted = true, CompletedAt = DateTime.UtcNow, CompletionNotes = "good",
                });
                completed.Workouts.Add(new ProgramWorkout
                {
                    WeekNumber = 1, DayNumber = 2, WorkoutName = "B", IsSkipped = true, SkippedAt = DateTime.UtcNow,
                });
                var draft = new Models.Program { UserId = user.Id, Title = "Draft", IsActive = false, Status = "draft" };
                draft.Workouts.Add(new ProgramWorkout { WeekNumber = 1, DayNumber = 1, WorkoutName = "D" });
                var archived = new Models.Program { UserId = user.Id, Title = "Archived", IsActive = false, Status = "archived" };
                ctx.Programs.AddRange(completed, draft, archived);
                await ctx.SaveChangesAsync();

                var s1 = NewSession(user.Id, completed.Id, completed.Workouts.First().Id);
                var s2 = NewSession(user.Id, null, null);
                ctx.Sessions.AddRange(s1, s2);
                await ctx.SaveChangesAsync();

                ctx.SessionCreateOperations.Add(new SessionCreateOperation
                {
                    UserId = user.Id, ClientOperationId = Guid.NewGuid(), SessionId = s1.Id, CompletedAt = DateTime.UtcNow,
                });

                var shared = new SharedWorkout
                {
                    OriginalId = s1.Id, Type = "session", SharedByUserId = user.Id, WorkoutName = "w",
                    ExercisesJson = "[]", Category = "Strength", LikeCount = 1, SaveCount = 1, CommentCount = 1,
                };
                shared.Likes.Add(new SharedWorkoutLike { UserId = user.Id });
                shared.Saves.Add(new SharedWorkoutSave { UserId = user.Id });
                ctx.SharedWorkouts.Add(shared);

                var goal = new Goal { UserId = user.Id, GoalType = "Weight", TargetValue = 80, CurrentValue = 90 };
                goal.ProgressHistory.Add(new GoalProgress { Value = 90 });
                ctx.Goals.Add(goal);
                ctx.BodyMetrics.Add(new BodyMetric { UserId = user.Id, Weight = 90 });
                ctx.RunSessions.Add(new RunSession { UserId = user.Id, Distance = 5 });
                ctx.ExerciseTemplates.Add(new ExerciseTemplate { Name = "Custom " + n, IsCustom = true, CreatedByUserId = user.Id });

                var log = new MealLog { UserId = user.Id, Date = DateTime.UtcNow.Date };
                var entry = new MealEntry { MealType = "Lunch" };
                entry.FoodItems.Add(new FoodItem { Name = "Rice", SourcePlanConversationId = mealPlanConv.Id, SourcePlanDay = 1 });
                log.MealEntries.Add(entry);
                ctx.MealLogs.Add(log);

                await ctx.SaveChangesAsync();
            }
            return (ids[0], ids[1]);
        }

        private static Session NewSession(int userId, int? programId, int? programWorkoutId)
        {
            var session = new Session
            {
                UserId = userId, Status = "completed", ProgramId = programId, ProgramWorkoutId = programWorkoutId,
            };
            var exercise = new Exercise { Name = "Squat" };
            exercise.ExerciseSets.Add(new ExerciseSet { SetNumber = 1, Reps = 5, Weight = 100 });
            exercise.ExerciseSets.Add(new ExerciseSet { SetNumber = 2, Reps = 5, Weight = 100 });
            session.Exercises.Add(exercise);
            return session;
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>Row counts (and goal values) of every table the reset must not touch or must empty.</summary>
        private async Task<Dictionary<string, long>> Snapshot()
        {
            var sql = new Dictionary<string, string>
            {
                ["Users"] = "SELECT COUNT(*) FROM \"Users\"",
                ["Goals"] = "SELECT COUNT(*) FROM \"Goals\"",
                ["GoalCurrentValues"] = "SELECT COALESCE(SUM(\"CurrentValue\"), 0)::bigint FROM \"Goals\"",
                ["GoalProgressHistory"] = "SELECT COUNT(*) FROM \"GoalProgressHistory\"",
                ["BodyMetrics"] = "SELECT COUNT(*) FROM \"BodyMetrics\"",
                ["RunSessions"] = "SELECT COUNT(*) FROM \"RunSessions\"",
                ["MealLogs"] = "SELECT COUNT(*) FROM \"MealLogs\"",
                ["MealEntries"] = "SELECT COUNT(*) FROM \"MealEntries\"",
                ["FoodItems"] = "SELECT COUNT(*) FROM \"FoodItems\"",
                ["FoodItemsWithSource"] = "SELECT COUNT(*) FROM \"FoodItems\" WHERE \"SourcePlanConversationId\" IS NOT NULL",
                ["ExerciseTemplates"] = "SELECT COUNT(*) FROM \"ExerciseTemplates\"",
                ["Programs"] = "SELECT COUNT(*) FROM \"Programs\"",
                ["ProgramsCompleted"] = "SELECT COUNT(*) FROM \"Programs\" WHERE \"IsCompleted\" OR \"Status\" = 'completed'",
                ["ProgramWorkouts"] = "SELECT COUNT(*) FROM \"ProgramWorkouts\"",
                ["ProgramWorkoutsMarked"] = "SELECT COUNT(*) FROM \"ProgramWorkouts\" WHERE \"IsCompleted\" OR \"IsSkipped\"",
                ["SessionCreateOperations"] = "SELECT COUNT(*) FROM \"SessionCreateOperations\"",
                ["SessionCreateOperationsLinked"] = "SELECT COUNT(*) FROM \"SessionCreateOperations\" WHERE \"SessionId\" IS NOT NULL",
                ["Sessions"] = "SELECT COUNT(*) FROM \"Sessions\"",
                ["Exercises"] = "SELECT COUNT(*) FROM \"Exercises\"",
                ["ExerciseSets"] = "SELECT COUNT(*) FROM \"ExerciseSets\"",
                ["SharedWorkouts"] = "SELECT COUNT(*) FROM \"SharedWorkouts\"",
                ["SharedWorkoutLikes"] = "SELECT COUNT(*) FROM \"SharedWorkoutLikes\"",
                ["SharedWorkoutSaves"] = "SELECT COUNT(*) FROM \"SharedWorkoutSaves\"",
                ["ChatConversations(progress)"] = "SELECT COUNT(*) FROM \"ChatConversations\" WHERE \"Type\" = 'progress_analysis'",
                ["ChatConversations(other)"] = "SELECT COUNT(*) FROM \"ChatConversations\" WHERE \"Type\" <> 'progress_analysis'",
                ["ChatMessages(other)"] = "SELECT COUNT(*) FROM \"ChatMessages\" m JOIN \"ChatConversations\" c ON c.\"Id\" = m.\"ConversationId\" WHERE c.\"Type\" <> 'progress_analysis'",
                ["ChatMessages"] = "SELECT COUNT(*) FROM \"ChatMessages\"",
            };
            await using var conn = new NpgsqlConnection(_cs);
            await conn.OpenAsync();
            var result = new Dictionary<string, long>();
            foreach (var (key, q) in sql)
            {
                await using var cmd = new NpgsqlCommand(q, conn);
                result[key] = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            }
            return result;
        }

        private async Task Exec(string sql)
        {
            await using var conn = new NpgsqlConnection(_cs);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Runs <paramref name="section"/> the way psql does with ON_ERROR_ROLLBACK=on: each
        /// top-level statement after BEGIN is wrapped in SAVEPOINT / RELEASE, a failure is
        /// rolled back to the savepoint and execution continues; COMMIT runs at the end.
        /// Returns the error messages.
        /// </summary>
        private async Task<List<string>> ExecWithSavepointPerStatement(string? sessionSetup, string section)
        {
            var errors = new List<string>();
            await using var conn = new NpgsqlConnection(_cs);
            await conn.OpenAsync();
            if (sessionSetup != null) await Run(sessionSetup);
            foreach (var stmt in SplitStatements(section))
            {
                var keyword = stmt.Trim().TrimEnd(';').Trim().ToUpperInvariant();
                if (keyword is "BEGIN" or "COMMIT")
                {
                    await Run(stmt);
                    continue;
                }
                await Run("SAVEPOINT on_error_rollback;");
                try
                {
                    await Run(stmt);
                    await Run("RELEASE SAVEPOINT on_error_rollback;");
                }
                catch (PostgresException ex)
                {
                    errors.Add(ex.MessageText);
                    await Run("ROLLBACK TO SAVEPOINT on_error_rollback;");
                }
            }
            return errors;

            async Task Run(string sql)
            {
                await using var cmd = new NpgsqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>Splits SQL into top-level statements (ends with ';' outside $$ bodies; drops comment-only lines).</summary>
        private static List<string> SplitStatements(string sql)
        {
            var statements = new List<string>();
            var current = new System.Text.StringBuilder();
            var inDollar = false;
            foreach (var raw in sql.Split('\n'))
            {
                var line = raw.TrimEnd('\r');
                if (!inDollar && current.Length == 0 && (line.Trim().Length == 0 || line.TrimStart().StartsWith("--")))
                {
                    continue;
                }
                current.AppendLine(line);
                if (Regex.Matches(line, @"\$\$").Count % 2 == 1) inDollar = !inDollar;
                if (!inDollar && line.TrimEnd().EndsWith(";"))
                {
                    statements.Add(current.ToString());
                    current.Clear();
                }
            }
            Assert.True(current.ToString().Trim().Length == 0, "unterminated statement in section");
            return statements;
        }

        /// <summary>Reads the first (check, count) result set of a section.</summary>
        private async Task<Dictionary<string, long>> Rows(string sql)
        {
            await using var conn = new NpgsqlConnection(_cs);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            var rows = new Dictionary<string, long>();
            do
            {
                if (reader.FieldCount == 2 && reader.GetName(0) == "check" && reader.GetName(1) == "count")
                {
                    while (await reader.ReadAsync())
                    {
                        rows[reader.GetString(0)] = reader.GetInt64(1);
                    }
                    return rows;
                }
            } while (await reader.NextResultAsync());
            throw new InvalidOperationException("section returned no (check, count) result set");
        }

        /// <summary>Reads the PREVIEW's FK inventory result set as "table.constraint->referenced:ON DELETE".</summary>
        private async Task<List<string>> FkInventory(string sql)
        {
            await using var conn = new NpgsqlConnection(_cs);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            do
            {
                if (reader.FieldCount > 0 && reader.GetName(0) == "referencing_table")
                {
                    var fks = new List<string>();
                    while (await reader.ReadAsync())
                    {
                        fks.Add($"{reader.GetString(0)}.{reader.GetString(1)}->{reader.GetString(2)}:{reader.GetString(3)}");
                    }
                    return fks;
                }
            } while (await reader.NextResultAsync());
            throw new InvalidOperationException("PREVIEW returned no FK inventory result set");
        }

        private static string Section(string name)
        {
            var script = File.ReadAllText(Path.Combine(FindRepoRoot(), "GoHardAPI", "Scripts", "Phase2C_WorkoutHistoryReset.sql"));
            var parts = Regex.Split(script, @"^-- (PREVIEW|RESET|VERIFY)[ \t]*\r?$", RegexOptions.Multiline);
            // parts = [header, "PREVIEW", body, "RESET", body, "VERIFY", body]
            Assert.Equal(7, parts.Length);
            for (var i = 1; i < parts.Length; i += 2)
            {
                if (parts[i] == name) return parts[i + 1];
            }
            throw new InvalidOperationException($"section {name} not found");
        }

        internal static string FindRepoRoot()
        {
            var dir = AppContext.BaseDirectory;
            while (dir != null && !File.Exists(Path.Combine(dir, "GoHardAPI.sln")))
            {
                dir = Directory.GetParent(dir)?.FullName;
            }
            return dir ?? throw new DirectoryNotFoundException("repo root (GoHardAPI.sln) not found");
        }
    }

    /// <summary>The reset is a manual operation: no application code path may reference the script.</summary>
    public class Phase2CWorkoutHistoryResetScriptIsolationTests
    {
        [Fact]
        public void script_exists_and_is_never_referenced_by_the_application()
        {
            var root = Phase2CWorkoutHistoryResetPostgresTests.FindRepoRoot();
            Assert.True(File.Exists(Path.Combine(root, "GoHardAPI", "Scripts", "Phase2C_WorkoutHistoryReset.sql")));
            Assert.True(File.Exists(Path.Combine(root, "GoHardAPI", "Scripts", "Phase2C_WorkoutHistoryReset.md")));

            var sep = Path.DirectorySeparatorChar;
            foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "GoHardAPI"), "*.cs", SearchOption.AllDirectories)
                         .Where(f => !f.Contains($"{sep}obj{sep}") && !f.Contains($"{sep}bin{sep}")))
            {
                Assert.DoesNotContain("Phase2C", File.ReadAllText(file));
            }
            Assert.DoesNotContain("Phase2C", File.ReadAllText(Path.Combine(root, "GoHardAPI", "GoHardAPI.csproj")));
            Assert.DoesNotContain("Phase2C", File.ReadAllText(Path.Combine(root, "Dockerfile")));
        }
    }
}
