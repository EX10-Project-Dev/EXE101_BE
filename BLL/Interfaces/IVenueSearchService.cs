using BLL.DTOs.Requests;
using BLL.DTOs.Responses;

namespace BLL.Interfaces;

public interface IVenueSearchService
{
    Task<VenueSearchResponse> SearchAsync(VenueSearchRequest request, CancellationToken cancellationToken = default);
}
