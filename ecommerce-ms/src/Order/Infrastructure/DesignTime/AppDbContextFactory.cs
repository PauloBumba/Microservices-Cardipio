
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Order.Infrastructure.Persistence;


namespace Infrastructure.DesignTime
{

    public class AppDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
    {
        public OrderDbContext CreateDbContext(string[] args)
        {
           
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Api");

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var builder = new DbContextOptionsBuilder<OrderDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            builder.UseNpgsql(
                 connectionString,
                 b => b.MigrationsAssembly(typeof(OrderDbContext).Assembly.FullName)
             );

            return new OrderDbContext(builder.Options);
        }
    }
}
