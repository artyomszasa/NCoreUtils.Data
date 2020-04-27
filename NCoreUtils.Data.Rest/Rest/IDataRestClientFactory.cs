using System;
using NCoreUtils.Rest;

namespace NCoreUtils.Data.Rest
{
    public interface IDataRestClientFactory
    {
        IRestClient CreateRestClient(Type entityType);

        IDataRestClient<TData> GetClient<TData>();

        IDataRestClient<TData, TId> GetClient<TData, TId>()
            where TData : IHasId<TId>;
    }
}