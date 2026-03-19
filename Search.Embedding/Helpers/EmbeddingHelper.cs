
namespace Helpers
{
    public static class EmbeddingHelper
    {
        public static float[] Normalize(float[] vector)
        {
            var magnitude = MathF.Sqrt(vector.Sum(x => x * x));
            return magnitude == 0 ? vector : vector.Select(x => x / magnitude).ToArray();
        }
    }
}
