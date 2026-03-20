using Qdrant.Client.Grpc;
using Search.Application.Interfaces;
using Search.Application.Interfaces.ML;
using Search.Application.Interfaces.Qdrant;
using Search.Application.Interfaces.Repositories;
using Search.Application.Interfaces.Setup;
using Search.Domain.Entity.Products;
using Search.Domain.Entity.Vectors;

namespace Search.Infrastructure.ML
{
    public class EmbeddingPipeline : IEmbeddingPipeline
    {
        private readonly IProductRepository _productRepository;
        private readonly ITextEmbeddingService _textEmbeddingService;
        private readonly IImageEmbeddingService _imageEmbeddingService;
        private readonly IQdrantService _qdrantService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUnitOfWork _unitOfWork;

        // Temp: set default 100, 10 for testing
        private const int BatchSize = 10;
        private const int MaxConcurrentDownloads = 5;

        public EmbeddingPipeline(
        IProductRepository productRepository,
        ITextEmbeddingService textEmbeddingService,
        IImageEmbeddingService imageEmbeddingService,
        IQdrantService qdrantService,
        IHttpClientFactory httpClientFactory,
        IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _textEmbeddingService = textEmbeddingService;
            _imageEmbeddingService = imageEmbeddingService;
            _qdrantService = qdrantService;
            _httpClientFactory = httpClientFactory;
            _unitOfWork = unitOfWork;
        }


        public async Task RunAsync(CancellationToken ct = default, int maxBatches = int.MaxValue)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var semaphore = new SemaphoreSlim(MaxConcurrentDownloads);

            int totalProcessed = 0;
            int batchCount = 0;

            while (!ct.IsCancellationRequested && batchCount < maxBatches)
            {
                var batch = await _productRepository.GetUnembeddedBatchAsync(BatchSize, ct);
                if (batch.Count == 0)
                {
                    Console.WriteLine("Embedding pipeline complete.");
                    break;
                }

                var tasks = batch.Select(product => ProcessProductAsync(product, httpClient, semaphore, ct)).ToList();

                var results = await Task.WhenAll(tasks);

                // collect successfully embedded product ids
                var successful = results.Where(r => r != null).Select(r => r!).ToList();

                // batch upsert to Qdrant — one call per collection instead of one per product
                var textPoints = successful.Select(r => r.TextPoint).ToList().AsReadOnly();
                var imagePoints = successful
                    .Where(r => r.ImagePoint != null)
                    .Select(r => r.ImagePoint!)
                    .ToList().AsReadOnly();

                if (textPoints.Count > 0)
                    await _qdrantService.UpsertTextAsync(textPoints);

                if (imagePoints.Count > 0)
                    await _qdrantService.UpsertImageAsync(imagePoints);

                var successIds = successful.Select(r => r.ProductId).ToList();
                await _productRepository.MarkAsEmbeddedAsync(successIds, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                totalProcessed += successIds.Count;
                Console.WriteLine($"Embedded {totalProcessed} products so far...");

                batchCount++;
            }
        }

        private record EmbedResult(Guid ProductId, VectorPoint TextPoint, VectorPoint? ImagePoint);

        private async Task<EmbedResult?> ProcessProductAsync(Product product, HttpClient httpClient, SemaphoreSlim semaphore, CancellationToken ct)
        {
            try
            {
                // text embedding
                var text = $"{product.Title} {product.MainCategory} {product.Store}".Trim();
                var textEmbedding = await _textEmbeddingService.EmbedAsync(text, ct);

                // image embedding if url exists
                float[]? imageEmbedding = null;
                if (!string.IsNullOrEmpty(product.Image))
                {
                    await semaphore.WaitAsync(ct);
                    try
                    {
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        // timeout per image to avoid amazon blocking
                        cts.CancelAfter(TimeSpan.FromSeconds(10)); 

                        var imageBytes = await httpClient.GetByteArrayAsync(product.Image, cts.Token);
                        imageEmbedding = await _imageEmbeddingService.EmbedAsync(imageBytes, ct);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Image download failed for {product.Asin}: {ex.Message}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }

                // build qdrant payload
                var payload = new Dictionary<string, object>
                {
                    ["product_id"] = product.Id.ToString(),
                    ["asin"] = product.Asin ?? "",
                    ["title"] = product.Title ?? "",
                    ["main_category"] = product.MainCategory ?? "",
                    ["price"] = product.Price,
                    ["average_rating"] = product.AverageRating,
                    ["image_url"] = product.Image ?? "",
                    ["store"] = product.Store ?? ""
                };

                var pointId = BitConverter.ToUInt64(product.Id.ToByteArray(), 0);

                var textPoint = new VectorPoint { Id = pointId, Vector = textEmbedding, Payload = payload };
                VectorPoint? imagePoint = imageEmbedding != null
                    ? new VectorPoint { Id = pointId, Vector = imageEmbedding, Payload = payload }
                    : null;

                return new EmbedResult(product.Id, textPoint, imagePoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to process product {product.Asin}: {ex.Message}");
                return null;
            }
        }
    }
}
