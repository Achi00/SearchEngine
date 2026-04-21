using Meilisearch;
using Search.Domain.Entity.TextSearch;

namespace Search.Application.Services.MeilisearchService
{
    public class MeiliSearch : IMeiliSearch
    {
        private readonly Meilisearch.Index _index;

        public MeiliSearch(Meilisearch.Index index)
        {
            _index = index;
        }

        public async Task IndexProductsAsync(IEnumerable<ProductMeiliDocument> products)
        {
            await _index.AddDocumentsInBatchesAsync(products, batchSize: 500);
        }

        public async Task ConfigureIndexAsync()
        {
            await _index.UpdateFilterableAttributesAsync(
                ["mainCategory", "store", "price", "averageRating"]);

            await _index.UpdateSearchableAttributesAsync(
                ["title", "description", "categories", "features", "details", "mainCategory", "store"]);

            await _index.UpdateSortableAttributesAsync(
                ["price", "averageRating", "ratingNumber"]);
        }

        public async Task<IEnumerable<ProductMeiliDocument>> SearchAsync(
            string query,
            string? categoryFilter = null,
            decimal? maxPrice = null,
            int limit = 40)
        {
            var filters = new List<string>();
            if (categoryFilter != null)
                filters.Add($"mainCategory = \"{categoryFilter}\"");
            if (maxPrice != null)
                filters.Add($"price < {maxPrice}");

            var result = await _index.SearchAsync<ProductMeiliDocument>(query, new SearchQuery
            {
                Limit = limit,
                Filter = filters.Count > 0 ? string.Join(" AND ", filters) : null,
                // needed for fusion weighting later
                ShowRankingScore = true
            });

            return result.Hits;
        }
    }
}
