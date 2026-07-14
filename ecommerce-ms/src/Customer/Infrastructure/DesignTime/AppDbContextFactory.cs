using Customer.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;


namespace Infrastructure.DesignTime
{

    public class AppDbContextFactory : IDesignTimeDbContextFactory<CustomerDbContext>
    {
        public CustomerDbContext CreateDbContext(string[] args)
        {
           
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Api");

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var builder = new DbContextOptionsBuilder<CustomerDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            builder.UseNpgsql(
                connectionString,
                b => b.MigrationsAssembly(typeof(CustomerDbContext).Assembly.FullName)
            );

            return new CustomerDbContext(builder.Options, TimeProvider.System);
        }
    }
}
