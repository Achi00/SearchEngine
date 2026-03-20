using Qdrant.Client;
using Qdrant.Client.Grpc;
using Search.Application.Interfaces.Qdrant;
using Search.Application.Mapping;
using Search.Domain.Entity.Vectors;

namespace Search.Application.Services.Qdrant
{
    public class QdrantServices : IVectorStore
    {
        private readonly QdrantClient _client;

        public QdrantServices(QdrantClient client)
        {
            _client = client;
        }
        public async Task<IReadOnlyList<SearchResult>> SearchAsync(string collection, float[] vector, int limit = 10, Dictionary<string, object>? filters = null)
        {
            Filter? filter = null;

            if (filters != null && filters.Any())
            {
                filter = new Filter
                {
                    Must =
                {
                    filters.Select(f => new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = f.Key,
                            Match = new Match
                            {
                                Keyword = f.Value.ToString()
                            }
                        }
                    })
                }
                };
            }

            var results = await _client.SearchAsync(
                collection,
                vector,
                limit: (uint)limit,
                filter: filter);

            return results.Select(r => new SearchResult
            {
                Id = r.Id.Num,
                Score = r.Score,
                Payload = r.Payload.ToDictionary(x => x.Key, x => QdrantValueMapConfig.FromValue(x.Value))
            }).ToList();
        }

        public async Task UpsertAsync(string collection, IReadOnlyList<VectorPoint> points)
        {
            // UpsertAsync Expects IReadOnlyList
            var qdrantPoints = points.Select(p => new PointStruct
            {
                Id = p.Id,
                Vectors = p.Vector,
                Payload = { QdrantValueMapConfig.ToPayload(p.Payload) }
            }).ToList().AsReadOnly();

            await _client.UpsertAsync(collection, qdrantPoints);
        }
    }
}
