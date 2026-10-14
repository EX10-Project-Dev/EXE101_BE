using System;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IUnitOfWork : IDisposable
    {
        IVenueRepository Venues { get; }
        IGenericRepository<T> Repository<T>() where T : class;
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
