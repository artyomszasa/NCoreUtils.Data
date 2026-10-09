using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

public class FirestoreDbFactory(IFirestoreConfiguration configuration, ILoggerFactory? loggerFactory = default)
{
#if NET10_0_OR_GREATER
    private readonly Lock _sync = new();
#else
    private readonly object _sync = new();
#endif

    private FirestoreDb? _db;

    public IFirestoreConfiguration Configuration { get; } = configuration ?? new FirestoreConfiguration();

    public ILoggerFactory? LoggerFactory { get; } = loggerFactory;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "False positive")]
    public FirestoreDb GetOrCreateFirestoreDb()
    {
        if (_db is null)
        {
            lock (_sync)
            {
                if (_db is null)
                {
                    var builder = new FirestoreDbBuilder
                    {
                        ProjectId = Configuration.ProjectId,
#if NET6_0_OR_GREATER
                        GoogleCredential = configuration.GoogleCredential,
                        Logger = LoggerFactory?.CreateLogger("NCoreUtils.Data.Google.Cloud.Firestore.Client"),
                        GrpcAdapter = global::Google.Api.Gax.Grpc.GrpcNetClientAdapter.Default.WithAdditionalOptions(opts =>
                        {
                            if (LoggerFactory is not null)
                            {
                                opts.LoggerFactory = LoggerFactory;
                            }
                            Configuration.ConfigureGrpcChannelOptions?.Invoke(opts);
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