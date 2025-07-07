namespace Application.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IRepository<T> Repository<T>() where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();

    ILoanRepository LoanRepository { get; }
    IFineRepository FineRepository { get; }
    IAuditLogRepository AuditLogRepository { get; }
}
