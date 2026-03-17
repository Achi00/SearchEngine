using Search.Application.Interfaces.Repositories;
using Search.Domain.Entity.Products;
using Search.Persistance.Context;

namespace Search.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly SearchDbContext _context;

        public ProductRepository(SearchDbContext context)
        {
            _context = context;
        }
        public async Task AddRangeAsync(List<Product> products, CancellationToken ct = default)
        {
            await _context.Products.AddRangeAsync(products, ct);
        }
    }
}
