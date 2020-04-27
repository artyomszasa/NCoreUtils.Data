using System;
using System.Linq;
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
            services.TryAddSingleton<DataRestClientCache>();
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
            where TRepository : DataRepository<TData, TId>
            where TData : IHasId<TId>
            => services
                .AddRestDataRepositoryServices()
                .AddRestDataRepositoryContext()
                .ConfigureRest(b => b.Add((typeof(TData), typeof(TId), configuration)))
                .AddScoped<TRepository>()
                .AddScoped<IDataRepository<TData, TId>>(serviceProvider => serviceProvider.GetRequiredService<TRepository>())
                .AddScoped<IDataRepository<TData>>(serviceProvider => serviceProvider.GetRequiredService<TRepository>());
    }
}