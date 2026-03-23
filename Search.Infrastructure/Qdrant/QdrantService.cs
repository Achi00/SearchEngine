using Qdrant.Client;
using Qdrant.Client.Grpc;
using Search.Application.Interfaces.Qdrant;
using Search.Application.Mapping;
using Search.Domain.Entity.Vectors;

namespace Search.Infrastructure.Qdrant
{
    public class QdrantServices : IQdrantService
    {
        private readonly QdrantClient _client;

        public QdrantServices(QdrantClient client)
        {
            _client = client;
        }

        // image search only, in case orc could not extract any texts
        public async Task<IReadOnlyList<SearchResult>> SearchAsync(string collection, float[] vector, int limit = 10, Dictionary<string, object>? filters = null, CancellationToken ct = default)
        {
            var images = await _client.SearchAsync(
                collection,
                vector,
                filter: BuildFilter(filters),
                limit: (ulong)limit,
                payloadSelector: new WithPayloadSelector { Enable = true },
                cancellationToken: ct
            );

            return images.Select(r => new SearchResult
            {
                Id = r.Payload.TryGetValue("product_id", out var pid)
                    ? Guid.Parse(pid.StringValue)
                    : Guid.Empty,
                Score = r.Score,
                Payload = r.Payload.ToDictionary(
                    k => k.Key,
                     v => v.Value.KindCase switch
                     {
                         Value.KindOneofCase.StringValue => (object)v.Value.StringValue,
                         Value.KindOneofCase.DoubleValue => (object)v.Value.DoubleValue,
                         Value.KindOneofCase.IntegerValue => (object)v.Value.IntegerValue,
                         Value.KindOneofCase.BoolValue => (object)v.Value.BoolValue,
                         _ => (object)v.Value.StringValue
                     })
            }).ToList().AsReadOnly();
        }

        // hybrid search, in case ORC extracts text from image, Image - 60% | Text - 40%
        public async Task<IReadOnlyList<SearchResult>> SearchHybridAsync(
            float[] imageVector,
            float[] textVector,
            int limit = 10,
            Dictionary<string, object>? filters = null,
            CancellationToken ct = default)
        {
            // search both collections in parallel
            var imageTask = _client.SearchAsync(
                "products_image",
                imageVector,
                filter: BuildFilter(filters),
                limit: (ulong)limit,
                payloadSelector: new WithPayloadSelector { Enable = true }
            );

            var textTask = _client.SearchAsync(
                "products_text",
                textVector,
                filter: BuildFilter(filters),
                limit: (ulong)limit,
                payloadSelector: new WithPayloadSelector { Enable = true }
            );

            await Task.WhenAll(imageTask, textTask);

            var imageResults = imageTask.Result;
            var textResults = textTask.Result;

            // merge by product id, combine scores
            // image gets higher weight since that's the primary search signal
            var merged = new Dictionary<string, (float Score, ScoredPoint Point)>();

            foreach (var r in imageResults)
            {
                // 60% weight for image
                merged[r.Id.Uuid] = (r.Score * 0.6f, r);
            }

            foreach (var r in textResults)
            {
                // add 40% text weight
                if (merged.TryGetValue(r.Id.Uuid, out var existing))
                {
                    merged[r.Id.Uuid] = (existing.Score + r.Score * 0.4f, existing.Point);
                }
                else
                {
                    merged[r.Id.Uuid] = (r.Score * 0.4f, r);
                }
            }

            return merged.Values
                .OrderByDescending(r => r.Score)
                .Take(limit)
                .Select(r => new SearchResult
                {
                    Id = Guid.Parse(r.Point.Id.Uuid),
                    Score = r.Score,
                    Payload = r.Point.Payload.ToDictionary(
                        k => k.Key,
                        v => (object)v.Value.StringValue)
                })
                .ToList()
                .AsReadOnly();
        }

        public async Task UpsertImageAsync(IReadOnlyList<VectorPoint> points)
            => await UpsertAsync("products_image", points);

        public async Task UpsertTextAsync(IReadOnlyList<VectorPoint> points)
            => await UpsertAsync("products_text", points);

        // helpers
        private async Task UpsertAsync(string collection, IReadOnlyList<VectorPoint> points)
        {
            var qdrantPoints = points.Select(p => new PointStruct
            {
                Id = p.Id,
                Vectors = p.Vector,
                Payload = { QdrantValueMapConfig.ToPayload(p.Payload) }
            }).ToList().AsReadOnly();

            await _client.UpsertAsync(collection, qdrantPoints);
        }
        private static Filter? BuildFilter(Dictionary<string, object>? filters)
        {
            if (filters == null || !filters.Any()) return null;

            var filter = new Filter();
            filter.Must.AddRange(filters.Select(f => new Condition
            {
                Field = new FieldCondition
                {
                    Key = f.Key,
                    Match = new Match { Keyword = f.Value.ToString() }
                }
            }));

            return filter;
        }
    }
}
