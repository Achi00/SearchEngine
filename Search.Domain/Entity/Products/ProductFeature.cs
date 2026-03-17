
namespace Search.Domain.Entity.Products
{
    public sealed class ProductFeature
    {
        public int Id { get; set; }
        public Guid ProductId { get; set; }
        public Product Product { get; set; }
        public string Text { get; set; }
    }
}
