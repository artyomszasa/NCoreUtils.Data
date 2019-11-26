using System;
using System.Collections.Concurrent;
using System.Threading;
using Google.Cloud.Firestore;
using NCoreUtils.Data.Google.FireStore.Builders;

namespace NCoreUtils.Data.Google.FireStore
{
    public class FireStoreDbFactory
    {
        protected readonly ConcurrentQueue<FirestoreDb> _pool = new ConcurrentQueue<FirestoreDb>();

        readonly Lazy<Model> _lazyModel;

        readonly IFireStoreConfiguration _configuration;

        public Model Model => _lazyModel.Value;

        public FireStoreDbFactory(IFireStoreConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _lazyModel = new Lazy<Model>(() =>
            {
                var builder = new ModelBuilder();
                SetUpModel(builder);
                return builder.Build();
            }, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        protected virtual void SetUpModel(ModelBuilder builder) { }

        public virtual FirestoreDb Rent() => _pool.TryDequeue(out var instance) ? instance : FirestoreDb.Create(_configuration.ProjectId);

        public virtual void Return(FirestoreDb instance) => _pool.Enqueue(instance);
    }
}