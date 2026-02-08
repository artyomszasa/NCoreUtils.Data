using System;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

public static partial class LoggingExtensions
{
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
    public static partial void LogTransactionExecutingMessage(this ILogger logger, Guid guid, string? message);

    [LoggerMessage(
        EventId = EventIds.TransactionExecutedMessage,
        EventName = nameof(EventIds.TransactionExecutedMessage),
        Level = LogLevel.Trace,
        Message = "{Guid} | Executed message {Message} ({ElapsedMilliseconds}ms) => {Result}."
    )]
    public static partial void LogTransactionExecutedMessage(this ILogger logger, Guid guid, string? message, long elapsedMilliseconds, bool result);

    [LoggerMessage(
        EventId = EventIds.TransactionCommitting,
        EventName = nameof(EventIds.TransactionCommitting),
        Level = LogLevel.Debug,
        Message = "{Guid} | Committing firestore transaction."
    )]
    public static partial void LogTransactionCommitting(this ILogger logger, Guid guid);

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
        Message = "{Guid} | Firestore transaction have not finished."
    )]
    public static partial void LogTransactionTaskNotFinished(this ILogger logger, Guid guid);

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

    public static void LogTransactionExecutingMessage(this ILogger logger, Guid guid, string? message)
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

    public static void LogTransactionExecutedMessage(this ILogger logger, Guid guid, string? message, long elapsedMilliseconds, bool result)
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

    public static void LogTransactionTaskNotFinished(this ILogger logger, Guid guid)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.Log(
                logLevel: LogLevel.Error,
                eventId: new EventId(EventIds.TransactionTaskNotFinished, nameof(EventIds.TransactionTaskNotFinished)),
                message: "{Guid} | Firestore transaction have not finished.",
                args: [guid]
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
#endif
}