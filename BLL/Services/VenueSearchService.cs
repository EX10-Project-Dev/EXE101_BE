using BLL.DTOs.Requests;
using BLL.DTOs.Responses;
using BLL.Interfaces;

namespace BLL.Services;

/// <summary>
/// Contract scaffold. Replace the empty result with venue filtering, ranking,
/// explanation generation, and search-history persistence.
/// </summary>
public sealed class VenueSearchService : IVenueSearchService
{
    public Task<VenueSearchResponse> SearchAsync(VenueSearchRequest request, CancellationToken cancellationToken = default)
    {
        // Venue schema exists, but query/ranking logic and search-history persistence are pending.
        // History persistence is also pending; this ID identifies this response only for now.
        var response = new VenueSearchResponse
        {
            SearchHistoryId = Guid.NewGuid(),
            Status = "NO_MATCH",
            Message = "Chưa có dữ liệu địa điểm để tìm kiếm.",
            TotalCount = 0,
            Page = request.Page,
            PageSize = request.PageSize,
            HasMore = false,
            Results = []
        };

        return Task.FromResult(response);
    }
}
