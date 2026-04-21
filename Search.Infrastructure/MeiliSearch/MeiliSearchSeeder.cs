using Mapster;
using Microsoft.EntityFrameworkCore;
using Search.Domain.Entity.TextSearch;
using Search.Persistance.Context;

namespace Search.Infrastructure.MeiliSearch
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
           // configure index settings once
           await _index.UpdateSearchableAttributesAsync(
               ["title", "description", "categories", "features", "details", "mainCategory", "store"]);

           await _index.UpdateFilterableAttributesAsync(
               ["mainCategory", "store", "price", "averageRating"]);

            const int batchSize = 500;
            int skip = 0;
            int totalSeeded = 0;

            // read from SQL, push to Meilisearch, avoiding storing all records in memory, as single read
            while (true)
            {
                var batch = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Categories)
                        .ThenInclude(c => c.Category)
                    .Include(p => p.Features)
                    .Include(p => p.Details)
                    .AsSplitQuery()
                    // only for skip/take
                    .OrderBy(p => p.Id)
                    .Skip(skip)
                    .Take(batchSize)
                    .ToListAsync();

                if (batch.Count == 0) break;

                var docs = batch.Adapt<List<ProductMeiliDocument>>();
                await _index.AddDocumentsInBatchesAsync(docs, batchSize: 100);

                totalSeeded += batch.Count;
                skip += batchSize;

                Console.WriteLine($"Seeded {totalSeeded} products...");

                // release memory between batches
                batch.Clear();
                docs.Clear();
            }

            Console.WriteLine($"Done. Total seeded: {totalSeeded}");
        }
   }
}
