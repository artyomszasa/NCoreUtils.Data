using System;
using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.Data.InMemory;

[ExcludeFromCodeCoverage]
public sealed class NoopTransaction(InMemoryDataRepositoryContext context) : IDataTransaction
{
    private readonly InMemoryDataRepositoryContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public Guid Guid => Guid.NewGuid();

    public void Commit() => _context.ClearTransaction();

    public void Dispose() => _context.ClearTransaction();

    public void Rollback() => _context.ClearTransaction();

    public ValueTask CommitAsync(CancellationToken cancellationToken = default)
    {
        Commit();
        return default;
    }

    public ValueTask RollbackAsync(CancellationToken cancellationToken = default)
    {
        Rollback();
        return default;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }
}