
using Search.Application.Dtos.ImageSearch;

namespace Search.Application.Interfaces.ImageSearch
{
    public interface ISearch
    {
        Task<IEnumerable<SearchResponse>> SearchByImageAsync(byte[] imageBytes, int limit = 10, CancellationToken ct = default);
        Task<IEnumerable<SearchResponse>> SearchByTextAsync(string? searchQuery, int limit = 20, CancellationToken ct = default);
    }
}
