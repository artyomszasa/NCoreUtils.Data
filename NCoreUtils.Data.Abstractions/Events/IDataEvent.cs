using System;
using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.Data.Events
{
    /// <summary>
    /// Defines functionality implemented by data repository related events.
    /// </summary>
    public interface IDataEvent
    {
        /// <summary>
        /// Gets service provider of the actual context of the data repository.
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Gets the operation being signaled.
        /// </summary>
        DataOperation Operation { get; }

        /// <summary>
        /// Gets the type of the target entity being inserted, updated or removed.
        /// </summary>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        Type EntityType { get; }

        /// <summary>
        /// Gets the boxed value of the target entity being inserted, updated or removed.
        /// </summary>
        object Entity { get; }
    }

    /// <summary>
    /// Defines functionality implemented by data repository related events within typed environment.
    /// </summary>
    public interface IDataEvent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T> : IDataEvent
        where T : class
    {
        /// <summary>
        /// Gets repository performing the operation.
        /// </summary>
        IDataRepository<T> Repository { get; }

        /// <summary>
        /// Gets the boxed value of the target entity being deleted.
        /// </summary>
        new T Entity { get; }
    }
}