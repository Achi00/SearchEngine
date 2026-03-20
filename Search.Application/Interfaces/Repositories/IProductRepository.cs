using Search.Domain.Entity.Products;

namespace Search.Application.Interfaces.Repositories
{
    public interface IProductRepository
    {
        Task AddRangeAsync(List<Product> products, CancellationToken ct = default);
        Task<List<Product>> GetUnembeddedBatchAsync(int batchSize, CancellationToken ct = default);
        Task MarkAsEmbeddedAsync(List<Guid> productIds, CancellationToken ct = default);
    }
}
