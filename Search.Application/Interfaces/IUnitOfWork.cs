namespace Search.Application.Interfaces
{
    public interface IUnitOfWork
    {
        //Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
        //Task CommitTransactionAsync(CancellationToken ct = default);
        //Task RollbackTransactionAsync(CancellationToken ct = default);
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
