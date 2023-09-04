using System;
using System.Diagnostics.CodeAnalysis;
using NCoreUtils.Rest;

namespace NCoreUtils.Data.Rest
{
    public interface IDataRestClientFactory
    {
        IRestClient CreateRestClient(Type entityType);

        IDataRestClient<TData> GetClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData>();

        IDataRestClient<TData, TId> GetClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData, TId>()
            where TData : IHasId<TId>;
    }
}