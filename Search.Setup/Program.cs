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

        services.AddScoped<ITextEmbeddingService, ClipTextEmbeddingService>();
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

    // Temp: test embeddings
    var textEmbedder = scope.ServiceProvider.GetRequiredService<ITextEmbeddingService>();
    var embedding = await textEmbedder.EmbedAsync("red electric guitar");

    Console.WriteLine($"Embedding dims: {embedding.Length}");
    Console.WriteLine($"First 5 values: {string.Join(", ", embedding.Take(5))}");
    Console.WriteLine($"Magnitude: {MathF.Sqrt(embedding.Sum(x => x * x))}");
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

// temp: test models and token files
//var textSession = new InferenceSession("D:\\Csharp\\ImageSearch\\Search.Embedding\\Models\\text_model.onnx");
//Console.WriteLine("TEXT INPUTS:");
//foreach (var input in textSession.InputMetadata)
//    Console.WriteLine($"  {input.Key} → shape: [{string.Join(",", input.Value.Dimensions)}]");
//Console.WriteLine("TEXT OUTPUTS:");
//foreach (var output in textSession.OutputMetadata)
//    Console.WriteLine($"  {output.Key}");

//var imageSession = new InferenceSession("D:\\Csharp\\ImageSearch\\Search.Embedding\\Models\\vision_model.onnx");
//Console.WriteLine("IMAGE INPUTS:");
//foreach (var input in imageSession.InputMetadata)
//    Console.WriteLine($"  {input.Key} → shape: [{string.Join(",", input.Value.Dimensions)}]");
//Console.WriteLine("IMAGE OUTPUTS:");
//foreach (var output in imageSession.OutputMetadata)
//    Console.WriteLine($"  {output.Key}");