using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BLL.DTOs.Responses;

namespace BLL.Interfaces;

public interface IVenueService
{
    Task<VenueDetailResponse?> GetVenueByIdAsync(string identifier, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AmenityResponse>> GetAmenitiesAsync(CancellationToken cancellationToken = default);
}
