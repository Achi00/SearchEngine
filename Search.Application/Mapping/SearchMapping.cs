using Mapster;
using Search.Application.Dtos.ImageSearch;
using Search.Domain.Entity.Vectors;

namespace Search.Application.Mapping
{
    public class SearchMapping : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<SearchResult, SearchResponse>()
                .Map(dest => dest.ProductId, src => src.Id)
                .Map(dest => dest.Asin, src => 
                        src.Payload != null && src.Payload.ContainsKey("asin") ? src.Payload["asin"].ToString() : null)
                .Map(dest => dest.Title, src => 
                        src.Payload != null && src.Payload.ContainsKey("title") ? src.Payload["title"].ToString() : null)
                .Map(dest => dest.MainCategory, 
                        src => src.Payload != null && src.Payload.ContainsKey("main_category") ? src.Payload["main_category"].ToString() : null)
                .Map(dest => dest.Store, src => 
                        src.Payload != null && src.Payload.ContainsKey("store") ? src.Payload["store"].ToString() : null)
                .Map(dest => dest.AverageRating, 
                        src => Convert.ToDouble(src.Payload.GetValueOrDefault("average_rating") ?? 0))
                .Map(dest => dest.Price, 
                        src => Convert.ToDecimal(src.Payload.GetValueOrDefault("price") ?? 0))
                .Map(dest => dest.ImageUrl, 
                        src => src.Payload != null && src.Payload.ContainsKey("image_url") ? src.Payload["image_url"].ToString() : null);
        }
    }
}
