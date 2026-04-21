using Search.Domain.Entity.TextSearch;

namespace Search.Application.Services.MeilisearchService
{
    public interface IMeiliSearch
    {
        Task ConfigureIndexAsync();
        Task IndexProductsAsync(IEnumerable<ProductMeiliDocument> products);
        Task<IEnumerable<ProductMeiliDocument>> SearchAsync(string query, string? categoryFilter = null, decimal? maxPrice = null, int limit = 40);
    }
}