using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NCoreUtils.Data.Rest;
using NCoreUtils.Rest;

namespace NCoreUtils.Data
{
    public static class ServiceCollectionDataRestExtensions
    {
        private static DataRestConfigurationBuilder GetOrAddRestConfigurationBuilder(this IServiceCollection services)
        {
            var desc = services.FirstOrDefault(e => e.ServiceType == typeof(DataRestConfigurationBuilder));
            if (null == desc)
            {
                var builder = new DataRestConfigurationBuilder();
                services.AddSingleton(builder);
                return builder;
            }
            if (desc.ImplementationInstance is null)
            {
                throw new InvalidOperationException("Incompatible implementation of DataRestConfigurationBuilder haas been registered.");
            }
            return (DataRestConfigurationBuilder)desc.ImplementationInstance;
        }

        private static IServiceCollection ConfigureRest(this IServiceCollection services, Action<DataRestConfigurationBuilder> configure)
        {
            var builder = services.GetOrAddRestConfigurationBuilder();
            configure(builder);
            return services;
        }

        private static IServiceCollection AddRestDataRepositoryServices(this IServiceCollection services)
        {
            services.AddRestClientServices();
            services.TryAddSingleton(serviceProvider => new DataRestConfiguration(serviceProvider.GetService<DataRestConfigurationBuilder>() ?? new DataRestConfigurationBuilder()));
            services.TryAddSingleton<IDataRestClientCache, DataRestClientCache>();
            services.TryAddScoped<IDataRestClientFactory, DataRestClientFactory>();
            services.TryAddScoped(typeof(IDataRestClient<,>), typeof(DataRestClientFactory.DataRestClient<,>));
            return services;
        }

        public static IServiceCollection AddRestDataRepositoryContext(this IServiceCollection services)
        {
            services.TryAddScoped<RestDataRepositoryContext>();
            services.TryAddScoped<IDataRepositoryContext>(serviceProvider => serviceProvider.GetRequiredService<RestDataRepositoryContext>());
            return services;
        }

        private static TRepository GetRequiredService<TRepository, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData, TId>(
            IServiceProvider serviceProvider)
            where TRepository : RestDataRepository<TData, TId>
            where TData : IHasId<TId>
            => serviceProvider.GetRequiredService<TRepository>();

        private sealed class AddTypesToBuilder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData, TId>
        {
            private IRestClientConfiguration Configuration { get; }

            public AddTypesToBuilder(IRestClientConfiguration configuration)
            {
                Configuration = configuration;
            }

            public void Add(DataRestConfigurationBuilder b)
                => b.Add((typeof(TData), typeof(TId), Configuration));
        }

        public static IServiceCollection AddRestDataRepository<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TRepository,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData,
            TId>(
            this IServiceCollection services,
            IRestClientConfiguration configuration)
            where TRepository : RestDataRepository<TData, TId>
            where TData : IHasId<TId>
            => services
                .AddRestDataRepositoryServices()
                .AddRestDataRepositoryContext()
                .ConfigureRest(new AddTypesToBuilder<TData, TId>(configuration).Add)
                .AddScoped<TRepository>()
                .AddScoped<IDataRepository<TData, TId>>(GetRequiredService<TRepository, TData, TId>)
                .AddScoped<IDataRepository<TData>>(GetRequiredService<TRepository, TData, TId>);

        public static IServiceCollection AddRestDataRepository<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData, TId>(
            this IServiceCollection services,
            IRestClientConfiguration configuration)
            where TData : IHasId<TId>
            => services.AddRestDataRepository<RestDataRepository<TData, TId>, TData, TId>(configuration);

        public static IServiceCollection AddRestDataRepository<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TRepository,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData,
            TId>(
            this IServiceCollection services,
            string endpoint,
            string httpClient = RestClientConfiguration.DefaultHttpClient)
            where TRepository : RestDataRepository<TData, TId>
            where TData : IHasId<TId>
            => services.AddRestDataRepository<TRepository, TData, TId>(
                new RestClientConfiguration { Endpoint = endpoint, HttpClient = httpClient }
            );

        public static IServiceCollection AddRestDataRepository<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData, TId>(
            this IServiceCollection services,
            string endpoint,
            string httpClient = RestClientConfiguration.DefaultHttpClient)
            where TData : IHasId<TId>
            => services.AddRestDataRepository<RestDataRepository<TData, TId>, TData, TId>(
                new RestClientConfiguration { Endpoint = endpoint, HttpClient = httpClient }
            );

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RestClientConfiguration))]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Configuration type is preserved using dynamic dependency.")]
        public static IServiceCollection AddRestDataRepository<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TRepository,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData,
            TId>(
            this IServiceCollection services,
            IConfiguration configuration)
            where TRepository : RestDataRepository<TData, TId>
            where TData : IHasId<TId>
            => services.AddRestDataRepository<TRepository, TData, TId>(
                configuration.Get<RestClientConfiguration>()
                ?? throw new InvalidOperationException($"No REST client configuration found for {typeof(TData)}.")
            );
    }
}