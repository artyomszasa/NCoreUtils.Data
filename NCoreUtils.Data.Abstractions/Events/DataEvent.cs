using System;

namespace NCoreUtils.Data.Events
{
    /// <summary>
    /// Represents generic data repository related event.
    /// </summary>
    public abstract class DataEvent : IDataEvent
    {
        /// <summary>
        /// Gets service provider of the actual context of the data repository.
        /// </summary>
        public IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Gets the operation being signaled.
        /// </summary>
        public abstract DataOperation Operation { get; }

        /// <summary>
        /// Gets the type of the target entity being inserted, updated or removed.
        /// </summary>
        public abstract Type EntityType { get; }

        /// <summary>
        /// Gets the boxed value of the target entity being inserted, updated or removed.
        /// </summary>
        public abstract object Entity { get; }

        internal DataEvent(IServiceProvider serviceProvider)
            => ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Represents generic data repository related update event.
    /// </summary>
    public abstract class DataUpdateEvent : DataEvent
    {
        /// <summary>
        /// Gets the operation being signaled. Overridden to constantly return <c>Update</c>.
        /// </summary>
        public sealed override DataOperation Operation => DataOperation.Update;

        internal DataUpdateEvent(IServiceProvider serviceProvider) : base(serviceProvider) { }
    }

    /// <summary>
    /// Represents generic data repository related insert event.
    /// </summary>
    public abstract class DataInsertEvent : DataEvent
    {
        /// <summary>
        /// Gets the operation being signaled. Overridden to constantly return <c>Insert</c>.
        /// </summary>
        public sealed override DataOperation Operation => DataOperation.Insert;

        internal DataInsertEvent(IServiceProvider serviceProvider) : base(serviceProvider) { }
    }

    /// <summary>
    /// Represents generic data repository related delete event.
    /// </summary>
    public abstract class DataDeleteEvent : DataEvent
    {
        /// <summary>
        /// Gets the operation being signaled. Overridden to constantly return <c>Delete</c>.
        /// </summary>
        public sealed override DataOperation Operation => DataOperation.Delete;

        internal DataDeleteEvent(IServiceProvider serviceProvider) : base(serviceProvider) { }
    }

    /// <summary>
    /// Represents base class of data repository related update event for concrete entity type.
    /// </summary>
    public abstract class DataUpdateEventBase<T> : DataUpdateEvent
        where T : class
    {
        /// <summary>
        /// Read-only target entity reference.
        /// </summary>
        protected readonly T _entity;

        /// <summary>
        /// Gets the type of the target entity being updated. Overridden to constantly return type of the generic
        /// parameter.
        /// </summary>
        public sealed override Type EntityType => typeof(T);

        /// <summary>
        /// Gets the boxed value of the target entity being updated.
        /// </summary>
        public sealed override object Entity => _entity;

        /// <summary>
        /// Gets repository performing the operation.
        /// </summary>
        public IDataRepository<T> Repository { get; }

        internal DataUpdateEventBase(IServiceProvider serviceProvider, IDataRepository<T> repository, T entity)
            : base(serviceProvider)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }
    }

    /// <summary>
    /// Represents data repository related update event for concrete entity type.
    /// </summary>
    public sealed class DataUpdateEvent<T> : DataUpdateEventBase<T>, IDataEvent<T>
        where T : class
    {
        /// <summary>
        /// Gets the typed value of the target entity being updated.
        /// </summary>
        public new T Entity => _entity;

        internal DataUpdateEvent(IServiceProvider serviceProvider, IDataRepository<T> repository, T entity)
            : base(serviceProvider, repository, entity)
        { }
    }

    /// <summary>
    /// Represents base class of data repository related insert event for concrete entity type.
    /// </summary>
    public abstract class DataInsertEventBase<T> : DataInsertEvent
        where T : class
    {
        /// <summary>
        /// Read-only target entity reference.
        /// </summary>
        protected readonly T _entity;

        /// <summary>
        /// Gets the type of the target entity being inserted. Overridden to constantly return type of the generic
        /// parameter.
        /// </summary>
        public sealed override Type EntityType => typeof(T);

        /// <summary>
        /// Gets the boxed value of the target entity being inserted.
        /// </summary>
        public sealed override object Entity => _entity;

        /// <summary>
        /// Gets repository performing the operation.
        /// </summary>
        public IDataRepository<T> Repository { get; }

        internal DataInsertEventBase(IServiceProvider serviceProvider, IDataRepository<T> repository, T entity)
            : base(serviceProvider)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }
    }

    /// <summary>
    /// Represents data repository related insert event for concrete entity type.
    /// </summary>
    public sealed class DataInsertEvent<T> : DataInsertEventBase<T>, IDataEvent<T>
        where T : class
    {
        /// <summary>
        /// Gets the typed value of the target entity being inserted.
        /// </summary>
        public new T Entity => _entity;

        internal DataInsertEvent(IServiceProvider serviceProvider, IDataRepository<T> repository, T entity)
            : base(serviceProvider, repository, entity)
        { }
    }

    /// <summary>
    /// Represents base class of data repository related delete event for concrete entity type.
    /// </summary>
    public abstract class DataDeleteEventBase<T> : DataDeleteEvent
        where T : class
    {
        /// <summary>
        /// Read-only target entity reference.
        /// </summary>
        protected readonly T _entity;

        /// <summary>
        /// Gets the type of the target entity being deleted. Overridden to constantly return type of the generic
        /// parameter.
        /// </summary>
        public sealed override Type EntityType => typeof(T);

        /// <summary>
        /// Gets the boxed value of the target entity being deleted.
        /// </summary>
        public sealed override object Entity => _entity;

        /// <summary>
        /// Gets repository performing the operation.
        /// </summary>
        public IDataRepository<T> Repository { get; }

        internal DataDeleteEventBase(IServiceProvider serviceProvider, IDataRepository<T> repository, T entity)
            : base(serviceProvider)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }
    }

    /// <summary>
    /// Represents data repository related delete event for concrete entity type.
    /// </summary>
    public sealed class DataDeleteEvent<T> : DataDeleteEventBase<T>, IDataEvent<T>
        where T : class
    {
        /// <summary>
        /// Gets the typed value of the target entity being deleted.
        /// </summary>
        public new T Entity => _entity;

        internal DataDeleteEvent(IServiceProvider serviceProvider, IDataRepository<T> repository, T entity)
            : base(serviceProvider, repository, entity)
        { }
    }
}