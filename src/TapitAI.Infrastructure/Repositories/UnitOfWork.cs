using Microsoft.EntityFrameworkCore.Storage;
using TapitAI.Domain.Common;
using TapitAI.Domain.Interfaces.Repositories;
using TapitAI.Infrastructure.Data;

namespace TapitAI.Infrastructure.Repositories;

public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repositories = [];
    private IDbContextTransaction? _transaction;

    public IRepository<T> Repository<T>() where T : BaseEntity
    {
        var type = typeof(T);
        if (!_repositories.TryGetValue(type, out var repo))
        {
            repo = new Repository<T>(context);
            _repositories[type] = repo;
        }
        return (IRepository<T>)repo;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);

    public void ResetChangeTracker() => context.ChangeTracker.Clear();

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => _transaction = await context.Database.BeginTransactionAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
            await _transaction.CommitAsync(ct);
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
            await _transaction.RollbackAsync(ct);
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        context.Dispose();
    }
}
