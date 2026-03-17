using Search.Domain.Entity.Products;

namespace Search.Application.Interfaces.Repositories
{
    public interface IProductRepository
    {
        Task AddRangeAsync(List<Product> products, CancellationToken ct = default);
    }
}
