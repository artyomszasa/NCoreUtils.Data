using System;
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

        public static IServiceCollection AddRestDataRepository<TRepository, TData, TId>(this IServiceCollection services, IRestClientConfiguration configuration)
            where TRepository : RestDataRepository<TData, TId>
            where TData : IHasId<TId>
            => services
                .AddRestDataRepositoryServices()
                .AddRestDataRepositoryContext()
                .ConfigureRest(b => b.Add((typeof(TData), typeof(TId), configuration)))
                .AddScoped<TRepository>()
                .AddScoped<IDataRepository<TData, TId>>(serviceProvider => serviceProvider.GetRequiredService<TRepository>())
                .AddScoped<IDataRepository<TData>>(serviceProvider => serviceProvider.GetRequiredService<TRepository>());

        public static IServiceCollection AddRestDataRepository<TData, TId>(this IServiceCollection services, IRestClientConfiguration configuration)
            where TData : IHasId<TId>
            => services.AddRestDataRepository<RestDataRepository<TData, TId>, TData, TId>(configuration);

        public static IServiceCollection AddRestDataRepository<TRepository, TData, TId>(this IServiceCollection services, string endpoint, string httpClient = RestClientConfiguration.DefaultHttpClient)
            where TRepository : RestDataRepository<TData, TId>
            where TData : IHasId<TId>
            => services.AddRestDataRepository<TRepository, TData, TId>(new RestClientConfiguration { Endpoint = endpoint, HttpClient = httpClient });

        public static IServiceCollection AddRestDataRepository<TData, TId>(this IServiceCollection services, string endpoint, string httpClient = RestClientConfiguration.DefaultHttpClient)
            where TData : IHasId<TId>
            => services.AddRestDataRepository<RestDataRepository<TData, TId>, TData, TId>(new RestClientConfiguration { Endpoint = endpoint, HttpClient = httpClient });

        public static IServiceCollection AddRestDataRepository<TRepository, TData, TId>(this IServiceCollection services, IConfiguration configuration)
            where TRepository : RestDataRepository<TData, TId>
            where TData : IHasId<TId>
            => services.AddRestDataRepository<TRepository, TData, TId>(configuration.Get<RestClientConfiguration>() ?? throw new InvalidOperationException($"No REST client configuration found for {typeof(TData)}."));
    }
}