
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Notification.Infrastructure.Persistence;


namespace Infrastructure.DesignTime
{

    public class AppDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
    {
        public NotificationDbContext CreateDbContext(string[] args)
        {
           
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Api");

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var builder = new DbContextOptionsBuilder<NotificationDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            builder.UseNpgsql(
               connectionString,
               b => b.MigrationsAssembly(typeof(NotificationDbContext).Assembly.FullName)
           );

            return new NotificationDbContext(builder.Options, TimeProvider.System);
        }
    }
}
