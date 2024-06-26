using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace NCoreUtils.Data;

/// <summary>
/// Data repository extensions.
/// </summary>
public static class DataRepositoryExtensions
{
    /// <summary>
    /// Attempts to query data entity by its business key. <c>null</c> may be returned no data entity in persistent
    /// store has the specified business key.
    /// </summary>
    /// <param name="repository">Data repository.</param>
    /// <param name="id">Business key.</param>
    /// <returns>
    /// Either data entity from persistent store that has the specified business key, <c>null</c> if no such entity
    /// exists.
    /// </returns>
    public static TData? Lookup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData, TId>(
        this IDataRepository<TData, TId> repository, TId id)
        where TData : IHasId<TId>
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(repository);
#else
        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }
#endif
        return repository.LookupAsync(id, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Persists specified data entity. If data entity is already present in the persistent store updates is
    /// persisted value, otherwise inserts data entity into persistent store. Persisting entity may update some
    /// of its values (e.g. id may be assigned to inserted data entity) thus result of this method should be used
    /// instead of the instance passed as parameter.
    /// </summary>
    /// <param name="repository">Data repository.</param>
    /// <param name="item">Data entity instance to persist.</param>
    /// <returns>Updated data entity.</returns>
    public static T Persist<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
        this IDataRepository<T> repository, T item)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(repository);
#else
        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }
#endif
        return repository.PersistAsync(item, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Removes data entity from the persistent store. Exception may be thrown if the specified data entity was not
    /// present in the persistent store.
    /// </summary>
    /// <param name="repository">Data repository.</param>
    /// <param name="item">Data entity to remove.</param>
    /// <param name="force">
    /// Force physical removal from the persistent store. If data entity implements
    /// <see cref="IHasState" /> interface then <see cref="IHasState.State" />
    /// may be updated instead of removing the entity. Setting <paramref name="force" /> to <c>true</c> suppresses
    /// this functionality.
    /// </param>
    public static void Remove<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
        this IDataRepository<T> repository, T item, bool force = false)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(repository);
#else
        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }
#endif
        repository.RemoveAsync(item, force, CancellationToken.None).GetAwaiter().GetResult();
    }
}