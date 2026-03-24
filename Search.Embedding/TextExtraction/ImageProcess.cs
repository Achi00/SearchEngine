using Microsoft.ML.OnnxRuntime.Tensors;

namespace TextExtraction
{
    public static class ImageProcess
    {
        public static DenseTensor<float> ConcatEmbeds(Tensor<float> a, Tensor<float> b)
        {
            int hiddenSize = a.Dimensions[2];
            int lenA = a.Dimensions[1];
            int lenB = b.Dimensions[1];
            var result = new DenseTensor<float>([1, lenA + lenB, hiddenSize]);

            for (int i = 0; i < lenA; i++)
                for (int h = 0; h < hiddenSize; h++)
                    result[0, i, h] = a[0, i, h];

            for (int i = 0; i < lenB; i++)
                for (int h = 0; h < hiddenSize; h++)
                    result[0, lenA + i, h] = b[0, i, h];

            return result;
        }

        public static int Argmax(Tensor<float> logits)
        {
            int vocabSize = logits.Dimensions[2];
            float max = float.MinValue;
            int idx = 0;
            for (int i = 0; i < vocabSize; i++)
            {
                if (logits[0, 0, i] > max)
                {
                    max = logits[0, 0, i];
                    idx = i;
                }
            }
            return idx;
        }
    }
}
