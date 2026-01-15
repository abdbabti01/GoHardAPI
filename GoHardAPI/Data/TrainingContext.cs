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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure DateTime to always use UTC for PostgreSQL compatibility
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(
                            new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                                v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
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
        }
    }
}
