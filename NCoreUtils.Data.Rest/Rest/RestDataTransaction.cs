using System;

namespace NCoreUtils.Data.Rest
{
    public class RestDataTransaction : IDataTransaction
    {
        readonly RestDataRepositoryContext _context;

        public Guid Guid { get; } = Guid.NewGuid();

        public RestDataTransaction(RestDataRepositoryContext context)
        {
            _context = context;
        }

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
}