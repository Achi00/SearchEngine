
using Search.Domain.Entity.Vectors;

namespace Search.Application.Interfaces.Qdrant
{
    public interface IVectorStore
    {
        Task UpsertAsync(string collection, IReadOnlyList<VectorPoint> points);
        Task<IReadOnlyList<SearchResult>> SearchAsync(
            string collection,
            float[] vector,
            int limit = 10,
            Dictionary<string, object>? filters = null);
    }
}
