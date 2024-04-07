using System;

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

    public void Dispose()
    {
        _context._tx = null;
    }

    public void Rollback()
    {
        // FIXME
        _context._tx = null;
    }
}