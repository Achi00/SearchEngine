using Mapster;
using Meilisearch;
using Search.Application.Dtos.ImageSearch;
using Search.Application.Interfaces.ImageSearch;
using Search.Application.Interfaces.ML.BackgroundRemoval;
using Search.Application.Interfaces.ML.Embeddings;
using Search.Application.Interfaces.Qdrant;
using Search.Application.Services.MeilisearchService;
using Search.Domain.Entity.TextSearch;

namespace Search.Application.Services.ImageServices
{
    public class SearchService : ISearch
    {
        private readonly IImageEmbeddingService _imageEmbeddingService;
        private readonly ITextEmbeddingService _textEmbeddingService;
        private readonly IBGRemovalService _bgRemovalService;
        private readonly IQdrantService _qdrantService;
        private readonly IMeiliSearch _meiliSearch;

        public SearchService(
            IImageEmbeddingService imageEmbeddingService,
            ITextEmbeddingService textEmbeddingService,
            IBGRemovalService bgRemovalService, 
            IQdrantService qdrantService,
            IMeiliSearch meiliSearch)
        {
            _imageEmbeddingService = imageEmbeddingService;
            _textEmbeddingService = textEmbeddingService;
            _bgRemovalService = bgRemovalService;
            _qdrantService = qdrantService;
            _meiliSearch = meiliSearch;
        }

        // image search
        public async Task<IEnumerable<SearchResponse>> SearchByImageAsync(byte[] imageBytes, int limit = 10, CancellationToken ct = default)
        {
            // remove background from image
            var BgRemovedImageBytes = await _bgRemovalService.RemoveBackgroundAsync(imageBytes, ct);

            // image embedding
            var embedding = await _imageEmbeddingService.EmbedAsync(BgRemovedImageBytes, ct);

            // search vectors in qdrant db
            var searchValue = await _qdrantService.ImageSearchAsync("products_image", embedding, limit, null, ct);

            // adapt needed response type
            var results = searchValue.Adapt<IEnumerable<SearchResponse>>();

            return results;
        }

        // text search combining millisearch and embedded vector meaning search
        public async Task<IEnumerable<SearchResponse>> SearchByTextAsync(string? searchQuery, int limit = 20, CancellationToken ct = default)
        {
            var scores = new Dictionary<string, float>();
            var meiliDocs = new Dictionary<string, ProductMeiliDocument>();

            // miliseach keyword match on raw text
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var meiliHits = await _meiliSearch.SearchAsync(searchQuery, limit: limit * 2);

                foreach (var hit in meiliHits)
                {
                    Accumulate(scores, hit.Id.ToString(), (float)(hit.RankingScore ?? 0.0f) * 0.35f);
                    meiliDocs[hit.Id] = hit;
                }
            }

            // qdrant semantic matching
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                // transform search query into vectors
                var embedding = await _textEmbeddingService.EmbedAsync(searchQuery, ct);

                var qdrantHits = await _qdrantService.TextSearchAsync("products_text", embedding, limit * 2);
                foreach (var hit in qdrantHits)
                {
                    Accumulate(scores, hit.Id.ToString(), hit.Score * 0.35f);
                }
            }

            return scores
                .OrderByDescending(kv => kv.Value)
                .Take(limit)
                .Select(kv =>
                {
                    meiliDocs.TryGetValue(kv.Key, out var doc);
                    return new SearchResponse
                    {
                        ProductId = kv.Key,
                        Score = kv.Value,
                        Title = doc?.Title,
                        ImageUrl = doc?.Image,
                        Price = doc?.Price ?? 0,
                        AverageRating = doc?.AverageRating ?? 0,
                        MainCategory = doc?.MainCategory
                    };
                })
                .ToList();
        }
        private static void Accumulate(Dictionary<string, float> scores, string id, float score)
        {
            scores.TryGetValue(id, out var existing);
            scores[id] = existing + score;
        }
    }
}
