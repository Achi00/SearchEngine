using Microsoft.Extensions.Options;
using Parquet;
using Parquet.Schema;
using Search.Application.Dtos.Dataset;
using Search.Application.Interfaces.Setup;
using Search.Domain.Entity.Products;
using System.Data;
using System.Text.Json;

namespace Search.Infrastructure.Dataset.Reader
{
    public class ParquetFileReader
    {
        private readonly DatasetOptions _options;
        private readonly IProductSeeder _seeder;

        public ParquetFileReader(IOptions<DatasetOptions> options, IProductSeeder seeder)
        {
            _options = options.Value;
            _seeder = seeder;
        }

        // read parquet files and create tables
        public async Task ReadFiles()
        {
            // identify .parquet files in directory
            string datasetDir = _options.DatasetPath;

            if (!Directory.Exists(datasetDir))
            {
                throw new DirectoryNotFoundException($"Dataset directory not found: {datasetDir}");
            }

            var parquetFiles = Directory.GetFiles(datasetDir, "*.parquet");

            Console.WriteLine($"Found {parquetFiles.Length} parquet files.");

            foreach (var file in parquetFiles)
            {
                Console.WriteLine($"Processing {Path.GetFileName(file)}...");
                await ProcessFileAsync(file);
            }
        }

        private async Task ProcessFileAsync(string filePath)
        {
            await using var stream = File.OpenRead(filePath);
            using var reader = await ParquetReader.CreateAsync(stream);

            for (int i = 0; i < reader.RowGroupCount; i++)
            {
                using var groupReader = reader.OpenRowGroupReader(i);
                var schema = reader.Schema;

                // --- simple DataFields ---
                var simpleColumns = new Dictionary<string, Array>();
                foreach (var field in schema.Fields.OfType<DataField>())
                {
                    var col = await groupReader.ReadColumnAsync(field);
                    simpleColumns[field.Name] = col.Data;
                }

                // --- list fields (categories, features) ---
                var categoriesPerRow = await ReadStringListColumnAsync(groupReader, "categories", schema);
                var featuresPerRow = await ReadStringListColumnAsync(groupReader, "features", schema);

                // --- details is BYTE_ARRAY JSON: {"Brand":"Sony",...} ---
                // already in simpleColumns["details"] as raw strings

                int rowCount = simpleColumns["parent_asin"].Length;
                var batch = new List<ProductSeedDto>(rowCount);

                for (int r = 0; r < rowCount; r++)
                {
                    var product = new ProductSeedDto
                    {
                        Id = Guid.NewGuid(),
                        Asin = GetString(simpleColumns, "parent_asin", r),
                        Title = GetString(simpleColumns, "title", r),
                        Description = GetString(simpleColumns, "description", r),
                        FileName = GetString(simpleColumns, "filename", r),
                        MainCategory = GetString(simpleColumns, "main_category", r),
                        Store = GetString(simpleColumns, "store", r),
                        AverageRating = GetDouble(simpleColumns, "average_rating", r),
                        RatingNumber = (int)GetDouble(simpleColumns, "rating_number", r),
                        Price = (decimal)GetDouble(simpleColumns, "price", r),
                        Image = GetString(simpleColumns, "image", r),
                        //DateFirstAvailable = DateTimeOffset
                        //    .FromUnixTimeMilliseconds(GetLong(simpleColumns, "date_first_available", r))
                        //    .UtcDateTime,

                        Categories = (categoriesPerRow.Count > r ? categoriesPerRow[r] : [])
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList(),
                        Features = featuresPerRow.Count > r ? featuresPerRow[r] : [],
                        Details = ParseDetailsAstuples(GetString(simpleColumns, "details", r))
                    };

                    batch.Add(product);
                }

                await _seeder.BulkInsertAsync(batch);
                Console.WriteLine($"  Inserted row group {i + 1}/{reader.RowGroupCount}");
            }
        }

        // reads a ListField<string> and reconstructs per-row arrays using repetition levels
        private static async Task<List<List<string>>> ReadStringListColumnAsync(
            ParquetRowGroupReader groupReader, string fieldName, ParquetSchema schema)
        {
            var listField = schema.Fields
                .OfType<ListField>()
                .FirstOrDefault(f => f.Name == fieldName);

            if (listField == null) return [];

            var innerField = (DataField)listField.Item;
            var column = await groupReader.ReadColumnAsync(innerField);

            var result = new List<List<string>>();
            var current = new List<string>();

            for (int i = 0; i < column.Data.Length; i++)
            {
                // repetition level 0 = start of a new list (new row)
                if (i > 0 && column.RepetitionLevels?[i] == 0)
                {
                    result.Add(current);
                    current = [];
                }

                var val = column.Data.GetValue(i);
                if (val != null)
                    current.Add(val.ToString()!);
            }

            result.Add(current);
            return result;
        }

        // details comes as key value pair: {"Brand":"Sony","Color":"Black"}
        private static List<(string Key, string Value)> ParseDetailsAstuples(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return [];

            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(raw);
                return dict?
                    .Select(kv => (kv.Key, kv.Value))
                    .ToList() ?? [];
            }
            catch
            {
                return [];
            }
        }

        private static string? GetString(Dictionary<string, Array> cols, string key, int r)
            => cols.TryGetValue(key, out var arr) ? arr.GetValue(r)?.ToString() : null;

        private static double GetDouble(Dictionary<string, Array> cols, string key, int r)
            => cols.TryGetValue(key, out var arr) && arr.GetValue(r) is double d ? d : 0;

        private static long GetLong(Dictionary<string, Array> cols, string key, int r)
            => cols.TryGetValue(key, out var arr) && arr.GetValue(r) is long l ? l : 0;
    }
}
