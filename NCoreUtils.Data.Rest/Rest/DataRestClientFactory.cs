using System;
using System.Linq;
using System.Reflection;
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

        private static readonly MethodInfo _getClient = typeof(DataRestClientFactory)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(e => e.Name == nameof(GetClient) && e.IsGenericMethodDefinition && e.GetGenericArguments().Length == 2);

        private readonly IDataRestClientCache _cache;

        public ExpressionParser ExpressionParser { get; }

        public DataRestConfiguration Configuration { get; }

        public DataRestClientFactory(IDataRestClientCache cache, ExpressionParser expressionParser, DataRestConfiguration configuration)
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

        public virtual IDataRestClient<TData, TId> GetClient<TData, TId>() where TData : IHasId<TId>
            => new DataRestClient<TData, TId>(this);

        public IDataRestClient<TData> GetClient<TData>()
            // => (IDataRestClient<TData>)Activator.CreateInstance(typeof(DataRestClient<,>).MakeGenericType(typeof(TData), Configuration[typeof(TData)].IdType), new object[]
            // {
            //     this
            // });
        {
            var m = _getClient.MakeGenericMethod(typeof(TData), Configuration[typeof(TData)].IdType);
            return (IDataRestClient<TData>)m.Invoke(this, new object[0]);
        }
    }
}