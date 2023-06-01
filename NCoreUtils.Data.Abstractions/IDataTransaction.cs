using System;

namespace NCoreUtils.Data;

/// <summary>
/// Defines synchronous transaction functionality.
/// </summary>
public interface IDataTransaction : IDisposable
{
    /// <summary>
    /// Gets unique identifier of the transaction.
    /// </summary>
    Guid Guid { get; }

    /// <summary>
    /// Commits all operations performed within the transaction.
    /// </summary>
    void Commit();

    /// <summary>
    /// Rollbacks all operations performed within the transaction.
    /// </summary>
    void Rollback();
}