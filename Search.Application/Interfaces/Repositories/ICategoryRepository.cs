using Search.Domain.Entity.Products;

namespace Search.Application.Interfaces.Repositories
{
    public interface ICategoryRepository
    {
        Task<List<Category>> GetByNamesAsync(List<string> names, CancellationToken ct = default);
        Task AddRangeAsync(List<Category> categories, CancellationToken ct = default);
    }
}
