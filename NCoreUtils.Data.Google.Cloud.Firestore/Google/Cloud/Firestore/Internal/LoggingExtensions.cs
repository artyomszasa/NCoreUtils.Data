using Microsoft.Extensions.Logging;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

public static partial class LoggingExtensions
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Used internally, should not pollute global namespace")]
    public static class EventIds
    {
        public const int QueryExecuted = 2400;

        public const int TransactionWaitForMessages = 2401;

        public const int TransactionExecutingMessage = 2402;

        public const int TransactionExecutedMessage = 2403;

        public const int TransactionCommitting = 2404;

        public const int TransactionRollback = 2405;

        public const int TransactionRollbackOnDispose = 2406;

        public const int TransactionTaskNotFinished = 2407;

        public const int TransactionUnexpectedExceptionOnDispose = 2408;

        public const int TransactionUnexpectedRetry = 2409;

        public const int TransactionWaitFailed = 2410;

        public const int TransactionWaitTimeout = 2411;

        public const int TransactionMultipleOnCompleted = 2412;

        public const int TransactionOnCompletedSetWhenOnFailedHasBeenSet = 2413;

        public const int TransactionMultipleOnFailed = 2414;

        public const int TransactionOnFailedSetWhenOnCompletedHasBeenSet = 2415;

        public const int TransactionNoOnFailedOnRollbak = 2416;

        public const int TransactionCommitted = 2417;

        public const int TransactionNoOnCompletedOnSuccess = 2418;

        public const int TransactionCancelled = 2419;

        public const int TransactionFailedDueToException = 2420;

        public const int TransactionAttemptToSendMessageAfterCompletion = 2421;
    }

