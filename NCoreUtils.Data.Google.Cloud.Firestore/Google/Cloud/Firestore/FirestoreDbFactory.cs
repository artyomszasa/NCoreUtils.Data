using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

public class FirestoreDbFactory(IFirestoreConfiguration configuration)
{
    private readonly object _sync = new();

    private FirestoreDb? _db;

    public IFirestoreConfiguration Configuration { get; } = configuration ?? new FirestoreConfiguration();

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
                        GrpcAdapter = global::Google.Api.Gax.Grpc.GrpcNetClientAdapter.Default
#endif
                    };
                    _db = builder.Build();
                }
            }
        }
        return _db;
    }
}