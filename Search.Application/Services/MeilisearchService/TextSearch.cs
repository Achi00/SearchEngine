namespace Search.Application.Services.MeilisearchService
{
    public class TextSearch
    {
        private readonly Meilisearch.Index _index;

        public TextSearch(Meilisearch.Index index)
        {
            _index = index;
        }

        //public async Task IndexProductsAsync(IEnumerable<ProductMeiliDocument> products)
        //{
        //    await _index.AddDocumentsInBatchesAsync(products, batchSize: 500);
        //}
    }
}
