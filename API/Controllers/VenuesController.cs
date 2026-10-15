using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BLL.DTOs.Common;
using BLL.DTOs.Responses;
using BLL.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public sealed class VenuesController : BaseApiController
{
    private readonly IVenueService _venueService;

    public VenuesController(IVenueService venueService)
    {
        _venueService = venueService;
    }

    /// <summary>
    /// Lấy thông tin chi tiết một địa điểm theo ID (GUID) hoặc Mã địa điểm nguồn (ví dụ: VEN-001).
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<VenueDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VenueDetailResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<VenueDetailResponse>>> GetById(
        string id,
        CancellationToken cancellationToken)
    {
        var venue = await _venueService.GetVenueByIdAsync(id, cancellationToken);
        if (venue is null)
        {
            return NotFound(ApiResponse<VenueDetailResponse>.Fail($"Không tìm thấy địa điểm với mã '{id}'."));
        }

        return Ok(ApiResponse<VenueDetailResponse>.Ok(venue, "Lấy thông tin chi tiết địa điểm thành công."));
    }

    /// <summary>
    /// Lấy danh sách danh mục địa điểm để hiển thị bộ lọc (Filter) cho người dùng.
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CategoryResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryResponse>>>> GetCategories(
        CancellationToken cancellationToken)
    {
        var categories = await _venueService.GetCategoriesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CategoryResponse>>.Ok(categories, "Lấy danh sách danh mục thành công."));
    }

    /// <summary>
    /// Lấy danh sách tiện ích (Wifi, máy lạnh, bãi đỗ xe...) để hiển thị bộ lọc (Filter) cho người dùng.
    /// </summary>
    [HttpGet("amenities")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AmenityResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AmenityResponse>>>> GetAmenities(
        CancellationToken cancellationToken)
    {
        var amenities = await _venueService.GetAmenitiesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AmenityResponse>>.Ok(amenities, "Lấy danh sách tiện ích thành công."));
    }
}
