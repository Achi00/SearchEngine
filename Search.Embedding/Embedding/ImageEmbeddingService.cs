using Helpers;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Search.Application.Interfaces.ML;
using Search.Application.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Embedding
{
    public class ImageEmbeddingService : IImageEmbeddingService, IDisposable
    {
        private readonly InferenceSession _session;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public ImageEmbeddingService(IOptions<MLOptions> options)
        {
            var modelPath = Path.Combine(options.Value.ModelsPath, "vision_model.onnx");
            var tokenizerPath = Path.Combine(options.Value.ModelsPath, "vocab.json");
            var mergesPath = Path.Combine(options.Value.ModelsPath, "merges.txt");

            var sessionOptions = new SessionOptions();
            sessionOptions.AppendExecutionProvider_DML(0);
            _session = new InferenceSession(modelPath, sessionOptions);
        }

        public async Task<float[]> EmbedAsync(byte[] imageBytes, CancellationToken ct = default)
        {
            var tensor = PreprocessImage(imageBytes);

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("pixel_values", tensor)
            };

            await _lock.WaitAsync(ct);

            try
            {
                using var results = _session.Run(inputs);
                var embedding = results.First(r => r.Name == "image_embeds").AsEnumerable<float>().ToArray();

                return EmbeddingHelper.Normalize(embedding);
            }
            finally
            {
                _lock.Release();
            }
        }

        public void Dispose()
        {
            _session.Dispose();
            _lock.Dispose();
        }

        private static readonly float[] Mean = [0.48145466f, 0.4578275f, 0.40821073f];
        private static readonly float[] Std = [0.26862954f, 0.26130258f, 0.27577711f];

        private static DenseTensor<float> PreprocessImage(byte[] imageBytes)
        {
            using Image<Rgba32> image = Image.Load<Rgba32>(imageBytes);
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(224, 224),
                // ResizeMode.Crop maintains aspect ration, will be benefitian with CLIP model
                Mode = ResizeMode.Crop
            }));

            int width = image.Width;
            int height = image.Height;
            Console.WriteLine("width " + width);
            Console.WriteLine("height " + height);

            // normalize image - convert each pixel channel from 0-255 int to 0.0-1.0 float
            // For RGB
            var tensor = new DenseTensor<float>(new[] { 1, 3, 224, 224 });

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        // normalize 0-255 -> 0.0-1.0, then apply CLIP mean/std per channel
                        tensor[0, 0, y, x] = (pixelRow[x].R / 255f - Mean[0]) / Std[0]; // R
                        tensor[0, 1, y, x] = (pixelRow[x].G / 255f - Mean[1]) / Std[1]; // G
                        tensor[0, 2, y, x] = (pixelRow[x].B / 255f - Mean[2]) / Std[2]; // B
                    }
                }
            });

            return tensor;
        }
    }
}
