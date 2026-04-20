using Mapster;
using Search.Domain.Entity.Products;
using Search.Domain.Entity.TextSearch;

namespace Search.Application.Mapping
{
    public class MeiliSearchMapping : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Product, ProductMeiliDocument>()
                .Map(dest => dest.Id, src => src.Id.ToString())
                .Map(dest => dest.Categories,
                    src => src.Categories.Select(c => c.Category.Name).ToList())
                .Map(dest => dest.Features,
                    src => src.Features.Select(f => f.Text).ToList())
                .AfterMapping((src, dest) =>
                {
                    dest.Details = string.Join(" | ",
                        src.Details.Select(d => $"{d.Key}: {d.Value}"));
                });
        }
    }
}
