using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Data;

/// <summary>
/// Date repository context exntesions.
/// </summary>
public static class DataRepositoryContextExtensions
{
    /// <summary>
    /// Starts new transaction.
    /// </summary>
    /// <param name="context">Data repository context.</param>
    /// <param name="isolationLevel">Isolation level.</param>
    /// <returns>Started transaction.</returns>
    public static IDataTransaction BeginTransaction(this IDataRepositoryContext context, IsolationLevel isolationLevel)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(context);
#else
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }
#endif
        var asyncRes = context.BeginTransactionAsync(isolationLevel, CancellationToken.None);
        if (asyncRes.IsCompletedSuccessfully)
        {
            return asyncRes.Result;
        }
        return asyncRes.AsTask().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Executes specified action within a transacted context. Implicitly commits the transaction if no exception
    /// has occured.
    /// </summary>
    /// <param name="context">Data repository context.</param>
    /// <param name="isolationLevel">Isolation level.</param>
    /// <param name="action">Action to perform in a transacted context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task TransactedAsync(this IDataRepositoryContext context, IsolationLevel isolationLevel, Func<Task> action, CancellationToken cancellationToken = default)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(action);
#else
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }
#endif
        using var tx = await context.BeginTransactionAsync(isolationLevel, cancellationToken);
        await action();
        tx.Commit();
    }

    /// <summary>
    /// Executes specified action within a transacted context. Implicitly commits the transaction if no exception
    /// has occured.
    /// </summary>
    /// <param name="context">Data repository context.</param>
    /// <param name="isolationLevel">Isolation level.</param>
    /// <param name="action">Action to perform in a transacted context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the action.</returns>
    public static async Task<TResult> TransactedAsync<TResult>(
        this IDataRepositoryContext context,
        IsolationLevel isolationLevel,
        Func<Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(action);
#else
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }
#endif
        using var tx = await context.BeginTransactionAsync(isolationLevel, cancellationToken);
        var result = await action();
        tx.Commit();
        return result;
    }

    /// <summary>
    /// Executes specified action within a transacted context. Implicitly commits the transaction if no exception
    /// has occured.
    /// </summary>
    /// <param name="context">Data repository context.</param>
    /// <param name="isolationLevel">Isolation level.</param>
    /// <param name="action">Action to perform in a transacted context.</param>
    public static void Transacted(this IDataRepositoryContext context, IsolationLevel isolationLevel, Action action)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(action);
#else
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }
#endif
        using var tx = context.BeginTransaction(isolationLevel);
        action();
        tx.Commit();
    }

    /// <summary>
    /// Executes specified action within a transacted context. Implicitly commits the transaction if no exception
    /// has occured.
    /// </summary>
    /// <param name="context">Data repository context.</param>
    /// <param name="isolationLevel">Isolation level.</param>
    /// <param name="action">Action to perform in a transacted context.</param>
    /// <returns>Result of the action.</returns>
    public static TResult Transacted<TResult>(this IDataRepositoryContext context, IsolationLevel isolationLevel, Func<TResult> action)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(action);
#else
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }
#endif
        using var tx = context.BeginTransaction(isolationLevel);
        var result = action();
        tx.Commit();
        return result;
    }
}