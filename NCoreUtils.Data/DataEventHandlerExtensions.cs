using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Data.Events;

namespace NCoreUtils.Data
{
    public static class DataEventHandlerExtensions
    {
        public static IServiceCollection AddDataEventHandlers(this IServiceCollection services)
            => services.AddSingleton<IDataEventHandlers, DataEventHandlers>();

        public static IDataEventHandlers Observe<T>(this IDataEventHandlers handlers, out IDataEventHandler handler)
            where T : DataEventObserver
        {
            handler = DataEventHandler.Create((e, cancellationToken) => ActivatorUtilities.CreateInstance<T>(e.ServiceProvider).HandleAsync(e, cancellationToken));
            handlers.Add(handler);
            return handlers;
        }

        public static IDataEventHandlers Observe<T>(this IDataEventHandlers handlers)
            where T : DataEventObserver
            => handlers.Observe<T>(out var _);

        public static IDataEventHandlers Observe<TEntity, TObserver>(this IDataEventHandlers handlers, out IDataEventHandler handler)
            where TObserver : DataEventObserver<TEntity>
            where TEntity : class
        {
            handler = DataEventHandler.Create((e, cancellationToken) => ActivatorUtilities.CreateInstance<TObserver>(e.ServiceProvider).HandleAsync(e, cancellationToken));
            handlers.Add(handler);
            return handlers;
        }

        public static IDataEventHandlers Observe<TEntity, TObserver>(this IDataEventHandlers handlers)
            where TObserver : DataEventObserver<TEntity>
            where TEntity : class
            => handlers.Observe<TEntity, TObserver>(out var _);


        public static IDataEventHandlers ObserveUpdate<TEntity>(this IDataEventHandlers handlers, Action<TEntity> observer, out IDataEventHandler handler)
            where TEntity : class
        {
            handler = DataEventHandler.CreateUpdateObserverFrom<TEntity>((_, entity, __) =>
            {
                observer(entity);
                return Task.CompletedTask;
            });
            handlers.Add(handler);
            return handlers;
        }

        public static IDataEventHandlers ObserveUpdate<TEntity>(this IDataEventHandlers handlers, Action<TEntity> observer)
            where TEntity : class
            => handlers.ObserveUpdate(observer, out var _);

        public static IDataEventHandlers ObserveInsert<TEntity>(this IDataEventHandlers handlers, Action<TEntity> observer, out IDataEventHandler handler)
            where TEntity : class
        {
            handler = DataEventHandler.CreateInsertObserverFrom<TEntity>((_, entity, __) =>
            {
                observer(entity);
                return Task.CompletedTask;
            });
            handlers.Add(handler);
            return handlers;
        }

        public static IDataEventHandlers ObserveInsert<TEntity>(this IDataEventHandlers handlers, Action<TEntity> observer)
            where TEntity : class
            => handlers.ObserveInsert(observer, out var _);

        public static IDataEventHandlers ObserveDelete<TEntity>(this IDataEventHandlers handlers, Action<TEntity> observer, out IDataEventHandler handler)
            where TEntity : class
        {
            handler = DataEventHandler.CreateDeleteObserverFrom<TEntity>((_, entity, __) =>
            {
                observer(entity);
                return Task.CompletedTask;
            });
            handlers.Add(handler);
            return handlers;
        }

        public static IDataEventHandlers ObserveDelete<TEntity>(this IDataEventHandlers handlers, Action<TEntity> observer)
            where TEntity : class
            => handlers.ObserveDelete(observer, out var _);


        public static IDataEventHandlers ObserveUpdate<TEntity>(this IDataEventHandlers handlers, Func<TEntity, CancellationToken, Task> observer, out IDataEventHandler handler)
            where TEntity : class
        {
            handler = DataEventHandler.CreateUpdateObserverFrom<TEntity>((_, entity, cancellationToken) => observer(entity, cancellationToken));
            handlers.Add(handler);
            return handlers;
        }

        public static IDataEventHandlers ObserveUpdate<TEntity>(this IDataEventHandlers handlers, Func<TEntity, CancellationToken, Task> observer)
            where TEntity : class
            => handlers.ObserveUpdate(observer, out var _);

        public static IDataEventHandlers ObserveInsert<TEntity>(this IDataEventHandlers handlers, Func<TEntity, CancellationToken, Task> observer, out IDataEventHandler handler)
            where TEntity : class
        {
            handler = DataEventHandler.CreateInsertObserverFrom<TEntity>((_, entity, cancellationToken) => observer(entity, cancellationToken));
            handlers.Add(handler);
            return handlers;
        }

        public static IDataEventHandlers ObserveInsert<TEntity>(this IDataEventHandlers handlers, Func<TEntity, CancellationToken, Task> observer)
            where TEntity : class
            => handlers.ObserveInsert(observer, out var _);

        public static IDataEventHandlers ObserveDelete<TEntity>(this IDataEventHandlers handlers, Func<TEntity, CancellationToken, Task> observer, out IDataEventHandler handler)
            where TEntity : class
        {
            handler = DataEventHandler.CreateDeleteObserverFrom<TEntity>((_, entity, cancellationToken) => observer(entity, cancellationToken));
            handlers.Add(handler);
            return handlers;
        }

        public static IDataEventHandlers ObserveDelete<TEntity>(this IDataEventHandlers handlers, Func<TEntity, CancellationToken, Task> observer)
            where TEntity : class
            => handlers.ObserveDelete(observer, out var _);

        public static IDataEventHandlers AddImplicitObservers(this IDataEventHandlers handlers)
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                    assembly.GetTypes()
                        .Where(ty => typeof(IDataEventHandler).IsAssignableFrom(ty) && ty.GetCustomAttribute<ImplicitDataEventObserverAttribute>() != null))
                .ToList();
            foreach (var type in types)
            {
                var handler = Create(type);
                handlers.Add(handler);
            }
            return handlers;

            IDataEventHandler Create(Type type)
            {
                return DataEventHandler.Create((e, token) => {
                    var observer = (IDataEventHandler)ActivatorUtilities.CreateInstance(e.ServiceProvider, type);
                    return observer.HandleAsync(e, token);
                });
            }
        }
    }
}