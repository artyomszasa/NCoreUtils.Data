using System.Reflection;
using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public class FirestoreDbFactory
    {
        private readonly object _sync = new();

        private FirestoreDb? _db;

        public IFirestoreConfiguration Configuration { get; }

        public FirestoreDbFactory(IFirestoreConfiguration configuration)
            => Configuration = configuration ?? new FirestoreConfiguration();

        public FirestoreDb GetOrCreateFirestoreDb()
        {
            if (null == _db)
            {
                lock (_sync)
                {
                    if (null == _db)
                    {
                        var builder = new FirestoreDbBuilder { ProjectId = Configuration.ProjectId };
                        _db = builder.Build();
                    }
                }
            }
            return _db;
        }
    }
}