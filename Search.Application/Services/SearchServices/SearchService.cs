using Mapster;
using Search.Application.Dtos.ImageSearch;
using Search.Application.Interfaces.ImageSearch;
using Search.Application.Interfaces.ML.BackgroundRemoval;
using Search.Application.Interfaces.ML.Embeddings;
using Search.Application.Interfaces.Qdrant;

namespace Search.Application.Services.ImageServices
{
    public class SearchService : ISearch
    {
        private readonly IImageEmbeddingService _imageEmbeddingService;
        private readonly IBGRemovalService _bgRemovalService;
        private readonly IQdrantService _qdrantService;

        public SearchService(IImageEmbeddingService imageEmbeddingService, IBGRemovalService bgRemovalService, IQdrantService qdrantService)
        {
            _imageEmbeddingService = imageEmbeddingService;
            _bgRemovalService = bgRemovalService;
            _qdrantService = qdrantService;
        }

        public async Task<IEnumerable<SearchResponse>> SearchByImageAsync(byte[] imageBytes, int limit = 10, CancellationToken ct = default)
        {
            // remove background from image
            var BgRemovedImageBytes = await _bgRemovalService.RemoveBackgroundAsync(imageBytes, ct);

            // TODO: add ocr later

            // image embedding
            var embedding = await _imageEmbeddingService.EmbedAsync(BgRemovedImageBytes, ct);

            // search vectors in qdrant db
            var searchValue = await _qdrantService.SearchAsync("products_image", embedding, limit, null, ct);

            // adapt needed response type
            var results = searchValue.Adapt<IEnumerable<SearchResponse>>();

            foreach (var r in results)
                Console.WriteLine($"Asin: {r.Asin}, ImageUrl: {r.ImageUrl}");

            return results;
        }
    }
}
