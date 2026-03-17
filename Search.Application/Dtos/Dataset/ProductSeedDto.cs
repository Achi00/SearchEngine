namespace Search.Application.Dtos.Dataset
{
    public sealed class ProductSeedDto
    {
        public Guid Id { get; set; }
        public string Asin { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string FileName { get; set; }
        public string MainCategory { get; set; }
        public string Store { get; set; }
        public double AverageRating { get; set; }
        public int RatingNumber { get; set; }
        public decimal Price { get; set; }
        public DateTime DateFirstAvailable { get; set; }
        public string? ImageUrl { get; set; }

        public List<string> Categories { get; set; } = [];
        public List<string> Features { get; set; } = [];
        public List<(string Key, string Value)> Details { get; set; } = [];
    }
}
