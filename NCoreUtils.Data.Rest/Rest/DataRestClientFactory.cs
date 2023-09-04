using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using NCoreUtils.Data.Protocol;
using NCoreUtils.Rest;
using NCoreUtils.Rest.Internal;

namespace NCoreUtils.Data.Rest
{
    public class DataRestClientFactory : IDataRestClientFactory
    {
        public class DataRestClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData, TId> : Rest.DataRestClient<TData, TId>
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

        public IDataUtils DataUtils { get; }

        public ExpressionParser ExpressionParser { get; }

        public DataRestConfiguration Configuration { get; }

        public DataRestClientFactory(
            IDataRestClientCache cache,
            IDataUtils dataUtils,
            ExpressionParser expressionParser,
            DataRestConfiguration configuration)
        {
            _cache = cache;
            DataUtils = dataUtils ?? throw new ArgumentNullException(nameof(dataUtils));
            ExpressionParser = expressionParser ?? throw new ArgumentNullException(nameof(expressionParser));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public IRestClient CreateRestClient(Type entityType)
        {
            var configuration = Configuration[entityType].Configuration;
            var httpRestClient = _cache.GetOrAdd(configuration);
            return CreateRestClient(httpRestClient);
        }

        protected virtual IRestClient CreateRestClient(IHttpRestClient httpRestClient)
            => new DefaultRestClient(DataUtils, ExpressionParser, httpRestClient);

        public virtual IDataRestClient<TData, TId> GetClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData, TId>() where TData : IHasId<TId>
            => new DataRestClient<TData, TId>(this);

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DataRestClientFactory))]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Generic method is preserved using dynamic dependency.")]
        [UnconditionalSuppressMessage("Trimming", "IL2060", Justification = "Argument types are preserved during registration.")]
        public IDataRestClient<TData> GetClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData>()
        {
            var m = _getClient.MakeGenericMethod(typeof(TData), Configuration[typeof(TData)].IdType);
            return (IDataRestClient<TData>)m.Invoke(this, Array.Empty<object>())!;
        }
    }
}