using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Data.IdNameGeneration;
using NCoreUtils.Text;

namespace NCoreUtils.Data
{
    public static class IdNameGenerationImplementationExtensions
    {

        public static Task<string> GenerateIdNameAsync<T>(
            this IQueryable<T> query,
            IServiceProvider serviceProvider,
            IdNameDescription idNameDescription,
            string name,
            object indexValues,
            CancellationToken cancellationToken = default)
            where T : class, IHasIdName
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }
            var generator = (IIdNameGenerator)(serviceProvider.GetService(typeof(IIdNameGenerator)) ?? ActivatorUtilities.CreateInstance(serviceProvider, typeof(IdNameGenerator)));
            return generator.GenerateAsync(query, idNameDescription, name, indexValues, cancellationToken);
        }

        public static Task<string> GenerateIdNameAsync<T>(
            this IQueryable<T> query,
            IServiceProvider serviceProvider,
            IdNameDescription idNameDescription,
            T entity,
            CancellationToken cancellationToken = default)
            where T : class, IHasIdName
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }
            var generator = (IIdNameGenerator)(serviceProvider.GetService(typeof(IIdNameGenerator)) ?? ActivatorUtilities.CreateInstance(serviceProvider, typeof(IdNameGenerator)));
            return generator.GenerateAsync(query, idNameDescription, entity, cancellationToken);
        }

        public static string GenerateIdName<T>(
            this IQueryable<T> query,
            IServiceProvider serviceProvider,
            IdNameDescription idNameDescription,
            string name,
            object indexValues)
            where T : class, IHasIdName
            => query.GenerateIdNameAsync(serviceProvider, idNameDescription, name, indexValues)
                .GetAwaiter()
                .GetResult();

        public static string GenerateIdName<T>(
            this IQueryable<T> query,
            IServiceProvider serviceProvider,
            IdNameDescription idNameDescription,
            T entity)
            where T : class, IHasIdName
            => query.GenerateIdNameAsync(serviceProvider, idNameDescription, entity)
                .GetAwaiter()
                .GetResult();
    }
}