#if NET6_0_OR_GREATER
    [LoggerMessage(
        EventId = EventIds.QueryExecuted,
        EventName = nameof(EventIds.QueryExecuted),
        Level = LogLevel.Debug,
        Message = "Firestore query executed ({ElapsedMilliseconds}ms)."
    )]
    public static partial void LogQueryExecuted(this ILogger logger, long elapsedMilliseconds);

    [LoggerMessage(
        EventId = EventIds.TransactionWaitForMessages,
        EventName = nameof(EventIds.TransactionWaitForMessages),
        Level = LogLevel.Trace,
        Message = "{Guid} | Waiting for messages."
    )]
    public static partial void LogTransactionWaitForMessages(this ILogger logger, Guid guid);

    [LoggerMessage(
        EventId = EventIds.TransactionExecutingMessage,
        EventName = nameof(EventIds.TransactionExecutingMessage),
        Level = LogLevel.Trace,
        Message = "{Guid} | Executing message {Message}."
    )]
    public static partial void LogTransactionExecutingMessage(this ILogger logger, Guid guid, ISpanFormattable message);

    [LoggerMessage(
        EventId = EventIds.TransactionExecutedMessage,
        EventName = nameof(EventIds.TransactionExecutedMessage),
        Level = LogLevel.Trace,
        Message = "{Guid} | Executed message {Message} ({ElapsedMilliseconds}ms) => {Result}.",
        SkipEnabledCheck = false)]
    public static partial void LogTransactionExecutedMessage(this ILogger logger, Guid guid, ISpanFormattable message, long elapsedMilliseconds, bool result);

    [LoggerMessage(
        EventId = EventIds.TransactionCommitting,
        EventName = nameof(EventIds.TransactionCommitting),
        Level = LogLevel.Debug,
        Message = "{Guid} | Committing firestore transaction."
    )]
    public static partial void LogTransactionCommitting(this ILogger logger, Guid guid);

    [LoggerMessage(
        EventId = EventIds.TransactionCommitted,
        EventName = nameof(EventIds.TransactionCommitted),
        Level = LogLevel.Debug,
        Message = "{Guid} | Firestore transaction has been committed."
    )]
    public static partial void LogTransactionCommitted(this ILogger logger, Guid guid);

    [LoggerMessage(
        EventId = EventIds.TransactionCancelled,
        EventName = nameof(EventIds.TransactionCancelled),
        Level = LogLevel.Debug,
        Message = "{Guid} | Firestore transaction has been cancelled."
    )]
    public static partial void LogTransactionCancelled(this ILogger logger, Guid guid);

    [LoggerMessage(
        EventId = EventIds.TransactionFailedDueToException,
        EventName = nameof(EventIds.TransactionFailedDueToException),
        Level = LogLevel.Debug,
        Message = "{Guid} | Firestore transaction has faild due to exception."
    )]
    public static partial void LogTransactionFailedDueToException(this ILogger logger, Exception exn, Guid guid);

    [LoggerMessage(
        EventId = EventIds.TransactionRollbackOnDispose,
        EventName = nameof(EventIds.TransactionRollbackOnDispose),
        Level = LogLevel.Debug,
        Message = "{Guid} | Rolling back disposing firestore transaction."
    )]
    public static partial void LogTransactionRollbackOnDispose(this ILogger logger, Guid guid);

    [LoggerMessage(
        EventId = EventIds.TransactionTaskNotFinished,
        EventName = nameof(EventIds.TransactionTaskNotFinished),
        Level = LogLevel.Error,
        Message = "{Guid} | Firestore transaction have not finished ({Status}, IsCancelled = {IsCancelled}, IsCompleted = {IsCompleted}, IsFaulted = {IsFaulted})."
    )]
    public static partial void LogTransactionTaskNotFinished(this ILogger logger, Guid guid, TaskStatus status, bool isCancelled, bool isCompleted, bool isFaulted);

    [LoggerMessage(
        EventId = EventIds.TransactionRollback,
        EventName = nameof(EventIds.TransactionRollback),
        Level = LogLevel.Debug,
        Message = "{Guid} | Rolling back firestore transaction."
    )]
    public static partial void LogTransactionRollback(this ILogger logger, Guid guid);

    [LoggerMessage(
        EventId = EventIds.TransactionUnexpectedExceptionOnDispose,
        EventName = nameof(EventIds.TransactionUnexpectedExceptionOnDispose),
        Level = LogLevel.Error,
        Message = "{Guid} | Unexpected exception while disposing firestore transaction."
    )]
    public static partial void LogTransactionUnexpectedExceptionOnDispose(this ILogger logger, Exception exn, Guid guid);

    [LoggerMessage(
        EventId = EventIds.TransactionUnexpectedRetry,
        EventName = nameof(EventIds.TransactionUnexpectedRetry),
        Level = LogLevel.Error,
        Message = "{Guid} | Unexpected retry."
    )]
    public static partial void LogTransactionUnexpectedRetry(this ILogger logger, Guid guid);

    [LoggerMessage(
        EventId = EventIds.TransactionWaitFailed,
        EventName = nameof(EventIds.TransactionWaitFailed),
        Level = LogLevel.Trace,
        Message = "{Guid} | Task did not finished ({Status}, IsCancelled = {IsCancelled}, IsCompleted = {IsCompleted}, IsFaulted = {IsFaulted}).",
        SkipEnabledCheck = false)]
    public static partial void LogTransactionWaitFailed(this ILogger logger, Guid guid, TaskStatus status, bool isCancelled, bool isCompleted, bool isFaulted);

    [LoggerMessage(
        EventId = EventIds.TransactionWaitTimeout,
        EventName = nameof(EventIds.TransactionWaitTimeout),
        Level = LogLevel.Trace,
        Message = "{Guid} | Task did not finished (timeout, {Status}, IsCancelled = {IsCancelled}, IsCompleted = {IsCompleted}, IsFaulted = {IsFaulted}).",
        SkipEnabledCheck = false)]
    public static partial void LogTransactionWaitTimeout(this ILogger logger, Guid guid, TaskStatus status, bool isCancelled, bool isCompleted, bool isFaulted);

    [LoggerMessage(
        EventId = EventIds.TransactionMultipleOnCompleted,
        EventName = nameof(EventIds.TransactionMultipleOnCompleted),
        Level = LogLevel.Warning,
        Message = "{Guid} | OnCompleted set multiple times, cancelling earlier completion.",
        SkipEnabledCheck = false)]
    public static partial void LogTransactionMultipleOnCompleted(this ILogger logger, Guid guid);

    [LoggerMessage(
        EventId = EventIds.TransactionOnCompletedSetWhenOnFailedHasBeenSet,
        EventName = nameof(EventIds.TransactionOnCompletedSetWhenOnFailedHasBeenSet),
        Level = LogLevel.Warning,
        Message = "{Guid} | OnCompleted set when OnFailed has been set, cancelling failure completion.",
        SkipEnabledCheck = false)]
    public static partial void LogTransactionOnCompletedSetWhenOnFailedHasBeenSet(this ILogger logger, Guid guid);

    [LoggerMessage(
        EventId = EventIds.TransactionMultipleOnFailed,
        EventName = nameof(EventIds.TransactionMultipleOnFailed),
        Level = LogLevel.Warning,
        Message = "{Guid} | OnFailed set multiple times, cancelling earlier completion.",
        SkipEnabledCheck = false)]
    public static partial void LogTransactionMultipleOnFailed(this ILogger logger, Guid guid);

    [LoggerMessage(
        EventId = EventIds.TransactionOnFailedSetWhenOnCompletedHasBeenSet,
        EventName = nameof(EventIds.TransactionOnFailedSetWhenOnCompletedHasBeenSet),
        Level = LogLevel.Warning,
        Message = "{Guid} | OnFailed set when OnCompleted has been set, cancelling success completion.",
        SkipEnabledCheck = false)]
    public static partial void LogTransactionOnFailedSetWhenOnCompletedHasBeenSet(this ILogger logger, Guid guid);

    [LoggerMessage(
        EventId = EventIds.TransactionNoOnFailedOnRollbak,
        EventName = nameof(EventIds.TransactionNoOnFailedOnRollbak),
        Level = LogLevel.Warning,
        Message = "{Guid} | OnFailed was not set on aborted transaction.",
        SkipEnabledCheck = false)]
    public static partial void LogTransactionNoOnFailedOnRollbak(this ILogger logger, Guid guid);

    [LoggerMessage(
        EventId = EventIds.TransactionNoOnCompletedOnSuccess,
        EventName = nameof(EventIds.TransactionNoOnCompletedOnSuccess),
        Level = LogLevel.Warning,
        Message = "{Guid} | OnCompleted was not set on successfull transaction.",
        SkipEnabledCheck = false)]
    public static partial void LogTransactionNoOnCompletedOnSuccess(this ILogger logger, Guid guid);

    [LoggerMessage(
        EventId = EventIds.TransactionAttemptToSendMessageAfterCompletion,
        EventName = nameof(EventIds.TransactionAttemptToSendMessageAfterCompletion),
        Level = LogLevel.Warning,
        Message = "{Guid} | Attempting to send message after the transaction has completed.",
        SkipEnabledCheck = false)]
    public static partial void LogTransactionAttemptToSendMessageAfterCompletion(this ILogger logger, Guid guid);

