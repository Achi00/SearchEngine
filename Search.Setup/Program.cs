using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Search.Infrastructure.Dataset;

//Run.RunBenchmark();

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHttpClient();
        services.AddTransient<DatasetLoader>();
    })
    .Build();

var loader = host.Services.GetRequiredService<DatasetLoader>();

var results = await loader.LoadDatasetAsync();
Console.WriteLine($"Downloaded: {results.Downloaded}, Skipped: {results.Skipped}, Failed: {results.Failed.Count}");

// IsSuccess, determined by failed count
if (results.IsSuccess)
{
    // transform .arrow and seed data into sql database
    await ArrowFileConverter.FileReader();
}