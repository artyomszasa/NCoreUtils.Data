using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Data.EntityFrameworkCore;

namespace NCoreUtils.Data
{
    public static class ServiceCollectionEntityFrameworkCoreDataRepositoryExtensions
    {
        public static IServiceCollection AddDataRepositoryContext<TContext>(this IServiceCollection services)
            where TContext : DbContext
            => services.AddScoped<DataRepositoryContext<TContext>>();

        public static IServiceCollection AddDefaultDataRepositoryContext<TContext>(this IServiceCollection services)
            where TContext : DbContext
            => services.AddDataRepositoryContext<TContext>()
                .AddScoped<DataRepositoryContext>(provider => provider.GetRequiredService<DataRepositoryContext<TContext>>())
                .AddScoped<IDataRepositoryContext>(provider => provider.GetRequiredService<DataRepositoryContext<TContext>>());

        public static IServiceCollection AddEntityFrameworkCoreDataRepository<TRepository, TData, TId>(this IServiceCollection services)
           where TRepository : class, IDataRepository<TData, TId>
           where TData : class, IHasId<TId>
           => services.AddScoped<TRepository>()
               .AddScoped<IDataRepository<TData, TId>>(serviceProvider => serviceProvider.GetRequiredService<TRepository>())
               .AddScoped<IDataRepository<TData>>(serviceProvider => serviceProvider.GetRequiredService<TRepository>());

        public static IServiceCollection AddEntityFrameworkCoreDataRepository<TData, TId>(this IServiceCollection services)
            where TData : class, IHasId<TId>
            where TId : IComparable<TId>
            => services.AddEntityFrameworkCoreDataRepository<DataRepository<TData, TId>, TData, TId>();

    }
}