using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Search.Application.Interfaces.Repositories;
using Search.Domain.Entity.Products;
using Search.Persistance;
using Search.Persistance.Context;
using System.Data;

namespace Search.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly SearchDbContext _context;
        private readonly string _connectionString;

        public ProductRepository(SearchDbContext context, IConfiguration configuration)
        {
            _context = context;
            _connectionString = configuration.GetConnectionString(nameof(ConnectionString.Default))!;
        }

        public async Task AddRangeAsync(List<Product> products, CancellationToken ct = default)
        {
            // bulk insert products
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct);

            using var productBulk = new SqlBulkCopy(connection);
            productBulk.DestinationTableName = "Products";
            productBulk.BatchSize = 1000;

            var productTable = ToProductTable(products);
            await productBulk.WriteToServerAsync(productTable, ct);

            // bulk insert features
            var featureTable = ToFeatureTable(products);
            if (featureTable.Rows.Count > 0)
            {
                using var featureBulk = new SqlBulkCopy(connection);
                featureBulk.DestinationTableName = "ProductFeatures";
                featureBulk.BatchSize = 1000;
                await featureBulk.WriteToServerAsync(featureTable, ct);
            }

            // bulk insert details
            var detailTable = ToDetailTable(products);
            if (detailTable.Rows.Count > 0)
            {
                using var detailBulk = new SqlBulkCopy(connection);
                detailBulk.DestinationTableName = "ProductDetails";
                detailBulk.BatchSize = 1000;
                await detailBulk.WriteToServerAsync(detailTable, ct);
            }

            // bulk insert product-category links
            var categoryLinkTable = ToCategoryLinkTable(products);
            if (categoryLinkTable.Rows.Count > 0)
            {
                using var categoryLinkBulk = new SqlBulkCopy(connection);
                categoryLinkBulk.DestinationTableName = "ProductCategory";
                categoryLinkBulk.BatchSize = 1000;
                await categoryLinkBulk.WriteToServerAsync(categoryLinkTable, ct);
            }
        }

        private static DataTable ToProductTable(List<Product> products)
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("Asin", typeof(string));
            table.Columns.Add("Title", typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("FileName", typeof(string));
            table.Columns.Add("MainCategory", typeof(string));
            table.Columns.Add("Store", typeof(string));
            table.Columns.Add("AverageRating", typeof(double));
            table.Columns.Add("RatingNumber", typeof(int));
            table.Columns.Add("Price", typeof(decimal));
            //table.Columns.Add("DateFirstAvailable", typeof(DateTime));
            table.Columns.Add("Image", typeof(string));

            foreach (var p in products)
            {
                table.Rows.Add(
                    p.Id, p.Asin, p.Title, p.Description, p.FileName,
                    p.MainCategory, p.Store, p.AverageRating, p.RatingNumber,
                    p.Price, /* p.DateFirstAvailable, */ (object?)p.Image ?? DBNull.Value);
            }

            return table;
        }
        private static DataTable ToFeatureTable(List<Product> products)
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("ProductId", typeof(Guid));
            table.Columns.Add("Text", typeof(string));

            foreach (var p in products)
                foreach (var f in p.Features)
                    table.Rows.Add(DBNull.Value, p.Id, f.Text);

            return table;
        }

        private static DataTable ToDetailTable(List<Product> products)
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("ProductId", typeof(Guid));
            table.Columns.Add("Key", typeof(string));
            table.Columns.Add("Value", typeof(string));

            foreach (var p in products)
                foreach (var d in p.Details)
                    table.Rows.Add(DBNull.Value, p.Id, d.Key, d.Value);

            return table;
        }

        private static DataTable ToCategoryLinkTable(List<Product> products)
        {
            var table = new DataTable();
            table.Columns.Add("ProductId", typeof(Guid));
            table.Columns.Add("CategoryId", typeof(int));

            foreach (var p in products)
                foreach (var c in p.Categories)
                    table.Rows.Add(p.Id, c.CategoryId);

            return table;
        }
    }
}
