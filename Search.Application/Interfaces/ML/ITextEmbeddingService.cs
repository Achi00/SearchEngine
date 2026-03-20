namespace Search.Application.Interfaces.ML
{
    public interface ITextEmbeddingService
    {
        Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
    }
}
