using Microsoft.Extensions.Logging;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

public static partial class LoggingExtensions
{
    public static class EventIds
    {
        public const int QueryExecuted = 2400;
    }

#if NET6_0_OR_GREATER
    [LoggerMessage(
        EventId = EventIds.QueryExecuted,
        EventName = nameof(EventIds.QueryExecuted),
        Level = LogLevel.Debug,
        Message = "Firestore query executed ({ElapsedMilliseconds}ms)"
    )]
    public static partial void LogQueryExecuted(this ILogger logger, long elapsedMilliseconds);

#else
    public static void LogQueryExecuted(this ILogger logger, long elapsedMilliseconds)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.Log(LogLevel.Debug, new EventId(EventIds.QueryExecuted, nameof(EventIds.QueryExecuted)), "Firestore query executed ({ElapsedMilliseconds}ms)", elapsedMilliseconds);
        }
    }
#endif
}