using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Defines boxed data repository functionality. Allows access to the data context and data entity type managed by
    /// the implementing object.
    /// </summary>
    public interface IDataRepository
    {
        /// <summary>
        /// Gets data entity type.
        /// </summary>
        Type ElementType { get; }

        /// <summary>
        /// Gets data context associated with the current data repository.
        /// </summary>
        IDataRepositoryContext Context { get; }
    }

    /// <summary>
    /// Defines data repository functionality.
    /// </summary>
    /// <typeparam name="T">Data entity type.</typeparam>
    public interface IDataRepository<T> : IDataRepository
    {
        /// <summary>
        /// Get customizable data entity query object.
        /// </summary>
        IQueryable<T> Items { get; }

        /// <summary>
        /// Persists specified data entity. If data entity is already present in the persistent store updates is
        /// persisted value, otherwise inserts data entity into persistent store. Persisting entity may update some
        /// of its values (e.g. id may be assigned to inserted data entity) thus result of this method should be used
        /// instead of the instance passed as parameter.
        /// </summary>
        /// <param name="item">Data entity instance to persist.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Updated data entity.</returns>
        Task<T> PersistAsync(T item, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes data entity from the persistent store. Exception may be thrown if the specified data entity was not
        /// present in the persistent store.
        /// </summary>
        /// <param name="item">Data entity to remove.</param>
        /// <param name="force">
        /// Force physical removal from the persistent store. If data entity implements
        /// <see cref="NCoreUtils.Data.IHasState" /> interface then <see cref="P:NCoreUtils.Data.IHasState.State" />
        /// may be updated instead of removing the entity. Setting <paramref name="force" /> to <c>true</c> suppresses
        /// this functionality.
        /// </param>
        /// <param name="cancellationToken">CancellationToken</param>
        Task RemoveAsync(T item, bool force = false, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Defines data repository functionality where data entity is identified by value of the specified type.
    /// </summary>
    /// <typeparam name="TData">Data entity type.</typeparam>
    /// <typeparam name="TId">Data entity business key type.</typeparam>
    public interface IDataRepository<TData, TId> : IDataRepository<TData>
        where TData : IHasId<TId>
    {
        /// <summary>
        /// Attempts to query data entity by its business key. <c>null</c> may be returned no data entity in persistent
        /// store has the specified business key.
        /// </summary>
        /// <param name="id">Business key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// Either data entity from persistent store that has the specified business key, <c>null</c> if no such entity
        /// exists.
        /// </returns>
        Task<TData?> LookupAsync(TId id, CancellationToken cancellationToken = default);
    }
}