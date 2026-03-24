using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using TextExtraction;
using TextExtraction.FlorenceHelpers;

namespace Search.ML.TextExtraction.FlorenceHelpers
{
    public class FlorenceDecoder
    {
        private const int NumLayers = 6;
        private const int NumHeads = 12;
        private const int HeadDim = 64;
        private const long DecoderStartTokenId = 2;
        private const int MaxNewTokens = 1024;
        private const long EosTokenId = 2;

        private readonly InferenceSession _decoderModel;
        private readonly TokenEmbeddingService _embedding;

        public FlorenceDecoder(FlorenceModelProvider models, TokenEmbeddingService embedding)
        {
            _decoderModel = models._decoderModel;
            _embedding = embedding;
        }

        public long[] Decode(
            DenseTensor<float> encoderHiddenState,
            DenseTensor<long> encoderAttentionMask,
            CancellationToken ct)
        {
            return RunDecoderLoop(encoderHiddenState, encoderAttentionMask, ct);
        }

        private long[] RunDecoderLoop(DenseTensor<float> encoderHiddenState, DenseTensor<long> encoderAttentionMask, CancellationToken ct)
        {
            var generatedTokens = new List<long>();

            // initialize empty KV cache [1, 12, 0, 64]
            var pastKV = new Dictionary<string, DenseTensor<float>>();
            foreach (var layer in Enumerable.Range(0, NumLayers))
                foreach (var role in new[] { "decoder", "encoder" })
                    foreach (var kv in new[] { "key", "value" })
                        pastKV[$"past_key_values.{layer}.{role}.{kv}"] =
                            new DenseTensor<float>([1, NumHeads, 0, HeadDim]);

            var currentEmbeds = _embedding.EmbedTokenId(DecoderStartTokenId);
            bool isFirstStep = true;

            for (int step = 0; step < MaxNewTokens; step++)
            {
                ct.ThrowIfCancellationRequested();

                var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("encoder_attention_mask", encoderAttentionMask),
                NamedOnnxValue.CreateFromTensor("encoder_hidden_states",  encoderHiddenState),
                NamedOnnxValue.CreateFromTensor("inputs_embeds",          currentEmbeds),
                NamedOnnxValue.CreateFromTensor("use_cache_branch",
                    new DenseTensor<bool>(new[] { !isFirstStep }, [1]))
            };

                foreach (var kvp in pastKV)
                    inputs.Add(NamedOnnxValue.CreateFromTensor(kvp.Key, kvp.Value));

                using var results = _decoderModel.Run(inputs);
                var resultList = results.ToList();

                // logits -> [1, seq, 51289] -> greedy argmax on last token
                var logits = resultList.First(r => r.Name == "logits").AsTensor<float>();
                int lastSeqIdx = logits.Dimensions[1] - 1;
                long nextToken = ArgMax(logits, lastSeqIdx);

                if (nextToken == EosTokenId)
                {
                    break;
                }

                generatedTokens.Add(nextToken);

                // update KV cache from present outputs
                for (int layer = 0; layer < NumLayers; layer++)
                    foreach (var role in new[] { "decoder", "encoder" })
                        foreach (var kv in new[] { "key", "value" })
                        {
                            var presentName = $"present.{layer}.{role}.{kv}";
                            var pastName = $"past_key_values.{layer}.{role}.{kv}";
                            pastKV[pastName] = (DenseTensor<float>)resultList
                                .First(r => r.Name == presentName)
                                .AsTensor<float>().Clone();
                        }

                currentEmbeds = _embedding.EmbedTokenId(nextToken);
                isFirstStep = false;
            }

            return [.. generatedTokens];
        }

        private static long ArgMax(Tensor<float> logits, int seqIdx)
        {
            int vocabSize = logits.Dimensions[2];
            long best = 0;
            float bestVal = float.MinValue;

            for (int v = 0; v < vocabSize; v++)
            {
                float val = logits[0, seqIdx, v];
                if (val > bestVal) { bestVal = val; best = v; }
            }

            return best;
        }
    }
}
