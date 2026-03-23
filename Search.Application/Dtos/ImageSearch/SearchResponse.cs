namespace Search.Application.Dtos.ImageSearch
{
    public class SearchResponse
    {
        public string ProductId { get; set; }
        public string Asin { get; set; }
        public string Title { get; set; }
        public string MainCategory { get; set; }
        public string Store { get; set; }
        public double AverageRating { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public float Score { get; set; }
    }
}
