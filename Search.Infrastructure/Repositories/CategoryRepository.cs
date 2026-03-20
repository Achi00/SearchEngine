using Microsoft.EntityFrameworkCore;
using Search.Application.Interfaces.Repositories;
using Search.Domain.Entity.Products;
using Search.Persistance.Context;

namespace Search.Infrastructure.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly SearchDbContext _context;

        public CategoryRepository(SearchDbContext context)
        {
            _context = context;
        }
        public async Task AddRangeAsync(List<Category> categories, CancellationToken ct = default)
        {
            await _context.Categories.AddRangeAsync(categories, ct);
        }

        public async Task<List<Category>> GetByNamesAsync(List<string> names, CancellationToken ct = default)
        {
            return await _context.Categories.Where(c => names.Contains(c.Name)).ToListAsync(ct);
        }
    }
}
