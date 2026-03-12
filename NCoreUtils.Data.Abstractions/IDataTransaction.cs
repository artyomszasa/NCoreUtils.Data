namespace NCoreUtils.Data;

/// <summary>
/// Defines synchronous transaction functionality.
/// </summary>
public interface IDataTransaction : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Gets unique identifier of the transaction.
    /// </summary>
    Guid Guid { get; }

    /// <summary>
    /// Commits all operations performed within the transaction.
    /// </summary>
    [Obsolete("Use async version when possible")]
    void Commit();

    /// <summary>
    /// Commits all operations performed within the transaction.
    /// </summary>
    ValueTask CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollbacks all operations performed within the transaction.
    /// </summary>
    [Obsolete("Use async version when possible")]
    void Rollback();

    /// <summary>
    /// Rollbacks all operations performed within the transaction.
    /// </summary>
    ValueTask RollbackAsync(CancellationToken cancellationToken = default);
}