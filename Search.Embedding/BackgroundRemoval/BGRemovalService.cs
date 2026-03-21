
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Search.Application.Options;

namespace BackgroundRemoval
{
    public class BGRemovalService
    {
        private readonly InferenceSession _session;

        public BGRemovalService(IOptions<MLOptions> options)
        {
            var modelPath = Path.Combine(options.Value.ModelsPath, "vision_model.onnx");

            var sessionOptions = new SessionOptions();
            sessionOptions.AppendExecutionProvider_DML(0);
            _session = new InferenceSession(modelPath, sessionOptions);
        }
    }
}
