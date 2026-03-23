namespace Search.Application.Interfaces.ML.Embeddings
{
    public interface IImageEmbeddingService
    {
        Task<float[]> EmbedAsync(byte[] imageBytes, CancellationToken ct = default);
    }
}
