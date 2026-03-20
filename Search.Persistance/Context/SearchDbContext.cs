using Microsoft.EntityFrameworkCore;
using Search.Domain.Entity.Products;

namespace Search.Persistance.Context
{
    public class SearchDbContext : DbContext
    {
        public SearchDbContext(DbContextOptions options) : base(options) { }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<ProductFeature> ProductFeatures => Set<ProductFeature>();
        public DbSet<ProductDetail> ProductDetails => Set<ProductDetail>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SearchDbContext).Assembly);
        }
    }
}
