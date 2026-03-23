using Helpers;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;
using Search.Application.Interfaces.ML.Embeddings;
using Search.Application.Options;

namespace Embedding
{
    /* 
     * To work this project should include vocab.json and merges.txt files, those are CLIP tokenizer files
     * Vocab.json - https://huggingface.co/openai/clip-vit-large-patch14/resolve/main/vocab.json
     * Merges.txt - https://huggingface.co/openai/clip-vit-large-patch14/resolve/main/merges.txt
     * Repository - https://huggingface.co/openai/clip-vit-large-patch14/tree/main
     * Should be in same directory as models
     * powershell commands:
     * wget https://huggingface.co/openai/clip-vit-large-patch14/resolve/main/merges.txt -OutFile merges.txt
     * wget https://huggingface.co/openai/clip-vit-large-patch14/resolve/main/vocab.json -OutFile vocab.json
     */
    public class TextEmbeddingService : ITextEmbeddingService, IDisposable
    {
        private readonly InferenceSession _session;
        private readonly Tokenizer _tokenizer;
        private const int MaxTokenLength = 77;
        private const int SotToken = 49406; // <|startoftext|>
        private const int EotToken = 49407; // <|endoftext|>

        private readonly SemaphoreSlim _lock = new(1, 1);

        public TextEmbeddingService(IOptions<MLOptions> options)
        {
            var modelPath = Path.Combine(options.Value.ModelsPath, "Embeddings", "text_model.onnx");
            var tokenizerPath = Path.Combine(options.Value.ModelsPath, "Embeddings", "vocab.json");
            var mergesPath = Path.Combine(options.Value.ModelsPath, "Embeddings", "merges.txt");

            var sessionOptions = new SessionOptions();
            sessionOptions.AppendExecutionProvider_DML(0);
            _session = new InferenceSession(modelPath, sessionOptions);

            _tokenizer = BpeTokenizer.Create(tokenizerPath, mergesPath);
        }

        public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
        {
            // tokenize
            var tokens = _tokenizer.EncodeToIds(text.ToLower());

            // build input: [SOT, ...tokens..., EOT] padded to MaxTokenLength
            var inputIds = new long[MaxTokenLength];
            inputIds[0] = SotToken;

            int copyCount = Math.Min(tokens.Count, MaxTokenLength - 2);
            for (int i = 0; i < copyCount; i++)
                inputIds[i + 1] = tokens[i];

            inputIds[copyCount + 1] = EotToken;

            // remaining positions stay 0 (padding)

            var tensor = new DenseTensor<long>(inputIds, new[] { 1, MaxTokenLength });

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", tensor)
            };

            await _lock.WaitAsync(ct);

            try
            {
                using var results = _session.Run(inputs);
                var embedding = results.First(r => r.Name == "text_embeds").AsEnumerable<float>().ToArray();
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
    }
}
