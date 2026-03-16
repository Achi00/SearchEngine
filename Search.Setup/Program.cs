using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Search.Infrastructure.Dataset;
using Search.Infrastructure.Dataset.Reader;

//Run.RunBenchmark();

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<DatasetOptions>(context.Configuration.GetSection(DatasetOptions.SectionName));
        services.AddHttpClient();
        services.AddTransient<DatasetLoader>();
        services.AddTransient<ParquetFileReader>();
    })
    .Build();

var loader = host.Services.GetRequiredService<DatasetLoader>();

var results = await loader.LoadDatasetAsync();
Console.WriteLine($"Downloaded: {results.Downloaded}, Skipped: {results.Skipped}, Failed: {results.Failed.Count}");

// IsSuccess, determined by failed count
if (results.IsSuccess)
{
    // transform .parquet and seed data into sql database
    var reader = host.Services.GetRequiredService<ParquetFileReader>();
    await reader.ReadFiles();
}