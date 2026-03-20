namespace Search.Domain.Entity.Products
{
    public sealed class Product
    {
        public Guid Id { get; set; }
        // amazon url identifier
        public string? Asin { get; set; }
        //public DateTime DateFirstAvailable { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? FileName { get; set; }
        public string? MainCategory { get; set; }
        public string? Store { get; set; }
        public double AverageRating { get; set; }
        public int RatingNumber { get; set; }
        public decimal Price { get; set; }
        public string? Image { get; set; }
        // for embedding set up
        public bool IsEmbedded { get; set; }

        public ICollection<ProductCategory> Categories { get; set; } = [];
        public ICollection<ProductFeature> Features { get; set; } = [];
        public ICollection<ProductDetail> Details { get; set; } = [];
    }
}
