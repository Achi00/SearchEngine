
using Search.Domain.Entity.Vectors;

namespace Search.Application.Interfaces.Qdrant
{
    public interface IQdrantService
    {
        Task UpsertTextAsync(IReadOnlyList<VectorPoint> points);
        Task UpsertImageAsync(IReadOnlyList<VectorPoint> points);

        // hybrid search - used when both image and OCR text are available
        // searches both collections and merges results
        Task<IReadOnlyList<SearchResult>> SearchHybridAsync(
            float[] imageVector,
            float[] textVector,
            int limit = 10,
            Dictionary<string, object>? filters = null);
    }
}
