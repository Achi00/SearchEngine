using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Search.Application.Interfaces.ML.BackgroundRemoval;
using Search.Application.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BackgroundRemoval
{
    public class BGRemovalService : IBGRemovalService
    {
        private readonly InferenceSession _session;
        private const int ModelSize = 1024;

        public BGRemovalService(IOptions<MLOptions> options)
        {
            // gpu optimized, DirectML/CUDA
            var modelPath = Path.Combine(options.Value.ModelsPath, "BgRemoval", "model_fp16.onnx");
            var sessionOptions = new SessionOptions();
            sessionOptions.AppendExecutionProvider_DML(0);
            _session = new InferenceSession(modelPath, sessionOptions);

            foreach (var input in _session.InputMetadata)
            {
                Console.WriteLine($"Input name: {input.Key}, Shape: [{string.Join(", ", input.Value.Dimensions)}], Type: {input.Value.ElementType}");
            }

            foreach (var output in _session.OutputMetadata)
            {
                Console.WriteLine($"Output name: {output.Key}, Shape: [{string.Join(", ", output.Value.Dimensions)}], Type: {output.Value.ElementType}");
            }
        }

        public async Task<byte[]> RemoveBackgroundAsync(byte[] imageBytes, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            using var image = Image.Load<Rgba32>(imageBytes);

            var originalWidth = image.Width;
            var originalHeight = image.Height;

            ct.ThrowIfCancellationRequested();

            var inputTensor = PreprocessImage(image, ct);

            ct.ThrowIfCancellationRequested();
            // name of input extracted from model itself, should match it!!!
            using var results = _session.Run(
            [
                NamedOnnxValue.CreateFromTensor("pixel_values", inputTensor)
            ]);
            //using var results = await Task.Run(() => _session.Run([
            //    NamedOnnxValue.CreateFromTensor("pixel_values", inputTensor)
            //]), ct);

            var maskTensor = results.First().AsTensor<float>();

            ct.ThrowIfCancellationRequested();

            // reload original before preprocessing stretched it
            using var original = Image.Load<Rgba32>(imageBytes);
            original.Mutate(x => x.Resize(originalWidth, originalHeight));

            var resultImage = ApplyMask(original, maskTensor, originalWidth, originalHeight);

            using var ms = new MemoryStream();

            await resultImage.SaveAsPngAsync(ms);
            resultImage.Dispose();

            return ms.ToArray();
        }

        private static DenseTensor<float> PreprocessImage(Image<Rgba32> image, CancellationToken ct = default)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(ModelSize, ModelSize),
                Mode = ResizeMode.Pad
            }));

            var data = new float[1 * 3 * ModelSize * ModelSize];

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    // every 50 row check if cancellation requested
                    if (y % 50 == 0)
                    {
                        ct.ThrowIfCancellationRequested();
                    }

                    Span<Rgba32> row = accessor.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        int idx = y * ModelSize + x;
                        data[0 * ModelSize * ModelSize + idx] = ((row[x].R / 255f));
                        data[1 * ModelSize * ModelSize + idx] = ((row[x].G / 255f));
                        data[2 * ModelSize * ModelSize + idx] = ((row[x].B / 255f));
                    }
                }
            });

            return new DenseTensor<float>(data, new[] { 1, 3, ModelSize, ModelSize });
        }

        private static Image<Rgba32> ApplyMask(Image<Rgba32> original, Tensor<float> maskTensor, int targetWidth, int targetHeight)
        {
            using var maskImage = new Image<Rgba32>(ModelSize, ModelSize);

            maskImage.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < ModelSize; y++)
                {
                    Span<Rgba32> row = accessor.GetRowSpan(y);
                    for (int x = 0; x < ModelSize; x++)
                    {
                        byte val = (byte)(Math.Clamp(maskTensor[0, 0, y, x], 0f, 1f) * 255f);
                        row[x] = new Rgba32(val, val, val, 255);
                    }
                }
            });

            original.Mutate(x => x.Resize(targetWidth, targetHeight));
            maskImage.Mutate(x => x.Resize(targetWidth, targetHeight));

            // create a fresh RGBA image to write results into
            var result = new Image<Rgba32>(targetWidth, targetHeight);

            result.ProcessPixelRows(original, maskImage, (resultAcc, origAcc, maskAcc) =>
            {
                for (int y = 0; y < resultAcc.Height; y++)
                {
                    Span<Rgba32> resultRow = resultAcc.GetRowSpan(y);
                    Span<Rgba32> origRow = origAcc.GetRowSpan(y);
                    Span<Rgba32> maskRow = maskAcc.GetRowSpan(y);

                    for (int x = 0; x < resultRow.Length; x++)
                    {
                        resultRow[x] = maskRow[x].R > 128
                            ? new Rgba32(origRow[x].R, origRow[x].G, origRow[x].B, 255)
                            : new Rgba32(0, 0, 0, 0);
                    }
                }
            });

            return result;
        }
    }
}
