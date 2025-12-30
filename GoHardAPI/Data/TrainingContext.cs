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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
        }
    }
}
