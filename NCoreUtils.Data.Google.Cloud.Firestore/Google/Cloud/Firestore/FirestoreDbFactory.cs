using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public class FirestoreDbFactory
    {
        private readonly object _sync = new object();

        private readonly FirestoreConfiguration _configuration;

        private FirestoreDb? _db;

        public FirestoreDbFactory(FirestoreConfiguration configuration)
            => _configuration = configuration ?? new FirestoreConfiguration();

        public FirestoreDb GetOrCreateFirestoreDb()
        {
            if (null == _db)
            {
                lock (_sync)
                {
                    if (null == _db)
                    {
                        _db = FirestoreDb.Create(_configuration.ProjectId);
                    }
                }
            }
            return _db;
        }
    }
}