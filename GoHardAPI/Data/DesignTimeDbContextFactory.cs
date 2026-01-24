using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GoHardAPI.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TrainingContext>
    {
        public TrainingContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TrainingContext>();

            // Check for DATABASE_URL (PostgreSQL on Railway) or use SQL Server for local
            var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

            if (!string.IsNullOrEmpty(databaseUrl))
            {
                // Parse PostgreSQL connection string from DATABASE_URL
                var uri = new Uri(databaseUrl);
                var userInfo = uri.UserInfo.Split(':');
                var connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
                optionsBuilder.UseNpgsql(connectionString);
            }
            else
            {
                // Use SQL Server for local development migrations
                optionsBuilder.UseSqlServer(
                    "Server=MSI\\MSSQLSERVER01;Database=TrainingAppDb;Trusted_Connection=True;TrustServerCertificate=True");
            }

            return new TrainingContext(optionsBuilder.Options);
        }
    }
}
