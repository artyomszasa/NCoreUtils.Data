using System;

namespace NCoreUtils.Data.Events
{
    public abstract class DataEvent : IDataEvent
    {
        public IServiceProvider ServiceProvider { get; }

        public abstract DataOperation Operation { get; }

        public abstract Type EntityType { get; }

        public abstract object Entity { get; }

        internal DataEvent(IServiceProvider serviceProvider)
            => ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public abstract class DataUpdateEvent : DataEvent
    {
        public sealed override DataOperation Operation => DataOperation.Update;

        internal DataUpdateEvent(IServiceProvider serviceProvider) : base(serviceProvider) { }
    }

    public abstract class DataInsertEvent : DataEvent
    {
        public sealed override DataOperation Operation => DataOperation.Insert;

        internal DataInsertEvent(IServiceProvider serviceProvider) : base(serviceProvider) { }
    }

    public abstract class DataDeleteEvent : DataEvent
    {
        public sealed override DataOperation Operation => DataOperation.Delete;

        internal DataDeleteEvent(IServiceProvider serviceProvider) : base(serviceProvider) { }
    }

    public abstract class DataUpdateEventBase<T> : DataUpdateEvent
        where T : class
    {
        protected readonly T _entity;

        public sealed override Type EntityType => typeof(T);

        public sealed override object Entity => _entity;

        public IDataRepository<T> Repository { get; }

        internal DataUpdateEventBase(IServiceProvider serviceProvider, IDataRepository<T> repository, T entity)
            : base(serviceProvider)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }
    }

    public sealed class DataUpdateEvent<T> : DataUpdateEventBase<T>, IDataEvent<T>
        where T : class
    {
        public new T Entity => _entity;

        internal DataUpdateEvent(IServiceProvider serviceProvider, IDataRepository<T> repository, T entity)
            : base(serviceProvider, repository, entity)
        { }
    }

    public abstract class DataInsertEventBase<T> : DataInsertEvent
        where T : class
    {
        protected readonly T _entity;

        public override Type EntityType => typeof(T);

        public override object Entity => _entity;

        public IDataRepository<T> Repository { get; }

        internal DataInsertEventBase(IServiceProvider serviceProvider, IDataRepository<T> repository, T entity)
            : base(serviceProvider)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }
    }


    public sealed class DataInsertEvent<T> : DataInsertEventBase<T>, IDataEvent<T>
        where T : class
    {
        public new T Entity => _entity;

        internal DataInsertEvent(IServiceProvider serviceProvider, IDataRepository<T> repository, T entity)
            : base(serviceProvider, repository, entity)
        { }
    }

    public abstract class DataDeleteEventBase<T> : DataDeleteEvent
        where T : class
    {
        protected readonly T _entity;

        public override Type EntityType => typeof(T);

        public override object Entity => _entity;

        public IDataRepository<T> Repository { get; }

        internal DataDeleteEventBase(IServiceProvider serviceProvider, IDataRepository<T> repository, T entity)
            : base(serviceProvider)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }
    }

    public sealed class DataDeleteEvent<T> : DataDeleteEventBase<T>, IDataEvent<T>
        where T : class
    {
        public new T Entity => _entity;

        internal DataDeleteEvent(IServiceProvider serviceProvider, IDataRepository<T> repository, T entity)
            : base(serviceProvider, repository, entity)
        { }
    }
}