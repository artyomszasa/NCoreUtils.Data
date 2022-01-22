using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Data.EntityFrameworkCore;

namespace NCoreUtils.Data
{
    public static class ServiceCollectionEntityFrameworkCoreDataRepositoryExtensions
    {
        private static IDataRepository<TData, TId> GetDownCasted<TRepository, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData, TId>(
            IServiceProvider serviceProvider)
            where TRepository : class, IDataRepository<TData, TId>
            where TData : class, IHasId<TId>
            => serviceProvider.GetRequiredService<TRepository>();

        public static IServiceCollection AddDataRepositoryContext<TContext>(this IServiceCollection services)
            where TContext : DbContext
            => services.AddScoped<DataRepositoryContext<TContext>>();

        public static IServiceCollection AddDefaultDataRepositoryContext<TContext>(this IServiceCollection services)
            where TContext : DbContext
            => services.AddDataRepositoryContext<TContext>()
                .AddScoped<DataRepositoryContext>(provider => provider.GetRequiredService<DataRepositoryContext<TContext>>())
                .AddScoped<IDataRepositoryContext>(provider => provider.GetRequiredService<DataRepositoryContext<TContext>>());

        public static IServiceCollection AddEntityFrameworkCoreDataRepository<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TRepository, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData, TId>(
            this IServiceCollection services)
            where TRepository : class, IDataRepository<TData, TId>
            where TData : class, IHasId<TId>
            => services.AddScoped<TRepository>()
                .AddScoped(GetDownCasted<TRepository, TData, TId>)
                .AddScoped<IDataRepository<TData>>(GetDownCasted<TRepository, TData, TId>);

        public static IServiceCollection AddEntityFrameworkCoreDataRepository<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData, TId>(this IServiceCollection services)
            where TData : class, IHasId<TId>
            where TId : IComparable<TId>
            => services.AddEntityFrameworkCoreDataRepository<DataRepository<TData, TId>, TData, TId>();

    }
}