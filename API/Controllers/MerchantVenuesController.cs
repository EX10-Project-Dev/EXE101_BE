using System.Security.Claims;
using API.DTOs;
using DAL.Constants;
using DAL.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[Route("api/v1/merchant/venues")]
[ApiController]
[Authorize(Roles = AppRoles.Merchant)]
public sealed class MerchantVenuesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(await db.Venues.AsNoTracking().Where(v => !v.IsDeleted && v.MerchantUserId == userId)
            .OrderBy(v => v.Name).Select(v => new { venueId = v.Id, v.SourceId, v.Name, v.Address,
                v.Description, v.PriceMin, v.PriceMax, v.PriceNote, v.PhoneNumber, status = v.Status.ToString() })
            .ToListAsync(cancellationToken));
    }

    [HttpPut("{venueId:guid}")]
    public async Task<IActionResult> Update(Guid venueId, UpdateMerchantVenueRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        // Ownership is part of the UPDATE predicate, including when an admin reassigns the venue concurrently.
        var updated = await db.Venues.Where(v => v.Id == venueId && !v.IsDeleted && v.MerchantUserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.Name, request.Name.Trim())
                .SetProperty(v => v.Address, request.Address.Trim())
                .SetProperty(v => v.Description, request.Description)
                .SetProperty(v => v.PriceMin, request.PriceMin)
                .SetProperty(v => v.PriceMax, request.PriceMax)
                .SetProperty(v => v.PriceNote, request.PriceNote)
                .SetProperty(v => v.PhoneNumber, request.PhoneNumber)
                .SetProperty(v => v.UpdatedAt, DateTime.UtcNow), cancellationToken);
        return updated == 0 ? NotFound(new { message = "Không tìm thấy quán thuộc quyền quản lý của bạn." })
            : Ok(new { message = "Đã cập nhật thông tin quán." });
    }
}
