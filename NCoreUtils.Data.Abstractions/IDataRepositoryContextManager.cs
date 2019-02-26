using System;

namespace NCoreUtils.Data
{
    public interface IDataRepositoryContextManager
    {
        IDataRepositoryContext GetOrCreateContext(Guid guid);
    }
}