#else
    public static void LogQueryExecuted(this ILogger logger, long elapsedMilliseconds)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.Log(LogLevel.Debug, new EventId(EventIds.QueryExecuted, nameof(EventIds.QueryExecuted)), "Firestore query executed ({ElapsedMilliseconds}ms)", elapsedMilliseconds);
        }
    }

    public static void LogTransactionWaitForMessages(this ILogger logger, Guid guid)
    {
        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.Log(
                logLevel: LogLevel.Trace,
                eventId: new EventId(EventIds.TransactionWaitForMessages, nameof(EventIds.TransactionWaitForMessages)),
                message: "{Guid} | Waiting for messages.",
                args: [guid]
            );
        }
    }

    public static void LogTransactionExecutingMessage(this ILogger logger, Guid guid, IFormattable message)
    {
        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.Log(
                logLevel: LogLevel.Trace,
                eventId: new EventId(EventIds.TransactionExecutingMessage, nameof(EventIds.TransactionExecutingMessage)),
                message: "{Guid} | Executing message {Message}.",
                args: [guid, message]
            );
        }
    }

    public static void LogTransactionExecutedMessage(this ILogger logger, Guid guid, IFormattable message, long elapsedMilliseconds, bool result)
    {
        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.Log(
                logLevel: LogLevel.Trace,
                eventId: new EventId(EventIds.TransactionExecutedMessage, nameof(EventIds.TransactionExecutedMessage)),
                message: "{Guid} | Executed message {Message} ({ElapsedMilliseconds}ms) => {Result}.",
                args: [guid, message, elapsedMilliseconds, result]
            );
        }
    }

    public static void LogTransactionCommitting(this ILogger logger, Guid guid)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.Log(
                logLevel: LogLevel.Debug,
                eventId: new EventId(EventIds.TransactionCommitting, nameof(EventIds.TransactionCommitting)),
                message: "{Guid} | Committing firestore transaction.",
                args: [guid]
            );
        }
    }

    public static void LogTransactionCommitted(this ILogger logger, Guid guid)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.Log(
                logLevel: LogLevel.Debug,
                eventId: new EventId(EventIds.TransactionCommitted, nameof(EventIds.TransactionCommitted)),
                message: "{Guid} | Firestore transaction has been committed.",
                args: [guid]
            );
        }
    }

    public static void LogTransactionCancelled(this ILogger logger, Guid guid)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.Log(
                logLevel: LogLevel.Debug,
                eventId: new EventId(EventIds.TransactionCancelled, nameof(EventIds.TransactionCancelled)),
                message: "{Guid} | Firestore transaction has been cancelled.",
                args: [guid]
            );
        }
    }

    public static void LogTransactionFailedDueToException(this ILogger logger, Exception exn, Guid guid)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.Log(
                logLevel: LogLevel.Debug,
                eventId: new EventId(EventIds.TransactionFailedDueToException, nameof(EventIds.TransactionFailedDueToException)),
                message: "{Guid} | Firestore transaction has faild due to exception.",
                args: [guid]
            );
        }
    }

    public static void LogTransactionRollbackOnDispose(this ILogger logger, Guid guid)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.Log(
                logLevel: LogLevel.Debug,
                eventId: new EventId(EventIds.TransactionRollbackOnDispose, nameof(EventIds.TransactionRollbackOnDispose)),
                message: "{Guid} | Rolling back disposing firestore transaction.",
                args: [guid]
            );
        }
    }

    public static void LogTransactionTaskNotFinished(this ILogger logger, Guid guid, TaskStatus status, bool isCancelled, bool isCompleted, bool isFaulted)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.Log(
                logLevel: LogLevel.Error,
                eventId: new EventId(EventIds.TransactionTaskNotFinished, nameof(EventIds.TransactionTaskNotFinished)),
                message: "{Guid} | Firestore transaction have not finished ({Status}, IsCancelled = {IsCancelled}, IsCompleted = {IsCompleted}, IsFaulted = {IsFaulted}).",
                args: [guid, status, isCancelled, isCompleted, isFaulted]
            );
        }
    }

    public static void LogTransactionRollback(this ILogger logger, Guid guid)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.Log(
                logLevel: LogLevel.Debug,
                eventId: new EventId(EventIds.TransactionRollback, nameof(EventIds.TransactionRollback)),
                message: "{Guid} | Roolling back firestore transaction.",
                args: [guid]
            );
        }
    }

    public static void LogTransactionUnexpectedExceptionOnDispose(this ILogger logger, Exception exn, Guid guid)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.Log(
                logLevel: LogLevel.Error,
                eventId: new EventId(EventIds.TransactionUnexpectedExceptionOnDispose, nameof(EventIds.TransactionUnexpectedExceptionOnDispose)),
                exception: exn,
                message: "{Guid} | Unexpected exception while disposing firestore transaction.",
                args: [guid]
            );
        }
    }

    public static void LogTransactionUnexpectedRetry(this ILogger logger, Guid guid)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.Log(
                logLevel: LogLevel.Error,
                eventId: new EventId(EventIds.TransactionUnexpectedRetry, nameof(EventIds.TransactionUnexpectedRetry)),
                exception: default,
                message: "{Guid} | Unexpected retry.",
                args: [guid]
            );
        }
    }

    public static void LogTransactionMultipleOnCompleted(this ILogger logger, Guid guid)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.Log(
                logLevel: LogLevel.Warning,
                eventId: new EventId(EventIds.TransactionMultipleOnCompleted, nameof(EventIds.TransactionMultipleOnCompleted)),
                exception: default,
                message: "{Guid} | OnCompleted set multiple times, cancelling earlier completion.",
                args: [guid]
            );
        }
    }

    public static void LogTransactionOnCompletedSetWhenOnFailedHasBeenSet(this ILogger logger, Guid guid)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.Log(
                logLevel: LogLevel.Warning,
                eventId: new EventId(EventIds.TransactionOnCompletedSetWhenOnFailedHasBeenSet, nameof(EventIds.TransactionOnCompletedSetWhenOnFailedHasBeenSet)),
                exception: default,
                message: "{Guid} | OnCompleted set when OnFailed has been set, cancelling failure completion.",
                args: [guid]
            );
        }
    }

    public static void LogTransactionMultipleOnFailed(this ILogger logger, Guid guid)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.Log(
                logLevel: LogLevel.Warning,
                eventId: new EventId(EventIds.TransactionMultipleOnFailed, nameof(EventIds.TransactionMultipleOnFailed)),
                exception: default,
                message: "{Guid} | OnFailed set multiple times, cancelling earlier completion.",
                args: [guid]
            );
        }
    }

    public static void LogTransactionOnFailedSetWhenOnCompletedHasBeenSet(this ILogger logger, Guid guid)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.Log(
                logLevel: LogLevel.Warning,
                eventId: new EventId(EventIds.TransactionOnFailedSetWhenOnCompletedHasBeenSet, nameof(EventIds.TransactionOnFailedSetWhenOnCompletedHasBeenSet)),
                exception: default,
                message: "{Guid} | OnFailed set when OnCompleted has been set, cancelling success completion.",
                args: [guid]
            );
        }
    }

    public static void LogTransactionNoOnFailedOnRollbak(this ILogger logger, Guid guid)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.Log(
                logLevel: LogLevel.Warning,
                eventId: new EventId(EventIds.TransactionNoOnFailedOnRollbak, nameof(EventIds.TransactionNoOnFailedOnRollbak)),
                exception: default,
                message: "{Guid} | OnFailed was not set on aborted transaction.",
                args: [guid]
            );
        }
    }

    public static void LogTransactionNoOnCompletedOnSuccess(this ILogger logger, Guid guid)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.Log(
                logLevel: LogLevel.Warning,
                eventId: new EventId(EventIds.TransactionNoOnCompletedOnSuccess, nameof(EventIds.TransactionNoOnCompletedOnSuccess)),
                exception: default,
                message: "{Guid} | OnCompleted was not set on successfull transaction.",
                args: [guid]
            );
        }
    }

    public static void LogTransactionAttemptToSendMessageAfterCompletion(this ILogger logger, Guid guid)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.Log(
                logLevel: LogLevel.Warning,
                eventId: new EventId(EventIds.TransactionAttemptToSendMessageAfterCompletion, nameof(EventIds.TransactionAttemptToSendMessageAfterCompletion)),
                exception: default,
                message: "{Guid} | Attempting to send message after the transaction has completed.",
                args: [guid]
            );
        }
    }

#endif
}