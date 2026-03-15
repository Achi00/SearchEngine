using Apache.Arrow;
using Apache.Arrow.Ipc;
using System.Reflection;
using System.Text.Json;

namespace Search.Infrastructure.Dataset
{
    public static class ArrowFileConverter
    {
        public static async Task FileReader()
        {
            // get base path from assembly
            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            // .arrow files should live in both disk and bin directory
            string datasetDir = Path.Combine(assemblyDir, "Dataset");

            var arrowFiles = Directory.GetFiles(datasetDir, "*.arrow");

            int fileCount = 1;

            foreach (var item in arrowFiles)
            {
                await ConvertArrowToJson(item, datasetDir, fileCount++);
            }

            // validate if .arrow and .json files row count matches
            for (int i = 1; i < 5; i++)
            {
                var path = Path.Combine(datasetDir, $"data-{i}.json");
                var json = await File.ReadAllTextAsync(path);
                var doc = JsonDocument.Parse(json);

                Console.WriteLine($"Json rows: {doc.RootElement.GetArrayLength()}");
            }
        }

        public static async Task ConvertArrowToJson(string arrowPath, string jsonPath, int fileCount)
        {
            await using var stream = File.OpenRead(arrowPath);

            using var reader = new ArrowStreamReader(stream);

            var schema = reader.Schema;

            var outputPath = Path.Combine(jsonPath, $"data-{fileCount}.json");

            await using var jsonStream = File.Create(outputPath);
            using var writer = new Utf8JsonWriter(jsonStream);

            writer.WriteStartArray();

            RecordBatch batch;

            long totalRows = 0;

            while ((batch = await reader.ReadNextRecordBatchAsync()) != null)
            {
                totalRows += batch.Length;
                for (int row = 0; row < batch.Length; row++)
                {
                    writer.WriteStartObject();

                    for (int col = 0; col < batch.ColumnCount; col++)
                    {
                        var field = schema.GetFieldByIndex(col);
                        var array = batch.Column(col);

                        writer.WritePropertyName(field.Name);

                        WriteValue(writer, array, row);
                    }

                    writer.WriteEndObject();
                }
            }

            Console.WriteLine($"Arrow rows: {totalRows}");

            writer.WriteEndArray();
            await writer.FlushAsync();
        }

        private static void WriteValue(Utf8JsonWriter writer, IArrowArray array, int index)
        {
            if (array.IsNull(index))
            {
                writer.WriteNullValue();
                return;
            }

            switch (array)
            {
                case StringArray str:
                    writer.WriteStringValue(str.GetString(index));
                    break;

                case Int32Array i32:
                    writer.WriteNumberValue(i32.GetValue(index) ?? 0);
                    break;

                case Int64Array i64:
                    writer.WriteNumberValue(i64.GetValue(index) ?? 0);
                    break;

                case FloatArray f:
                    writer.WriteNumberValue(f.GetValue(index) ?? 0);
                    break;

                case DoubleArray d:
                    writer.WriteNumberValue(d.GetValue(index) ?? 0);
                    break;

                case BooleanArray b:
                    writer.WriteBooleanValue(b.GetValue(index) ?? false);
                    break;

                case BinaryArray bin:
                    writer.WriteStringValue(
                        Convert.ToBase64String(bin.GetBytes(index).ToArray())
                    );
                    break;

                default:
                    writer.WriteStringValue("unsupported_type");
                    break;
            }
        }
    }
}
