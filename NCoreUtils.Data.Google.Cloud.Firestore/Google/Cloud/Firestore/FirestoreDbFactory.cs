using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

public class FirestoreDbFactory(IFirestoreConfiguration configuration, ILoggerFactory? loggerFactory = default)
{
    private readonly object _sync = new();

    private FirestoreDb? _db;

    public IFirestoreConfiguration Configuration { get; } = configuration ?? new FirestoreConfiguration();

    public ILoggerFactory? LoggerFactory { get; } = loggerFactory;

    public FirestoreDb GetOrCreateFirestoreDb()
    {
        if (null == _db)
        {
            lock (_sync)
            {
                if (null == _db)
                {
                    var builder = new FirestoreDbBuilder
                    {
                        ProjectId = Configuration.ProjectId,
#if NET6_0_OR_GREATER
                        Logger = LoggerFactory?.CreateLogger("NCoreUtils.Data.Google.Cloud.Firestore.Client"),
                        GrpcAdapter = global::Google.Api.Gax.Grpc.GrpcNetClientAdapter.Default.WithAdditionalOptions(opts =>
                        {
                            if (LoggerFactory is not null)
                            {
                                opts.LoggerFactory = LoggerFactory;
                            }
                        })
#endif
                    };
                    _db = builder.Build();
                }
            }
        }
        return _db;
    }
}