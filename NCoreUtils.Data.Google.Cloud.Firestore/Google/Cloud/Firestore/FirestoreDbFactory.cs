using System.Reflection;
using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public class FirestoreDbFactory
    {
        private readonly object _sync = new object();

        private readonly IFirestoreConfiguration _configuration;

        private FirestoreDb? _db;

        public FirestoreDbFactory(IFirestoreConfiguration configuration)
            => _configuration = configuration ?? new FirestoreConfiguration();

        public FirestoreDb GetOrCreateFirestoreDb()
        {
            if (null == _db)
            {
                lock (_sync)
                {
                    if (null == _db)
                    {
                        var builder = new FirestoreDbBuilder { ProjectId = _configuration.ProjectId };
                        _db = builder.Build();
                    }
                }
            }
            return _db;
        }
    }
}