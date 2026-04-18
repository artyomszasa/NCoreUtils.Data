namespace NCoreUtils.Data.Rest;

public sealed class RestDataTransaction(RestDataRepositoryContext context) : IDataTransaction
{
    readonly RestDataRepositoryContext _context = context;

    public Guid Guid { get; } = Guid.NewGuid();

    public void Commit()
    {
        // FIXME
        _context._tx = null;
    }

    public ValueTask CommitAsync(CancellationToken cancellationToken = default)
    {
        // FIXME
        _context._tx = null;
        return default;
    }

    public void Dispose()
    {
        _context._tx = null;
    }

    public ValueTask DisposeAsync()
    {
        _context._tx = null;
        return default;
    }

    public void Rollback()
    {
        // FIXME
        _context._tx = null;
    }

    public ValueTask RollbackAsync(CancellationToken cancellationToken = default)
    {
        // FIXME
        _context._tx = null;
        return default;
    }
}