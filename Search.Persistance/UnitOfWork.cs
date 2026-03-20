using Search.Application.Interfaces;
using Search.Persistance.Context;

namespace Search.Persistance
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly SearchDbContext _context;

        public UnitOfWork(SearchDbContext context)
        {
            _context = context;
        }
        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            return await _context.SaveChangesAsync(ct);
        }
    }
}
