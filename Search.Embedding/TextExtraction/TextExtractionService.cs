using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Search.Application.Options;
using Search.Domain.Entity.TextExtraction;
using Search.ML.TextExtraction.FlorenceHelpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text.RegularExpressions;
using TextExtraction.FlorenceHelpers;

namespace TextExtraction
{
    public class TextExtractionService
    {
        private readonly FlorenceImagePreprocessor _preprocessor;
        private readonly TokenEmbeddingService _embedding;
        private readonly FlorenceDecoder _decoder;

        private readonly InferenceSession _visionEncoder;
        private readonly InferenceSession _encoderModel;
        private readonly SemaphoreSlim _lock = new(1, 1);

        // Florence-2 image size is fixed
        private const int ImageSize = 768;

        private static readonly Dictionary<string, long[]> TaskTokenIds = new()
        {
            ["<OCR>"] = [0, 50267, 2], // <s> <ocr> </s>
            ["<OCR_WITH_REGION>"] = [0, 50267, 2],
            ["<CAPTION>"] = [0, 51269, 2], // <s> <cap> </s>
            ["<DETAILED_CAPTION>"] = [0, 51273, 2], // <s> <dcap> </s>
        };
        // extracting text and text regions from image
        // needs 4 session to work
        // 4 stage process
        public TextExtractionService(
            FlorenceImagePreprocessor preprocessor, 
            TokenEmbeddingService embedding, 
            FlorenceDecoder decoder, 
            FlorenceModelProvider models
        )
        {
            _preprocessor = preprocessor;
            _embedding = embedding;
            _decoder = decoder;

            _visionEncoder = models._visionEncoder;
            _encoderModel = models._encoderModel;
        }

        // Full pipeline:
        // 1. preprocess image -> pixel tensor
        // 2. vision_encoder(pixel tensor) -> image features
        // 3. tokenize task prompt -> token ids
        // 4. embed_tokens(token ids) -> text embeddings
        // 5. encoder_model(image features + text embeddings) -> encoder hidden state
        // 6. decoder_model loop -> generate output token ids
        // 7. decode token ids -> text + quad boxes

        public async Task<TextExtractionResult> ExtractTextAsync(
        byte[] imageBytes,
        string task = "<OCR_WITH_REGION>",
        CancellationToken ct = default)
        {
            await _lock.WaitAsync(ct);
            try
            {
                // 1. preprocess
                using var image = Image.Load<Rgba32>(imageBytes);
                var pixelTensor = FlorenceImagePreprocessor.PreprocessImage(image, ImageSize);

                // 2. vision encoder
                var imageFeatures = RunVisionEncoder(pixelTensor);
                Console.WriteLine($"ImageFeatures shape: [{string.Join(", ", imageFeatures.Dimensions.ToArray())}]");
                Console.WriteLine($"ImageFeatures sample values: {imageFeatures[0, 0, 0]:F4}, {imageFeatures[0, 1, 0]:F4}, {imageFeatures[0, 2, 0]:F4}");


                // 3. embed tokens
                var tokenIds = TaskTokenIds[task];
                var promptEmbeds = _embedding.EmbedPromptFromIds(tokenIds);
                Console.WriteLine($"PromptEmbeds shape: [{string.Join(", ", promptEmbeds.Dimensions.ToArray())}]");
                Console.WriteLine($"PromptEmbeds sample: {promptEmbeds[0, 0, 0]:F4}, {promptEmbeds[0, 1, 0]:F4}");


                // 4. encoder
                var (encoderHiddenState, encoderAttentionMask) =
                    RunEncoder(imageFeatures, promptEmbeds);
                Console.WriteLine($"EncoderHiddenState shape: [{string.Join(", ", encoderHiddenState.Dimensions.ToArray())}]");


                // 5. decoder
                var tokens = _decoder.Decode(
                    encoderHiddenState,
                    encoderAttentionMask,
                    ct);

                Console.WriteLine($"Output token count: {tokens.Length}");
                Console.WriteLine($"Raw token ids: [{string.Join(", ", tokens.Take(30))}]");

                // 6. parse
                return ParseOutput(tokens);
            }
            finally
            {
                _lock.Release();
            }
        }

        private DenseTensor<float> RunVisionEncoder(DenseTensor<float> pixelTensor)
        {
            using var results = _visionEncoder.Run([
                NamedOnnxValue.CreateFromTensor("pixel_values", pixelTensor)
            ]);

            return (DenseTensor<float>)results.First().AsTensor<float>().Clone();
        }

        private (DenseTensor<float>, DenseTensor<long>) RunEncoder(
            DenseTensor<float> imageFeatures,
            DenseTensor<float> promptEmbeds)
        {
            int imgLen = imageFeatures.Dimensions[1];
            int promptLen = promptEmbeds.Dimensions[1];
            int totalLen = imgLen + promptLen;

            var attentionMask = new DenseTensor<long>(new[] { 1, totalLen });

            for (int i = 0; i < totalLen; i++)
            {
                attentionMask[0, i] = 1;
            }

            var combinedEmbeds = ConcatOnSeqDim(imageFeatures, promptEmbeds);

            using var results = _encoderModel.Run([
                NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask),
        NamedOnnxValue.CreateFromTensor("inputs_embeds", combinedEmbeds)
            ]);

            var hiddenState = (DenseTensor<float>)results.First().AsTensor<float>().Clone();

            return (hiddenState, attentionMask);
        }

        private TextExtractionResult ParseOutput(long[] tokenIds)
        {
            var text = _embedding.Decode(tokenIds);

            var result = new TextExtractionResult { FullText = text };
            var regions = new List<TextRegion>();

            var pattern = @"([^<]+)(<loc_\d+>){4}";

            foreach (Match m in Regex.Matches(text, pattern))
            {
                var label = m.Groups[1].Value.Trim();
                var locs = Regex.Matches(m.Value, @"<loc_(\d+)>")
                                .Select(l => int.Parse(l.Groups[1].Value) / 1000f)
                                .ToArray();

                if (locs.Length == 4)
                    regions.Add(new TextRegion { Text = label, QuadBox = locs });
            }

            result.Regions = regions;
            return result;
        }

        private static DenseTensor<float> ConcatOnSeqDim(
            DenseTensor<float> a,
            DenseTensor<float> b)
        {
            int hidden = a.Dimensions[2];
            int seqA = a.Dimensions[1];
            int seqB = b.Dimensions[1];
            int totalSeq = seqA + seqB;

            var result = new DenseTensor<float>(new[] { 1, totalSeq, hidden });

            for (int s = 0; s < seqA; s++)
                for (int h = 0; h < hidden; h++)
                    result[0, s, h] = a[0, s, h];

            for (int s = 0; s < seqB; s++)
                for (int h = 0; h < hidden; h++)
                    result[0, seqA + s, h] = b[0, s, h];

            return result;
        }
    }
}
