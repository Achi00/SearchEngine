using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Search.Application.Interfaces;
using Search.Application.Interfaces.Repositories;
using Search.Application.Interfaces.Setup;
using Search.Application.Services.Setup;
using Search.Infrastructure.Dataset;
using Search.Infrastructure.Dataset.Reader;
using Search.Infrastructure.Repositories;
using Search.Persistance;
using Search.Persistance.Context;

//Run.RunBenchmark();

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<DatasetOptions>(context.Configuration.GetSection(DatasetOptions.SectionName));

        services.AddDbContext<SearchDbContext>(opts =>
            opts.UseSqlServer(context.Configuration.GetConnectionString(nameof(ConnectionString.Default))));

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductSeeder, ProductSeeder>();
        services.AddScoped<IProductRepository, ProductRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddHttpClient();
        services.AddTransient<DatasetLoader>();
        services.AddScoped<ParquetFileReader>();

    })
    .Build();

var loader = host.Services.GetRequiredService<DatasetLoader>();

var results = await loader.LoadDatasetAsync();
Console.WriteLine($"Downloaded: {results.Downloaded}, Skipped: {results.Skipped}, Failed: {results.Failed.Count}");

// IsSuccess, determined by failed count
if (results.IsSuccess)
{
    using var scope = host.Services.CreateScope();
    // transform .parquet and seed data into sql database
    var reader = scope.ServiceProvider.GetRequiredService<ParquetFileReader>();
    await reader.ReadFiles();
}