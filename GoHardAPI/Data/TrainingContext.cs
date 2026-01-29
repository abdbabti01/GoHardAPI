using GoHardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GoHardAPI.Data
{
    public class TrainingContext : DbContext
    {
        public TrainingContext(DbContextOptions<TrainingContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Exercise> Exercises { get; set; }
        public DbSet<ExerciseTemplate> ExerciseTemplates { get; set; }
        public DbSet<ExerciseSet> ExerciseSets { get; set; }
        public DbSet<ChatConversation> ChatConversations { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<SharedWorkout> SharedWorkouts { get; set; }
        public DbSet<SharedWorkoutLike> SharedWorkoutLikes { get; set; }
        public DbSet<SharedWorkoutSave> SharedWorkoutSaves { get; set; }
        public DbSet<WorkoutTemplate> WorkoutTemplates { get; set; }
        public DbSet<WorkoutTemplateRating> WorkoutTemplateRatings { get; set; }
        public DbSet<Goal> Goals { get; set; }
        public DbSet<GoalProgress> GoalProgressHistory { get; set; }
        public DbSet<BodyMetric> BodyMetrics { get; set; }
        public DbSet<Models.Program> Programs { get; set; }
        public DbSet<ProgramWorkout> ProgramWorkouts { get; set; }

        // Nutrition Tracking
        public DbSet<FoodTemplate> FoodTemplates { get; set; }
        public DbSet<MealLog> MealLogs { get; set; }
        public DbSet<MealEntry> MealEntries { get; set; }
        public DbSet<FoodItem> FoodItems { get; set; }
        public DbSet<NutritionGoal> NutritionGoals { get; set; }
        public DbSet<MealPlan> MealPlans { get; set; }
        public DbSet<MealPlanDay> MealPlanDays { get; set; }
        public DbSet<MealPlanMeal> MealPlanMeals { get; set; }
        public DbSet<MealPlanFoodItem> MealPlanFoodItems { get; set; }

        // Friends & Messaging
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<DirectMessage> DirectMessages { get; set; }
        public DbSet<DMConversation> DMConversations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure DateTime to always use UTC for PostgreSQL compatibility
            // FIX: Previous converter was calling ToUniversalTime() which could shift times incorrectly
            // if the DateTime was already UTC but had Kind=Utc set. The correct approach is to
            // ALWAYS treat incoming timestamps as UTC (since clients send UTC with 'Z' suffix)
            // and just mark them as UTC without any conversion.
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(
                            new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                                // On write: treat the value AS UTC without shifting
                                // If Kind is Utc or Unspecified, keep the value as-is
                                // If Kind is Local, convert to UTC (this handles edge cases)
                                v => v.Kind == DateTimeKind.Local
                                    ? v.ToUniversalTime()
                                    : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                                // On read: mark as UTC
                                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
                            )
                        );
                    }
                }
            }

            // Configure User-Session relationship
            modelBuilder.Entity<Session>()
                .HasOne(s => s.User)
                .WithMany(u => u.Sessions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Session-Exercise relationship
            modelBuilder.Entity<Exercise>()
                .HasOne(e => e.Session)
                .WithMany(s => s.Exercises)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ExerciseTemplate-Exercise relationship
            modelBuilder.Entity<Exercise>()
                .HasOne(e => e.ExerciseTemplate)
                .WithMany()
                .HasForeignKey(e => e.ExerciseTemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure ExerciseTemplate-User relationship (for custom templates)
            modelBuilder.Entity<ExerciseTemplate>()
                .HasOne(et => et.CreatedByUser)
                .WithMany()
                .HasForeignKey(et => et.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Exercise-ExerciseSet relationship
            modelBuilder.Entity<ExerciseSet>()
                .HasOne(es => es.Exercise)
                .WithMany(e => e.ExerciseSets)
                .HasForeignKey(es => es.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add index for faster set queries
            modelBuilder.Entity<ExerciseSet>()
                .HasIndex(es => new { es.ExerciseId, es.SetNumber });

            // Configure ChatConversation-User relationship
            modelBuilder.Entity<ChatConversation>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ChatMessage-ChatConversation relationship
            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add indexes for chat queries
            modelBuilder.Entity<ChatConversation>()
                .HasIndex(c => new { c.UserId, c.CreatedAt });

            modelBuilder.Entity<ChatMessage>()
                .HasIndex(m => new { m.ConversationId, m.CreatedAt });

            // Configure SharedWorkout-User relationship
            modelBuilder.Entity<SharedWorkout>()
                .HasOne(sw => sw.SharedByUser)
                .WithMany()
                .HasForeignKey(sw => sw.SharedByUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure SharedWorkoutLike relationships
            modelBuilder.Entity<SharedWorkoutLike>()
                .HasOne(swl => swl.SharedWorkout)
                .WithMany(sw => sw.Likes)
                .HasForeignKey(swl => swl.SharedWorkoutId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SharedWorkoutLike>()
                .HasOne(swl => swl.User)
                .WithMany()
                .HasForeignKey(swl => swl.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Prevent cascade cycles

            // Configure SharedWorkoutSave relationships
            modelBuilder.Entity<SharedWorkoutSave>()
                .HasOne(sws => sws.SharedWorkout)
                .WithMany(sw => sw.Saves)
                .HasForeignKey(sws => sws.SharedWorkoutId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SharedWorkoutSave>()
                .HasOne(sws => sws.User)
                .WithMany()
                .HasForeignKey(sws => sws.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Prevent cascade cycles

            // Add indexes for shared workout queries
            modelBuilder.Entity<SharedWorkout>()
                .HasIndex(sw => new { sw.Category, sw.SharedAt });

            modelBuilder.Entity<SharedWorkoutLike>()
                .HasIndex(swl => new { swl.SharedWorkoutId, swl.UserId })
                .IsUnique();

            modelBuilder.Entity<SharedWorkoutSave>()
                .HasIndex(sws => new { sws.SharedWorkoutId, sws.UserId })
                .IsUnique();

            // Configure WorkoutTemplate-User relationship
            modelBuilder.Entity<WorkoutTemplate>()
                .HasOne(wt => wt.CreatedByUser)
                .WithMany()
                .HasForeignKey(wt => wt.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure WorkoutTemplateRating relationships
            modelBuilder.Entity<WorkoutTemplateRating>()
                .HasOne(wtr => wtr.WorkoutTemplate)
                .WithMany(wt => wt.Ratings)
                .HasForeignKey(wtr => wtr.WorkoutTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkoutTemplateRating>()
                .HasOne(wtr => wtr.User)
                .WithMany()
                .HasForeignKey(wtr => wtr.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Prevent cascade cycles

            // Add indexes for template queries
            modelBuilder.Entity<WorkoutTemplate>()
                .HasIndex(wt => new { wt.IsCustom, wt.IsActive, wt.Category });

            modelBuilder.Entity<WorkoutTemplateRating>()
                .HasIndex(wtr => new { wtr.WorkoutTemplateId, wtr.UserId })
                .IsUnique();

            // Configure Goal-User relationship
            modelBuilder.Entity<Goal>()
                .HasOne(g => g.User)
                .WithMany()
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure GoalProgress-Goal relationship
            modelBuilder.Entity<GoalProgress>()
                .HasOne(gp => gp.Goal)
                .WithMany(g => g.ProgressHistory)
                .HasForeignKey(gp => gp.GoalId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add indexes for goal queries
            modelBuilder.Entity<Goal>()
                .HasIndex(g => new { g.UserId, g.IsActive, g.IsCompleted });

            modelBuilder.Entity<GoalProgress>()
                .HasIndex(gp => new { gp.GoalId, gp.RecordedAt });

            // Configure BodyMetric-User relationship
            modelBuilder.Entity<BodyMetric>()
                .HasOne(bm => bm.User)
                .WithMany()
                .HasForeignKey(bm => bm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add index for body metrics queries
            modelBuilder.Entity<BodyMetric>()
                .HasIndex(bm => new { bm.UserId, bm.RecordedAt });

            // Configure Program-User relationship
            modelBuilder.Entity<Models.Program>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Program-Goal relationship (CASCADE DELETE)
            modelBuilder.Entity<Models.Program>()
                .HasOne(p => p.Goal)
                .WithMany()
                .HasForeignKey(p => p.GoalId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ProgramWorkout-Program relationship (CASCADE DELETE)
            modelBuilder.Entity<ProgramWorkout>()
                .HasOne(pw => pw.Program)
                .WithMany(p => p.Workouts)
                .HasForeignKey(pw => pw.ProgramId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Session-Program relationship (CASCADE DELETE)
            // When a Program is deleted, delete all Sessions created from it
            modelBuilder.Entity<Session>()
                .HasOne(s => s.Program)
                .WithMany()
                .HasForeignKey(s => s.ProgramId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Session-ProgramWorkout relationship (CASCADE DELETE)
            // When a ProgramWorkout is deleted, delete all Sessions created from it
            modelBuilder.Entity<Session>()
                .HasOne(s => s.ProgramWorkout)
                .WithMany()
                .HasForeignKey(s => s.ProgramWorkoutId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add indexes for program queries
            modelBuilder.Entity<Models.Program>()
                .HasIndex(p => new { p.UserId, p.IsActive, p.IsCompleted });

            // Add index for program workout queries (non-unique to allow multiple workouts per day)
            modelBuilder.Entity<ProgramWorkout>()
                .HasIndex(pw => new { pw.ProgramId, pw.WeekNumber, pw.DayNumber });

            // ============ NUTRITION TRACKING CONFIGURATIONS ============

            // Configure FoodTemplate-User relationship (for custom templates)
            modelBuilder.Entity<FoodTemplate>()
                .HasOne(ft => ft.CreatedByUser)
                .WithMany()
                .HasForeignKey(ft => ft.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Add index for food template queries
            modelBuilder.Entity<FoodTemplate>()
                .HasIndex(ft => new { ft.Category, ft.IsCustom });

            modelBuilder.Entity<FoodTemplate>()
                .HasIndex(ft => ft.Barcode);

            modelBuilder.Entity<FoodTemplate>()
                .HasIndex(ft => ft.Name);

            // Configure MealLog-User relationship
            modelBuilder.Entity<MealLog>()
                .HasOne(ml => ml.User)
                .WithMany()
                .HasForeignKey(ml => ml.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add unique index for one meal log per user per date
            modelBuilder.Entity<MealLog>()
                .HasIndex(ml => new { ml.UserId, ml.Date })
                .IsUnique();

            // Configure MealEntry-MealLog relationship
            modelBuilder.Entity<MealEntry>()
                .HasOne(me => me.MealLog)
                .WithMany(ml => ml.MealEntries)
                .HasForeignKey(me => me.MealLogId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add index for meal entry queries
            modelBuilder.Entity<MealEntry>()
                .HasIndex(me => new { me.MealLogId, me.MealType });

            // Configure FoodItem-MealEntry relationship
            modelBuilder.Entity<FoodItem>()
                .HasOne(fi => fi.MealEntry)
                .WithMany(me => me.FoodItems)
                .HasForeignKey(fi => fi.MealEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure FoodItem-FoodTemplate relationship
            modelBuilder.Entity<FoodItem>()
                .HasOne(fi => fi.FoodTemplate)
                .WithMany()
                .HasForeignKey(fi => fi.FoodTemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure NutritionGoal-User relationship
            modelBuilder.Entity<NutritionGoal>()
                .HasOne(ng => ng.User)
                .WithMany()
                .HasForeignKey(ng => ng.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add index for nutrition goal queries
            modelBuilder.Entity<NutritionGoal>()
                .HasIndex(ng => new { ng.UserId, ng.IsActive });

            // Configure MealPlan-User relationship
            modelBuilder.Entity<MealPlan>()
                .HasOne(mp => mp.User)
                .WithMany()
                .HasForeignKey(mp => mp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add index for meal plan queries
            modelBuilder.Entity<MealPlan>()
                .HasIndex(mp => new { mp.UserId, mp.IsActive });

            // Configure MealPlanDay-MealPlan relationship
            modelBuilder.Entity<MealPlanDay>()
                .HasOne(mpd => mpd.MealPlan)
                .WithMany(mp => mp.Days)
                .HasForeignKey(mpd => mpd.MealPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add index for meal plan day queries
            modelBuilder.Entity<MealPlanDay>()
                .HasIndex(mpd => new { mpd.MealPlanId, mpd.DayNumber });

            // Configure MealPlanMeal-MealPlanDay relationship
            modelBuilder.Entity<MealPlanMeal>()
                .HasOne(mpm => mpm.MealPlanDay)
                .WithMany(mpd => mpd.Meals)
                .HasForeignKey(mpm => mpm.MealPlanDayId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add index for meal plan meal queries
            modelBuilder.Entity<MealPlanMeal>()
                .HasIndex(mpm => new { mpm.MealPlanDayId, mpm.MealType });

            // Configure MealPlanFoodItem-MealPlanMeal relationship
            modelBuilder.Entity<MealPlanFoodItem>()
                .HasOne(mpfi => mpfi.MealPlanMeal)
                .WithMany(mpm => mpm.FoodItems)
                .HasForeignKey(mpfi => mpfi.MealPlanMealId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure MealPlanFoodItem-FoodTemplate relationship
            modelBuilder.Entity<MealPlanFoodItem>()
                .HasOne(mpfi => mpfi.FoodTemplate)
                .WithMany()
                .HasForeignKey(mpfi => mpfi.FoodTemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            // ============ FRIENDS & MESSAGING CONFIGURATIONS ============

            // Add unique index on Username
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Configure Friendship relationships
            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.Requester)
                .WithMany()
                .HasForeignKey(f => f.RequesterId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.Addressee)
                .WithMany()
                .HasForeignKey(f => f.AddresseeId)
                .OnDelete(DeleteBehavior.NoAction);

            // Add unique index to prevent duplicate friend requests
            modelBuilder.Entity<Friendship>()
                .HasIndex(f => new { f.RequesterId, f.AddresseeId })
                .IsUnique();

            // Add index for friendship queries
            modelBuilder.Entity<Friendship>()
                .HasIndex(f => new { f.AddresseeId, f.Status });

            // Configure DirectMessage relationships
            modelBuilder.Entity<DirectMessage>()
                .HasOne(dm => dm.Sender)
                .WithMany()
                .HasForeignKey(dm => dm.SenderId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DirectMessage>()
                .HasOne(dm => dm.Receiver)
                .WithMany()
                .HasForeignKey(dm => dm.ReceiverId)
                .OnDelete(DeleteBehavior.NoAction);

            // Add index for message queries
            modelBuilder.Entity<DirectMessage>()
                .HasIndex(dm => new { dm.SenderId, dm.ReceiverId, dm.SentAt });

            // Configure DMConversation relationships
            modelBuilder.Entity<DMConversation>()
                .HasOne(c => c.User1)
                .WithMany()
                .HasForeignKey(c => c.User1Id)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DMConversation>()
                .HasOne(c => c.User2)
                .WithMany()
                .HasForeignKey(c => c.User2Id)
                .OnDelete(DeleteBehavior.NoAction);

            // Add unique index for conversation pair
            modelBuilder.Entity<DMConversation>()
                .HasIndex(c => new { c.User1Id, c.User2Id })
                .IsUnique();
        }
    }
}
