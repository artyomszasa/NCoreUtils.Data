using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Data.IdNameGeneration;

namespace NCoreUtils.Data
{
    public static class ServiceCollectionSqlIdNameGenerationExtensions
    {
        public static IServiceCollection AddSqlIdNameGeneration<TDbContext>(this IServiceCollection services)
            where TDbContext : DbContext
        {
            return services
                .AddScoped<IdNameGenerationInitialization, IdNameGenerationInitialization<TDbContext>>()
                .AddScoped<IIdNameGenerator, SqlIdNameGenerator<TDbContext>>();
        }
    }
}