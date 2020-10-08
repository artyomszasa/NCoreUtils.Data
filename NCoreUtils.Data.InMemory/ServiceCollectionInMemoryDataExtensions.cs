using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Data.InMemory;

namespace NCoreUtils.Data
{
    public static class ServiceCollectionInMemoryDataExtensions
    {
        public static IServiceCollection AddInMemoryDataRepositoryContext(this IServiceCollection services)
        {
            return services
                .AddSingleton<InMemoryDataRepositoryContext>();
        }

        public static IServiceCollection AddInMemoryDataRepository<TData, TId>(this IServiceCollection services, IList<TData>? data = default)
            where TData : class, IHasId<TId>
            where TId : IEquatable<TId>
        {
            return services
                .AddSingleton(sp => new InMemoryDataRepository<TData, TId>(
                    sp,
                    sp.GetRequiredService<InMemoryDataRepositoryContext>(),
                    sp.GetService<IDataEventHandlers>(),
                    data)
                )
                .AddSingleton<IDataRepository<TData, TId>>(sp => sp.GetRequiredService<InMemoryDataRepository<TData, TId>>())
                .AddSingleton<IDataRepository<TData>>(sp => sp.GetRequiredService<InMemoryDataRepository<TData, TId>>());
        }
    }
}