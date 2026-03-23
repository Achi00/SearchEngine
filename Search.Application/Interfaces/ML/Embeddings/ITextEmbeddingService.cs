namespace Search.Application.Interfaces.ML.Embeddings
{
    public interface ITextEmbeddingService
    {
        Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
    }
}
