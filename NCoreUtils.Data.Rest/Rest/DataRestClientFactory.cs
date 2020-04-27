using System;
using NCoreUtils.Data.Protocol;
using NCoreUtils.Rest;
using NCoreUtils.Rest.Internal;

namespace NCoreUtils.Data.Rest
{
    public class DataRestClientFactory : IDataRestClientFactory
    {
        public class DataRestClient<TData, TId> : Rest.DataRestClient<TData, TId>
            where TData : IHasId<TId>
        {
            public DataRestClient(IDataRestClientFactory factory)
                : base(factory.CreateRestClient(typeof(TData)))
            { }
        }

        private readonly DataRestClientCache _cache;

        public ExpressionParser ExpressionParser { get; }

        public DataRestConfiguration Configuration { get; }

        public DataRestClientFactory(DataRestClientCache cache, ExpressionParser expressionParser, DataRestConfiguration configuration)
        {
            _cache = cache;
            ExpressionParser = expressionParser;
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public IRestClient CreateRestClient(Type entityType)
        {
            var configuration = Configuration[entityType].Configuration;
            var httpRestClient = _cache.GetOrAdd(configuration);
            return CreateRestClient(httpRestClient);
        }

        protected virtual IRestClient CreateRestClient(IHttpRestClient httpRestClient)
            => new DefaultRestClient(ExpressionParser, httpRestClient);

        public IDataRestClient<TData, TId> GetClient<TData, TId>() where TData : IHasId<TId>
            => new DataRestClient<TData, TId>(this);

        public IDataRestClient<TData> GetClient<TData>()
            => (IDataRestClient<TData>)Activator.CreateInstance(typeof(DataRestClient<,>).MakeGenericType(typeof(TData), Configuration[typeof(TData)].IdType), new object[]
            {
                this
            });
    }
}