using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GoHardAPI.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TrainingContext>
    {
        public TrainingContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TrainingContext>();

            // Use SQL Server connection string for local development
            optionsBuilder.UseSqlServer(
                "Server=MSI\\MSSQLSERVER01;Database=TrainingAppDb;Trusted_Connection=True;TrustServerCertificate=True");

            return new TrainingContext(optionsBuilder.Options);
        }
    }
}
