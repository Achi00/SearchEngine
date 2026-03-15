using Search.Application.Dtos.Dataset;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace Search.Infrastructure.Dataset
{
    public class DatasetLoader
    {
        private readonly IHttpClientFactory _factory;

        private static readonly string DatasetDirectory = Path.Combine(GetInfrastructureRoot(), "Dataset");

        public DatasetLoader(IHttpClientFactory factory)
        {
            _factory = factory;
        }

        // downloading .arrow files from huggingface and loading in project dir
        // parallel file download
        // Dataset: https://huggingface.co/datasets/milistu/AMAZON-Products-2023
        public async Task<DatasetLoadResultResponse> LoadDatasetAsync()
        {
            var client = _factory.CreateClient();
            var jsonFilesData = await client.GetStringAsync("https://huggingface.co/api/datasets/milistu/AMAZON-Products-2023/tree/main").ConfigureAwait(false);

            var files = JsonSerializer.Deserialize<List<HuggingFaceDatasetResponse>>(jsonFilesData);

            if (files == null)
            {
                return new DatasetLoadResultResponse();
            }
            if (!Directory.Exists(DatasetDirectory))
            {
                Directory.CreateDirectory(DatasetDirectory);
            }

            int downloaded = 0;
            int skipped = 0;

            var failed = new ConcurrentBag<string>();

            await Parallel.ForEachAsync(files.Where(f => f.path.EndsWith(".arrow")),
            new ParallelOptions { MaxDegreeOfParallelism = 4 },
            async (file, _) =>
            {
                var url = $"https://huggingface.co/datasets/milistu/AMAZON-Products-2023/resolve/main/{file.path}";

                var localPath = Path.Combine(DatasetDirectory, Path.GetFileName(file.path));
                // slip if file already in dir
                if (File.Exists(localPath))
                {
                    // ensure thread safe increment
                    Interlocked.Increment(ref skipped);
                    Console.WriteLine($"Skipping {file.path}, already exists.");
                    return;
                }
                try
                {
                    Console.WriteLine($"Download of {file.path} Started!");
                    Console.WriteLine($"File Size: {(file.size / 1024).ToString("F2")} Mb");

                    await DownloadFileAsync(url, localPath);
                    
                    Interlocked.Increment(ref downloaded);
                    Console.WriteLine($"Download of {file.path} Ended!");
                }
                catch (Exception ex)
                {
                    failed.Add(file.path);
                    Console.WriteLine($"Failed to download {file.path}: {ex.Message}");
                }
            });

            return new DatasetLoadResultResponse
            {
                Downloaded = downloaded,
                Skipped = skipped,
                Failed = [..failed]
            };
        }

        private async Task DownloadFileAsync(string url, string localPath)
        {
            var client = _factory.CreateClient();

            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;

            using var stream = await response.Content.ReadAsStreamAsync();
            using var file = File.Create(localPath);

            var buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;

            // avoid spaming download info, display in every 3 seconds
            Stopwatch sw = Stopwatch.StartNew();
            TimeSpan interval = TimeSpan.FromSeconds(3);

            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, bytesRead));

                totalRead += bytesRead;

                if (sw.Elapsed >= interval)
                {
                    if (totalBytes > 0)
                    {
                        double percent = (double)totalRead / totalBytes * 100;
                        Console.WriteLine($"{Path.GetFileName(localPath)} {percent:F2}%");
                    }
                    sw.Restart();
                }
            }

            Console.WriteLine($"{Path.GetFileName(localPath)} download completed");
        }


        private static string GetInfrastructureRoot()
        {
            var thisFilePath = GetThisFilePath();
            return Path.GetFullPath(Path.Combine(thisFilePath, "..", ".."));
        }

        private static string GetThisFilePath([System.Runtime.CompilerServices.CallerFilePath] string path = "")
        {
            return path;
        }
    }
}
