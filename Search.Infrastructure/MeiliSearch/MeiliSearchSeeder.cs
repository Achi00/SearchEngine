using Mapster;
using Microsoft.EntityFrameworkCore;
using Search.Domain.Entity.TextSearch;
using Search.Persistance.Context;

namespace Search.Infrastructure.MeiliSearch
{
    public class MeiliSearchSeeder
    {
        // seeding data from me ms sql database and creating indexes from product title
        public class MeilisearchSeeder
        {
            private readonly Meilisearch.Index _index;
            private readonly SearchDbContext _context;

            public MeilisearchSeeder(Meilisearch.Index index, SearchDbContext context)
            {
                _index = index;
                _context = context;
            }

            public async Task SeedAsync()
            {
                // Configure index settings once
                await _index.UpdateSearchableAttributesAsync(
                    ["title", "description", "categories", "features", "details", "mainCategory", "store"]);

                await _index.UpdateFilterableAttributesAsync(
                    ["mainCategory", "store", "price", "averageRating"]);

                // Read from SQL, push to Meilisearch
                var products = await _context.Products
                    .Include(p => p.Categories)
                        // to reach c.Category.Name
                        .ThenInclude(c => c.Category)
                    .Include(p => p.Features)
                    .Include(p => p.Details)
                    .ToListAsync();

                var docs = products.Adapt<List<ProductMeiliDocument>>();

                await _index.AddDocumentsInBatchesAsync(docs, batchSize: 500);

                Console.WriteLine($"Seeded {docs.Count} products into Meilisearch");
            }
        }
    }
}
