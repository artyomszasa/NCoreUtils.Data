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
    }
}