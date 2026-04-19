using BackgroundRemoval;
using Embedding;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Search.Application.Interfaces;
using Search.Application.Interfaces.ImageSearch;
using Search.Application.Interfaces.ML.BackgroundRemoval;
using Search.Application.Interfaces.ML.Embeddings;
using Search.Application.Interfaces.Qdrant;
using Search.Application.Interfaces.Repositories;
using Search.Application.Interfaces.Setup;
using Search.Application.Options;
using Search.Application.Services.ImageServices;
using Search.Application.Services.Setup;
using Search.Infrastructure.Dataset;
using Search.Infrastructure.Dataset.Reader;
using Search.Infrastructure.ML;
using Search.Infrastructure.Qdrant;
using Search.Infrastructure.Repositories;
using Search.ML.TextExtraction.FlorenceHelpers;
using Search.Persistance;
using Search.Persistance.Context;
using TextExtraction;
using TextExtraction.FlorenceHelpers;


var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<DatasetOptions>(context.Configuration.GetSection(DatasetOptions.SectionName));
        services.Configure<MLOptions>(context.Configuration.GetSection(MLOptions.SectionName));

        services.AddDbContext<SearchDbContext>(opts =>
            opts.UseSqlServer(context.Configuration.GetConnectionString(nameof(ConnectionString.Default))));

        services.Configure<DatasetOptions>(context.Configuration.GetSection(DatasetOptions.SectionName));

        services.AddSingleton<ITextEmbeddingService, TextEmbeddingService>();
        services.AddSingleton<IImageEmbeddingService, ImageEmbeddingService>();
        services.AddSingleton<IBGRemovalService, BGRemovalService>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductSeeder, ProductSeeder>();
        services.AddScoped<IProductRepository, ProductRepository>();

        services.AddScoped<IEmbeddingPipeline, EmbeddingPipeline>();
        services.AddScoped<IQdrantService, QdrantServices>();

        // text extraction
        // use huggingface tokenizer not Microsoft.ML tokenizer
        services.AddSingleton<Tokenizers.HuggingFace.Tokenizer.Tokenizer>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MLOptions>>();
            var path = Path.Combine(options.Value.ModelsPath, "TextExtraction", "tokenizer.json");
            return Tokenizers.HuggingFace.Tokenizer.Tokenizer.FromFile(path);
        });

        services.AddSingleton<TextExtractionService>();
        services.AddSingleton<TokenEmbeddingService>();
        services.AddSingleton<FlorenceModelProvider>();
        services.AddSingleton<FlorenceDecoder>();
        //services.AddSingleton<Tokenizer>();
        services.AddSingleton<FlorenceImagePreprocessor>();
        //services.AddSingleton<InferenceSession>();

        services.AddScoped<ISearch, SearchService>();

        services.AddSingleton<QdrantClient>(sp => new QdrantClient("localhost", 6334));
        services.AddTransient<QdrantSetup>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddHttpClient();
        services.AddTransient<DatasetLoader>();
        services.AddScoped<ParquetFileReader>();

        // mapster
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(typeof(Search.Application.Mapping.SearchMapping).Assembly);
        services.AddSingleton(config);

    })
    .Build();

var loader = host.Services.GetRequiredService<DatasetLoader>();

var results = await loader.LoadDatasetAsync();
Console.WriteLine($"Downloaded: {results.Downloaded}, Skipped: {results.Skipped}, Failed: {results.Failed.Count}");

// remove image background
//using var scope = host.Services.CreateScope();

//// search by uploaded image
//var imageSearch = scope.ServiceProvider.GetRequiredService<ISearch>();

//var imageBytes = File.ReadAllBytes("C:\\Users\\Achi\\Desktop\\719VuO+vHOL._AC_SL1500_.jpg");

//var result = await imageSearch.SearchByImageAsync(imageBytes);

//foreach (var item in result)
//{
//    Console.WriteLine(item.Asin);
//    Console.WriteLine(item.ImageUrl);
//    Console.WriteLine(item.Score);
//}

//var bgRemove = scope.ServiceProvider.GetRequiredService<IBGRemovalService>();

//var imgBytes = File.ReadAllBytes("C:\\Users\\Achi\\Desktop\\test.png");
//Console.WriteLine("Start image background removal");
//var res = await bgRemove.RemoveBackgroundAsync(imgBytes);
////File.WriteAllBytes("C:\\Users\\Achi\\Desktop\\output.png", res);
//Console.WriteLine(res.Length);
//Console.WriteLine("Image saved");

// IsSuccess, determined by failed count
//if (results.IsSuccess)
//{
//    using var scope = host.Services.CreateScope();
//    // create collections in Qdrant
//    var qdrantSetup = scope.ServiceProvider.GetRequiredService<QdrantSetup>();
//    await qdrantSetup.InitializeAsync();

//    // embedding pipeline
//    var pipeline = scope.ServiceProvider.GetRequiredService<IEmbeddingPipeline>();
//    await pipeline.RunAsync();

//    // only seed if Products table is empty
//    var dbContext = scope.ServiceProvider.GetRequiredService<SearchDbContext>();
//    var hasProducts = await dbContext.Products.AnyAsync();
//    if (!hasProducts)
//    {
//        // read .parquet and seed data into sql database
//        var reader = scope.ServiceProvider.GetRequiredService<ParquetFileReader>();
//        await reader.ReadFiles();
//    }
//    else
//    {
//        Console.WriteLine("Database already seeded, skipping.");
//    }
//}