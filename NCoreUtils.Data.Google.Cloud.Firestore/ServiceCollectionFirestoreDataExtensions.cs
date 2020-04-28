using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NCoreUtils.Data.Google.Cloud.Firestore;

namespace NCoreUtils.Data
{
    public static class ServiceCollectionFirestoreDataExtensions
    {
        public static IServiceCollection AddFirestoreDataRepositoryContext(this IServiceCollection services, FirestoreConfiguration configuration)
        {
            services.AddSingleton(configuration);
            services.TryAddSingleton<FirestoreDbFactory>();
            services.TryAddScoped(serviceProvider => serviceProvider.GetRequiredService<FirestoreDbFactory>().GetOrCreateFirestoreDb());
            services.TryAddSingleton<FirestoreModel>();
            services.TryAddSingleton<FirestoreMaterializer>();
            services.TryAddScoped<FirestoreQueryProvider>();
            services.TryAddScoped<FirestoreDataRepositoryContext>();
            services.TryAddScoped<IDataRepositoryContext>(serviceProvider => serviceProvider.GetRequiredService<FirestoreDataRepositoryContext>());
            services.TryAddScoped<IFirestoreDbAccessor>(serviceProvider => serviceProvider.GetRequiredService<FirestoreDataRepositoryContext>());
            return services;
        }

        public static IServiceCollection AddFirestoreDataRepository<TRepository, TData>(this IServiceCollection services)
            where TRepository : FirestoreDataRepository<TData>
            where TData : IHasId<string>
            => services
                .AddScoped<TRepository>()
                .AddScoped<IDataRepository<TData, string>>(serviceProvider => serviceProvider.GetRequiredService<TRepository>())
                .AddScoped<IDataRepository<TData>>(serviceProvider => serviceProvider.GetRequiredService<TRepository>());

        public static IServiceCollection AddFirestoreDataRepository<TData>(this IServiceCollection services)
            where TData : IHasId<string>
            => services.AddFirestoreDataRepository<FirestoreDataRepository<TData>, TData>();
    }
}