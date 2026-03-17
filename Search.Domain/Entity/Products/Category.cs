namespace Search.Domain.Entity.Products
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<ProductCategory> Products { get; set; } = [];
    }
}
