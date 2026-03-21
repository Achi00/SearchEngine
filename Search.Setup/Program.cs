using Embedding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Qdrant.Client;
using Search.Application.Interfaces;
using Search.Application.Interfaces.ML;
using Search.Application.Interfaces.Qdrant;
using Search.Application.Interfaces.Repositories;
using Search.Application.Interfaces.Setup;
using Search.Application.Options;
using Search.Application.Services.Setup;
using Search.Infrastructure.Dataset;
using Search.Infrastructure.Dataset.Reader;
using Search.Infrastructure.ML;
using Search.Infrastructure.Qdrant;
using Search.Infrastructure.Repositories;
using Search.Persistance;
using Search.Persistance.Context;

//Run.RunBenchmark();

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<DatasetOptions>(context.Configuration.GetSection(DatasetOptions.SectionName));
        services.Configure<MLOptions>(context.Configuration.GetSection(MLOptions.SectionName));

        services.AddDbContext<SearchDbContext>(opts =>
            opts.UseSqlServer(context.Configuration.GetConnectionString(nameof(ConnectionString.Default))));

        services.Configure<DatasetOptions>(context.Configuration.GetSection(DatasetOptions.SectionName));

        services.AddScoped<ITextEmbeddingService, TextEmbeddingService>();
        services.AddScoped<IImageEmbeddingService, ImageEmbeddingService>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductSeeder, ProductSeeder>();
        services.AddScoped<IProductRepository, ProductRepository>();

        services.AddScoped<IEmbeddingPipeline, EmbeddingPipeline>();
        services.AddScoped<IQdrantService, QdrantServices>();

        services.AddSingleton<QdrantClient>(sp => new QdrantClient("localhost", 6334));
        services.AddTransient<QdrantSetup>();

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
    // create collections in Qdrant
    var qdrantSetup = scope.ServiceProvider.GetRequiredService<QdrantSetup>();
    await qdrantSetup.InitializeAsync();

    // embedding pipeline
    var pipeline = scope.ServiceProvider.GetRequiredService<IEmbeddingPipeline>();
    await pipeline.RunAsync();

    // only seed if Products table is empty
    var dbContext = scope.ServiceProvider.GetRequiredService<SearchDbContext>();
    var hasProducts = await dbContext.Products.AnyAsync();
    if (!hasProducts)
    {
        // read .parquet and seed data into sql database
        var reader = scope.ServiceProvider.GetRequiredService<ParquetFileReader>();
        await reader.ReadFiles();
    }
    else
    {
        Console.WriteLine("Database already seeded, skipping.");
    }
}