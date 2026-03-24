using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;


namespace Search.ML.TextExtraction.FlorenceHelpers
{
    public class FlorenceImagePreprocessor
    {
        private static readonly float[] Mean = [0.485f, 0.456f, 0.406f];
        private static readonly float[] Std = [0.229f, 0.224f, 0.225f];

        public static DenseTensor<float> PreprocessImage(Image<Rgba32> image, int imageSize)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(imageSize, imageSize),
                Mode = ResizeMode.Stretch
            }));

            var data = new float[1 * 3 * imageSize * imageSize];

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> row = accessor.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        int idx = y * imageSize + x;
                        data[0 * imageSize * imageSize + idx] = (row[x].R / 255f - Mean[0]) / Std[0];
                        data[1 * imageSize * imageSize + idx] = (row[x].G / 255f - Mean[1]) / Std[1];
                        data[2 * imageSize * imageSize + idx] = (row[x].B / 255f - Mean[2]) / Std[2];
                    }
                }
            });

            return new DenseTensor<float>(data, [1, 3, imageSize, imageSize]);
        }
    }
}
