using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Data.Google.FireStore;

namespace NCoreUtils.Data
{
    public static class ServiceCollectionGoogleFireStoreExtensions
    {
        public static IServiceCollection AddFireStoreContext<TFactory, TContext>(this IServiceCollection services, IFireStoreConfiguration configuration)
            where TFactory : FireStoreDbFactory
            where TContext : DataRepositoryContext
        {
            services.AddSingleton(configuration);
            services.AddSingleton<FireStoreDbFactory, TFactory>();
            services.AddScoped<TContext>();
            if (typeof(TContext) != typeof(DataRepositoryContext))
            {
                services.AddScoped<DataRepositoryContext>(serviceProvider => serviceProvider.GetRequiredService<TContext>());
            }
            services.AddScoped<IDataRepositoryContext>(serviceProvider => serviceProvider.GetRequiredService<TContext>());
            return services;
        }

        public static IServiceCollection AddFireStoreContext<TFactory>(this IServiceCollection services, IFireStoreConfiguration configuration)
            where TFactory : FireStoreDbFactory
            => services.AddFireStoreContext<TFactory, DataRepositoryContext>(configuration);

        public static IServiceCollection AddFireStoreContext<TFactory>(this IServiceCollection services, IConfiguration configuration)
            where TFactory : FireStoreDbFactory
        {
            var config = new FireStoreConfiguration();
            configuration.Bind(config);
            return services.AddFireStoreContext<TFactory, DataRepositoryContext>(config);
        }

        public static IServiceCollection AddFireStoreContext<TFactory>(this IServiceCollection services, string projectId)
            where TFactory : FireStoreDbFactory
        {
            var config = new FireStoreConfiguration { ProjectId = projectId };
            return services.AddFireStoreContext<TFactory, DataRepositoryContext>(config);
        }

        public static IServiceCollection AddFireStoreRepository<TRepository, TData>(this IServiceCollection services)
            where TRepository : DataRepository<TData>
            where TData : IHasId<string>
        {
            services.AddScoped<TRepository>();
            if (typeof(TRepository) != typeof(DataRepository<TData>))
            {
                services.AddScoped<DataRepository<TData>>(serviceProvider => serviceProvider.GetRequiredService<TRepository>());
            }
            services.AddScoped<IDataRepository<TData, string>>(serviceProvider => serviceProvider.GetRequiredService<TRepository>());
            services.AddScoped<IDataRepository<TData>>(serviceProvider => serviceProvider.GetRequiredService<TRepository>());
            return services;
        }

        public static IServiceCollection AddFireStoreRepository<TData>(this IServiceCollection services)
            where TData : IHasId<string>
            => services.AddFireStoreRepository<DataRepository<TData>, TData>();
    }
}