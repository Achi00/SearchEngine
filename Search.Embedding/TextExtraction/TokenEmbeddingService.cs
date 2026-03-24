using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using TextExtraction.FlorenceHelpers;
using Tokenizers.HuggingFace.Tokenizer;

namespace TextExtraction
{
    public class TokenEmbeddingService
    {
        private readonly InferenceSession _embedTokens;
        private readonly Tokenizer _tokenizer;

        public TokenEmbeddingService(FlorenceModelProvider models, Tokenizer tokenizer)
        {
            _embedTokens = models._embedTokens;
            _tokenizer = tokenizer;
        }

        public string Decode(long[] tokenIds)
        {
            // filter out special tokens before decoding
            var filtered = tokenIds
                // skip <s>, <pad>, </s>
                .Where(id => id != 0 && id != 1 && id != 2)  
                .Select(t => (uint)t);
            return _tokenizer.Decode(filtered, skipSpecialTokens: true);
        }

        public DenseTensor<float> EmbedPrompt(string promptText)
        {
            var ids = _tokenizer.Encode(promptText, addSpecialTokens: true)
                       .First().Ids;

            var inputIds = new DenseTensor<long>(new[] { 1, ids.Count });
            for (int i = 0; i < ids.Count; i++)
                inputIds[0, i] = ids[i];

            using var results = _embedTokens.Run([
                NamedOnnxValue.CreateFromTensor("input_ids", inputIds)
            ]);

            var idss = _tokenizer.Encode("<OCR_WITH_REGION>", addSpecialTokens: true).First().Ids;
            Console.WriteLine($"Task token ids: [{string.Join(", ", idss)}]");
            Console.WriteLine($"Count: {idss.Count}");

            return (DenseTensor<float>)results.First().AsTensor<float>().Clone();
        }

        public DenseTensor<float> EmbedPromptFromIds(long[] tokenIds)
        {
            var inputIds = new DenseTensor<long>(new[] { 1, tokenIds.Length });
            for (int i = 0; i < tokenIds.Length; i++)
                inputIds[0, i] = tokenIds[i];

            using var results = _embedTokens.Run([
                NamedOnnxValue.CreateFromTensor("input_ids", inputIds)
            ]);
            return (DenseTensor<float>)results.First().AsTensor<float>().Clone();
        }

        public DenseTensor<float> EmbedTokenId(long tokenId)
        {
            var inputIds = new DenseTensor<long>(new long[] { tokenId }, [1, 1]);
            using var results = _embedTokens.Run([
                NamedOnnxValue.CreateFromTensor("input_ids", inputIds)
            ]);
            return (DenseTensor<float>)results.First().AsTensor<float>().Clone();
        }
    }
}
