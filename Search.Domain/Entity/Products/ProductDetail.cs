namespace Search.Domain.Entity.Products
{
    public sealed class ProductDetail
    {
        public int Id { get; set; }
        public Guid ProductId { get; set; }
        public Product Product { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
