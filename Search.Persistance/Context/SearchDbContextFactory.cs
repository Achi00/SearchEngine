using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Search.Persistance.Context
{
    public class SearchDbContextFactory : IDesignTimeDbContextFactory<SearchDbContext>
    {
        public SearchDbContext CreateDbContext(string[] args)
        {
            // walk up from Persistence bin folder to solution root, then into Search.Setup
            var basePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "../Search.Setup");

            // fallback: if that doesn't exist, try the absolute path
            if (!Directory.Exists(basePath))
            {
                basePath = Directory.GetCurrentDirectory();
            }

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Search.Setup"))
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var connectionString = configuration.GetConnectionString(nameof(ConnectionString.Default));

            Console.WriteLine($"Using basePath: {Path.GetFullPath(basePath)}");
            Console.WriteLine($"ConnectionString: {connectionString}");

            var optionsBuilder = new DbContextOptionsBuilder<SearchDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new SearchDbContext(optionsBuilder.Options);
        }
    }
}
