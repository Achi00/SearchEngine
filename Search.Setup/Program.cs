using Embedding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.ML.OnnxRuntime;
using Qdrant.Client;
using Search.Application.Interfaces;
using Search.Application.Interfaces.ML;
using Search.Application.Interfaces.Repositories;
using Search.Application.Interfaces.Setup;
using Search.Application.Options;
using Search.Application.Services.Setup;
using Search.Infrastructure.Dataset;
using Search.Infrastructure.Dataset.Reader;
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

    // Temp: text embeddings
    var textEmbedder = scope.ServiceProvider.GetRequiredService<ITextEmbeddingService>();
    var embedding = await textEmbedder.EmbedAsync("red electric guitar");

    Console.WriteLine($"Embedding dims: {embedding.Length}");
    Console.WriteLine($"First 5 values: {string.Join(", ", embedding.Take(5))}");
    Console.WriteLine($"Magnitude: {MathF.Sqrt(embedding.Sum(x => x * x))}");

    // Temp: image embeddings
    var imageEmbedder = scope.ServiceProvider.GetRequiredService<IImageEmbeddingService>();
    var httpClient = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
    var imageBytes = await httpClient.GetByteArrayAsync("https://m.media-amazon.com/images/I/71m-Vs8ULhL._AC_UL1500_.jpg");

    var imageEmbedding = await imageEmbedder.EmbedAsync(imageBytes);

    Console.WriteLine($"Image embedding dims: {imageEmbedding.Length}");
    Console.WriteLine($"First 5 values: {string.Join(", ", imageEmbedding.Take(5))}");
    Console.WriteLine($"Magnitude: {MathF.Sqrt(imageEmbedding.Sum(x => x * x))}");

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