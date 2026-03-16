namespace Search.Domain.Entity
{
    public sealed class Product
    {
        public Guid Id { get; set; }
        public Guid Asin { get; set; }
        public DateTime DateFirstAlailable { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string FileName { get; set; }
        public string MainCategory { get; set; }
        //public string Categories{ get; set; }
        public string Store { get; set; }
        public double AverageRating { get; set; }
        public int RatingNumber { get; set; }
        public decimal Price { get; set; }
        //public string Features { get; set; }
        //public string Details { get; set; }
        public string ImageUrl { get; set; }
    }
}
