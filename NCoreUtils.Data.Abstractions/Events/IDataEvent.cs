using System;

namespace NCoreUtils.Data.Events
{
    public interface IDataEvent
    {
        IServiceProvider ServiceProvider { get; }

        DataOperation Operation { get; }

        Type EntityType { get; }

        object Entity { get; }
    }

    public interface IDataEvent<T> : IDataEvent
        where T : class
    {
        IDataRepository<T> Repository { get; }

        new T Entity { get; }
    }
}