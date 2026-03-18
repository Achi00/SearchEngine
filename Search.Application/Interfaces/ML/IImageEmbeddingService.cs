namespace Search.Application.Interfaces.ML
{
    public interface IImageEmbeddingService
    {
        Task<float[]> EmbedAsync(byte[] imageBytes, CancellationToken ct = default);
    }
}
