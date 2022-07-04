using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NCoreUtils.Data.Google.Cloud.Firestore;

namespace NCoreUtils.Data
{
    public static class ServiceCollectionFirestoreDataExtensions
    {
        public static IServiceCollection AddFirestoreDataRepositoryContext(this IServiceCollection services, IFirestoreConfiguration? configuration)
        {
            var conf = services.AddOptions<FirestoreConfiguration>();
            if (configuration is not null)
            {
                conf.Configure(c =>
                {
                    if (!string.IsNullOrEmpty(configuration.ProjectId))
                    {
                        c.ProjectId = configuration.ProjectId;
                    }
                    if (configuration.ConversionOptions is not null)
                    {
                        c.ConversionOptions = configuration.ConversionOptions;
                    }
                });
            }
            services.AddTransient<IFirestoreConfiguration>(serviceProvider => serviceProvider.GetRequiredService<IOptionsMonitor<FirestoreConfiguration>>().CurrentValue);
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

        public static IServiceCollection AddFirestoreDataRepositoryContext(
            this IServiceCollection services,
            string? projectId = default,
            Action<FirestoreConversionOptionsBuilder>? configure = default)
        {
            var builder = new FirestoreConversionOptionsBuilder();
            configure?.Invoke(builder);
            return services.AddFirestoreDataRepositoryContext(new FirestoreConfiguration
            {
                ProjectId = projectId,
                ConversionOptions = builder.ToOptions()
            });
        }

        public static IServiceCollection AddFirestoreDataRepositoryContext(
            this IServiceCollection services,
            Action<FirestoreConversionOptionsBuilder> configure)
            => services.AddFirestoreDataRepositoryContext(default, configure);

        private static TRepository GetRepositoryService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TRepository>(IServiceProvider serviceProvider)
            where TRepository : notnull
            =>  serviceProvider.GetRequiredService<TRepository>();

        public static IServiceCollection AddFirestoreDataRepository<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TRepository, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TData>(this IServiceCollection services)
            where TRepository : FirestoreDataRepository<TData>
            where TData : IHasId<string>
            => services
                .AddScoped<TRepository>()
                .AddScoped<IDataRepository<TData, string>>(GetRepositoryService<TRepository>)
                .AddScoped<IDataRepository<TData>>(GetRepositoryService<TRepository>);

        [UnconditionalSuppressMessage("Trimming", "IL2111")]
        [DynamicDependency("PopulateDTO", typeof(FirestoreDataRepository))]
        public static IServiceCollection AddFirestoreDataRepository<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TData>(this IServiceCollection services)
            where TData : IHasId<string>
            => services.AddFirestoreDataRepository<FirestoreDataRepository<TData>, TData>();
    }
}