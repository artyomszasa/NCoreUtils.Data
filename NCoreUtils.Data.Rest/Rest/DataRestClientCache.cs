using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Rest;
using NCoreUtils.Rest.Internal;

namespace NCoreUtils.Data.Rest
{
    public class DataRestClientCache
    {
        private readonly object _sync = new object();

        private readonly ConcurrentDictionary<IRestClientConfiguration, IHttpRestClient> _instances = new ConcurrentDictionary<IRestClientConfiguration, IHttpRestClient>();

        private readonly IServiceProvider _serviceProvider;

        public DataRestClientCache(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IHttpRestClient GetOrAdd(IRestClientConfiguration configuration)
        {
            if (_instances.TryGetValue(configuration, out var client))
            {
                return client;
            }
            lock (_sync)
            {
                return _instances.GetOrAdd(
                    configuration,
                    endpoint => ActivatorUtilities.CreateInstance<HttpRestClient>(
                        _serviceProvider.Override(configuration)
                    )
                );
            }
        }
    }
}