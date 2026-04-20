namespace Search.Domain.Entity.TextSearch
{
    // Meilisearch indexing
    public class ProductMeiliDocument
    {
        // Qdrant id
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Brand { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public string ThumbnailUrl { get; set; }
    }
}
