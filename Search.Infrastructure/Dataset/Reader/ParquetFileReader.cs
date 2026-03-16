using Microsoft.Extensions.Options;
using Search.Domain.Enum;
using System.Reflection;

namespace Search.Infrastructure.Dataset.Reader
{
    public class ParquetFileReader
    {
        private readonly DatasetOptions _options;

        public ParquetFileReader(IOptions<DatasetOptions> options)
        {
            _options = options.Value;
        }

        public async Task ReadFiles()
        {
            string datasetDir = _options.DatasetPath;

            if (!Directory.Exists(datasetDir))
            {
                throw new DirectoryNotFoundException($"Dataset directory not found: {datasetDir}");
            }

            var parquetFiles = Directory.GetFiles(datasetDir, "*.parquet");

            Console.WriteLine($"Found {parquetFiles.Length} parquet files.");

            foreach (var file in parquetFiles)
            {
                Console.WriteLine(file);
            }
        }
    }
}
