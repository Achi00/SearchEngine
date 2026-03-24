using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Search.Application.Options;

namespace TextExtraction.FlorenceHelpers
{
    public class FlorenceModelProvider
    {
        public InferenceSession _visionEncoder { get; }
        public InferenceSession _embedTokens { get; }
        public InferenceSession _encoderModel { get; }
        public InferenceSession _decoderModel { get; }

        public FlorenceModelProvider(IOptions<MLOptions> options)
        {
            var modelsPath = Path.Combine(options.Value.ModelsPath, "TextExtraction");

            var sessionOptions = new SessionOptions();
            // TODO: go do AppendExecutionProvider_DML(0) after fix
            sessionOptions.AppendExecutionProvider_DML();

            _visionEncoder = new InferenceSession(
                Path.Combine(modelsPath, "vision_encoder_q4f16.onnx"), sessionOptions);

            _embedTokens = new InferenceSession(
                Path.Combine(modelsPath, "embed_tokens_fp16.onnx"), sessionOptions);

            _encoderModel = new InferenceSession(
                Path.Combine(modelsPath, "encoder_model_q4f16.onnx"), sessionOptions);

            _decoderModel = new InferenceSession(
                Path.Combine(modelsPath, "decoder_model_merged.onnx"), sessionOptions);
        }
    }
}
