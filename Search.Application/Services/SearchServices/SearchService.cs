using Mapster;
using Search.Application.Dtos.ImageSearch;
using Search.Application.Interfaces.ImageSearch;
using Search.Application.Interfaces.ML.BackgroundRemoval;
using Search.Application.Interfaces.ML.Embeddings;
using Search.Application.Interfaces.Qdrant;
using Search.Application.Services.MeilisearchService;

namespace Search.Application.Services.ImageServices
{
    public class SearchService : ISearch
    {
        private readonly IImageEmbeddingService _imageEmbeddingService;
        private readonly IBGRemovalService _bgRemovalService;
        private readonly IQdrantService _qdrantService;
        private readonly IMeiliSearch _meiliSearch;

        public SearchService(IImageEmbeddingService imageEmbeddingService, IBGRemovalService bgRemovalService, IQdrantService qdrantService)
        {
            _imageEmbeddingService = imageEmbeddingService;
            _bgRemovalService = bgRemovalService;
            _qdrantService = qdrantService;
        }

        // image search
        public async Task<IEnumerable<SearchResponse>> SearchByImageAsync(byte[] imageBytes, int limit = 10, CancellationToken ct = default)
        {
            // remove background from image
            var BgRemovedImageBytes = await _bgRemovalService.RemoveBackgroundAsync(imageBytes, ct);

            // image embedding
            var embedding = await _imageEmbeddingService.EmbedAsync(BgRemovedImageBytes, ct);

            // search vectors in qdrant db
            var searchValue = await _qdrantService.SearchAsync("products_image", embedding, limit, null, ct);

            // adapt needed response type
            var results = searchValue.Adapt<IEnumerable<SearchResponse>>();

            return results;
        }

        // text search combining millisearch and embedded vector meaning search
        public async Task<IEnumerable<SearchResponse>> SearchByTextAsync(string? searchQuery, int limit = 20)
        {
            var scores = new Dictionary<string, float>();

            // miliseach keyword match on raw text
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var meiliHits = await _meiliSearch.SearchAsync(searchQuery, limit: limit);

                foreach (var hit in meiliHits)
                {
                    Accumulate(scores, hit.Id.ToString(), (float)(hit.RankingScore ?? 0.0f) * 0.35f);
                }
            }

            // qdrant semantic matching
        }
        private static void Accumulate(Dictionary<string, float> scores, string id, float score)
        {
            scores.TryGetValue(id, out var existing);
            scores[id] = existing + score;
        }
    }
}
