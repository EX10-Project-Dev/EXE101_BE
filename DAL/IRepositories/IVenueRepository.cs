using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DAL.Entities;

namespace DAL.IRepositories;

public interface IVenueRepository : IGenericRepository<Venue>
{
    Task<List<Venue>> GetPublishedVenuesWithDetailsAsync(CancellationToken cancellationToken = default);
    Task<Venue?> GetVenueByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}
