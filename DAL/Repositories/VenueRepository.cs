using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAL.Data;
using DAL.Entities;
using DAL.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;

public class VenueRepository : GenericRepository<Venue>, IVenueRepository
{
    public VenueRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<List<Venue>> GetPublishedVenuesWithDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Venues
            .Include(v => v.Category)
            .Include(v => v.Photos)
            .Include(v => v.OpeningHours)
            .Include(v => v.HoursExceptions)
            .Include(v => v.Tags).ThenInclude(t => t.Tag)
            .Include(v => v.Amenities).ThenInclude(a => a.Amenity)
            .Where(v => v.Status == VenueStatus.Published && !v.IsDeleted)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Venue?> GetVenueByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Venues
            .Include(v => v.Category)
            .Include(v => v.Photos)
            .Include(v => v.OpeningHours)
            .Include(v => v.HoursExceptions)
            .Include(v => v.Tags).ThenInclude(t => t.Tag)
            .Include(v => v.Amenities).ThenInclude(a => a.Amenity)
            .Where(v => v.Id == id && !v.IsDeleted)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }
}
