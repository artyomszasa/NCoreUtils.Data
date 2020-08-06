using System;
using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.Data.InMemory
{
    [ExcludeFromCodeCoverage]
    public sealed class NoopTransaction : IDataTransaction
    {
        private readonly InMemoryDataRepositoryContext _context;

        public Guid Guid => Guid.NewGuid();

        public NoopTransaction(InMemoryDataRepositoryContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void Commit() => _context.ClearTransaction();

        public void Dispose() => _context.ClearTransaction();

        public void Rollback() => _context.ClearTransaction();
    }
}