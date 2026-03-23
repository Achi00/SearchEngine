
namespace Search.Application.Interfaces.Setup
{
    public interface IEmbeddingPipeline
    {
        Task RunAsync(CancellationToken ct = default);
    }
}
