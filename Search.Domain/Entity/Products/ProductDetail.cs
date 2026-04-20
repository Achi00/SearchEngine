namespace Search.Domain.Entity.Products
{
    public class ProductMeiliDocument
    {
        // Guid.ToString()
        public string Id { get; set; }          
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? MainCategory { get; set; }
        public string? Store { get; set; }
        public decimal Price { get; set; }
        public double AverageRating { get; set; }
        public int RatingNumber { get; set; }
        public string? Asin { get; set; }
        public string? Image { get; set; }

        // flatten collections into searchable strings
        public List<string> Categories { get; set; } = [];
        public List<string> Features { get; set; } = [];

        // ProductDetails flattened key:value pairs become searchable text
        public string Details { get; set; } = "";
    }
}
