using System.Reflection;
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
                        var builder = new FirestoreDbBuilder
                        {
                            ProjectId = _configuration.ProjectId,
                            ConverterRegistry = new ConverterRegistry()
                        };
                        foreach (var converter in _configuration.CustomConverters)
                        {
                            if (converter.GetType().GetInterfaces().TryGetFirst(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IFirestoreConverter<>), out var ity))
                            {
                                var ety = ity.GetGenericArguments()[0];
                                var madd = typeof(ConverterRegistry).GetMethod(nameof(ConverterRegistry.Add), BindingFlags.Public | BindingFlags.Instance).MakeGenericMethod(ety);
                                madd.Invoke(builder.ConverterRegistry, new object[] { converter });
                            }
                        }
                        _db = builder.Build();
                    }
                }
            }
            return _db;
        }
    }
}