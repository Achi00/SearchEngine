using Search.Application.Dtos.Dataset;

namespace Search.Application.Interfaces.Setup
{
    public interface IProductSeeder
    {
        Task BulkInsertAsync(List<ProductSeedDto> dtos, CancellationToken ct = default);
    }
}
