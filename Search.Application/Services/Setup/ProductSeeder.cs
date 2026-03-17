using Search.Application.Dtos.Dataset;
using Search.Application.Interfaces;
using Search.Application.Interfaces.Repositories;
using Search.Application.Interfaces.Setup;
using Search.Domain.Entity.Products;

namespace Search.Application.Services.Setup
{
    public class ProductSeeder : IProductSeeder
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly Dictionary<string, int> _categoryCache = new(StringComparer.OrdinalIgnoreCase);


        public ProductSeeder(IUnitOfWork unitOfWork, IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _unitOfWork = unitOfWork;
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
        }

        public async Task BulkInsertAsync(List<ProductSeedDto> dtos, CancellationToken ct = default)
        {
            await ResolveCategoriesAsync(dtos, ct);

            var products = dtos.Select(dto => new Product
            {
                Id = dto.Id,
                Asin = dto.Asin,
                Title = dto.Title,
                Description = dto.Description,
                FileName = dto.FileName,
                MainCategory = dto.MainCategory,
                Store = dto.Store,
                AverageRating = dto.AverageRating,
                RatingNumber = dto.RatingNumber,
                Price = dto.Price,
                DateFirstAvailable = dto.DateFirstAvailable,
                ImageUrl = dto.ImageUrl,

                Features = dto.Features
                    .Select(text => new ProductFeature { Text = text })
                    .ToList(),

                Details = dto.Details
                    .Select(kv => new ProductDetail { Key = kv.Key, Value = kv.Value })
                    .ToList(),

                Categories = dto.Categories
                    .Select(name => new ProductCategory { CategoryId = _categoryCache[name] })
                    .ToList()

            }).ToList();

            await _productRepository.AddRangeAsync(products, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        private async Task ResolveCategoriesAsync(List<ProductSeedDto> dtos, CancellationToken ct)
        {
            var newNames = dtos
                .SelectMany(d => d.Categories)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(name => !_categoryCache.ContainsKey(name))
                .ToList();

            if (newNames.Count == 0) return;

            // query through repository, not DbContext directly
            var existing = await _categoryRepository.GetByNamesAsync(newNames, ct);

            foreach (var cat in existing)
                _categoryCache[cat.Name] = cat.Id;

            var missing = newNames
                .Where(name => !_categoryCache.ContainsKey(name))
                .Select(name => new Category { Name = name })
                .ToList();

            if (missing.Count > 0)
            {
                await _categoryRepository.AddRangeAsync(missing, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                foreach (var cat in missing)
                    _categoryCache[cat.Name] = cat.Id;
            }
        }
    }
}
