using API.Controllers;
using BLL.DTOs.Requests;
using BLL.DTOs.Responses;
using BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public sealed class SearchController : BaseApiController
{
    private readonly IVenueSearchService _venueSearchService;

    public SearchController(IVenueSearchService venueSearchService)
    {
        _venueSearchService = venueSearchService;
    }

    [HttpPost("recommendations")]
    [ProducesResponseType(typeof(VenueSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VenueSearchResponse>> GetRecommendations(
        [FromBody] VenueSearchRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _venueSearchService.SearchAsync(request, cancellationToken);
        return Ok(response);
    }
}
