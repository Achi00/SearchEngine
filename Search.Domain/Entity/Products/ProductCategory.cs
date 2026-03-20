namespace Search.Domain.Entity.Products
{
    public sealed class ProductCategory
    {
        public Guid ProductId { get; set; }
        public Product Product { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }
    }
}
