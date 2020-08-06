using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Data.Events;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Provides extensions methods for managing data repository related event handlers.
    /// </summary>
    public static class DataEventHandlerExtensions
    {
        sealed class DataEventHandlerFactory : IDataEventHandler
        {
            readonly Type _targetHandlerType;

            public DataEventHandlerFactory(Type targetHandlerType)
            {
                _targetHandlerType = targetHandlerType;
            }

            public ValueTask HandleAsync(IDataEvent @event, CancellationToken cancellationToken = default)
            {
                var observer = (IDataEventHandler)ActivatorUtilities.CreateInstance(@event.ServiceProvider, _targetHandlerType);
                return observer.HandleAsync(@event, cancellationToken);
            }

            [ExcludeFromCodeCoverage]
            public override string ToString() => $"DataEventHandlerFactory({_targetHandlerType})";
        }

        /// <summary>
        /// Adds default implementation of data event handlers.
        /// </summary>
        /// <param name="services">Service collection.</param>
        public static IServiceCollection AddDataEventHandlers(this IServiceCollection services)
            => services.AddSingleton<IDataEventHandlers, DataEventHandlers>();

        /// <summary>
        /// Creates data event handler that when invoked creates new instance of the specified observer and passes data
        /// event to the observer, then adds newly created handler to the data event handler collection. The created
        /// handler is returned into variable specified by <paramref name="handler" /> parameter.
        /// </summary>
        /// <typeparam name="T">Type of the data event observer to use.</typeparam>
        /// <param name="handlers">Handler collection.</param>
        /// <param name="handler">Created handler.</param>
        /// <returns>Handler collection.</returns>
        public static IDataEventHandlers Observe<T>(this IDataEventHandlers handlers, out IDataEventHandler handler)
            where T : DataEventObserver
        {
            handler = DataEventHandler.Create((e, cancellationToken) => ActivatorUtilities.CreateInstance<T>(e.ServiceProvider).HandleAsync(e, cancellationToken));
            handlers.Add(handler);
            return handlers;
        }

        /// <summary>
        /// Creates data event handler that when invoked creates new instance of the specified observer and passes data
        /// event to the observer, then adds newly created handler to the data event handler collection.
        /// </summary>
        /// <typeparam name="T">Type of the data event observer to use.</typeparam>
        /// <param name="handlers">Handler collection.</param>
        /// <returns>Handler collection.</returns>
        public static IDataEventHandlers Observe<T>(this IDataEventHandlers handlers)
            where T : DataEventObserver
            => handlers.Observe<T>(out var _);

        /// <summary>
        /// Creates data event handler that when invoked creates new instance of the specified observer and passes data
        /// event to the observer, then adds newly created handler to the data event handler collection. The created
        /// handler is returned into variable specified by <paramref name="handler" /> parameter.
        /// </summary>
        /// <typeparam name="TObserver">Type of the typed data event observer to use.</typeparam>
        /// <typeparam name="TEntity">Type of data entity handled by the observer.</typeparam>
        /// <param name="handlers">Handler collection.</param>
        /// <param name="handler">Created handler.</param>
        /// <returns>Handler collection.</returns>
        public static IDataEventHandlers Observe<TEntity, TObserver>(this IDataEventHandlers handlers, out IDataEventHandler handler)
            where TObserver : DataEventObserver<TEntity>
            where TEntity : class
        {
            handler = DataEventHandler.Create((e, cancellationToken) => ActivatorUtilities.CreateInstance<TObserver>(e.ServiceProvider).HandleAsync(e, cancellationToken));
            handlers.Add(handler);
            return handlers;
        }

        /// <summary>
        /// Creates data event handler that when invoked creates new instance of the specified observer and passes data
        /// event to the observer, then adds newly created handler to the data event handler collection.
        /// </summary>
        /// <typeparam name="TObserver">Type of the typed data event observer to use.</typeparam>
        /// <typeparam name="TEntity">Type of data entity handled by the observer.</typeparam>
        /// <param name="handlers">Handler collection.</param>
        /// <returns>Handler collection.</returns>
        public static IDataEventHandlers Observe<TEntity, TObserver>(this IDataEventHandlers handlers)
            where TObserver : DataEventObserver<TEntity>
            where TEntity : class
            => handlers.Observe<TEntity, TObserver>(out var _);


        /// <summary>
        /// Creates data event handler that when invoked on <c>Update</c> operation, invokes the specified function,
        /// then adds newly created handler to the data event handler collection. The created handler is returned into
        /// variable specified by <paramref name="handler" /> parameter.
        /// </summary>
        /// <typeparam name="TEntity">Type of data entity handled by the observer.</typeparam>
        /// <param name="handlers">Handler collection.</param>
        /// <param name="observer">Observer function.</param>
        /// <param name="handler">Created handler.</param>
        /// <returns>Handler collection.</returns>
        public static IDataEventHandlers ObserveUpdate<TEntity>(this IDataEventHandlers handlers, Action<TEntity> observer, out IDataEventHandler handler)
            where TEntity : class
        {
            handler = DataEventHandler.CreateUpdateObserverFrom<TEntity>((_, entity, __) =>
            {
                observer(entity);
                return default;
            });
            handlers.Add(handler);
            return handlers;
        }

        /// <summary>
        /// Creates data event handler that when invoked on <c>Update</c> operation, invokes the specified function,
        /// then adds newly created handler to the data event handler collection.
        /// </summary>
        /// <typeparam name="TEntity">Type of data entity handled by the observer.</typeparam>
        /// <param name="handlers">Handler collection.</param>
        /// <param name="observer">Observer function.</param>
        /// <returns>Handler collection.</returns>
        public static IDataEventHandlers ObserveUpdate<TEntity>(this IDataEventHandlers handlers, Action<TEntity> observer)
            where TEntity : class
            => handlers.ObserveUpdate(observer, out var _);

        /// <summary>
        /// Creates data event handler that when invoked on <c>Insert</c> operation, invokes the specified function,
        /// then adds newly created handler to the data event handler collection. The created handler is returned into
        /// variable specified by <paramref name="handler" /> parameter.
        /// </summary>
        /// <typeparam name="TEntity">Type of data entity handled by the observer.</typeparam>
        /// <param name="handlers">Handler collection.</param>
        /// <param name="observer">Observer function.</param>
        /// <param name="handler">Created handler.</param>
        /// <returns>Handler collection.</returns>
        public static IDataEventHandlers ObserveInsert<TEntity>(this IDataEventHandlers handlers, Action<TEntity> observer, out IDataEventHandler handler)
            where TEntity : class
        {
            handler = DataEventHandler.CreateInsertObserverFrom<TEntity>((_, entity, __) =>
            {
                observer(entity);
                return default;
            });
            handlers.Add(handler);
            return handlers;
        }

        /// <summary>
        /// Creates data event handler that when invoked on <c>Insert</c> operation, invokes the specified function,
        /// then adds newly created handler to the data event handler collection.
        /// </summary>
        /// <typeparam name="TEntity">Type of data entity handled by the observer.</typeparam>
        /// <param name="handlers">Handler collection.</param>
        /// <param name="observer">Observer function.</param>
        /// <returns>Handler collection.</returns>
        public static IDataEventHandlers ObserveInsert<TEntity>(this IDataEventHandlers handlers, Action<TEntity> observer)
            where TEntity : class
            => handlers.ObserveInsert(observer, out var _);

        /// <summary>
        /// Creates data event handler that when invoked on <c>Delete</c> operation, invokes the specified function,
        /// then adds newly created handler to the data event handler collection. The created handler is returned into
        /// variable specified by <paramref name="handler" /> parameter.
        /// </summary>
        /// <typeparam name="TEntity">Type of data entity handled by the observer.</typeparam>
        /// <param name="handlers">Handler collection.</param>
        /// <param name="observer">Observer function.</param>
        /// <param name="handler">Created handler.</param>
        /// <returns>Handler collection.</returns>
        public static IDataEventHandlers ObserveDelete<TEntity>(this IDataEventHandlers handlers, Action<TEntity> observer, out IDataEventHandler handler)
            where TEntity : class
        {
            handler = DataEventHandler.CreateDeleteObserverFrom<TEntity>((_, entity, __) =>
            {
                observer(entity);
                return default;
            });
            handlers.Add(handler);
            return handlers;
        }

        /// <summary>
        /// Creates data event handler that when invoked on <c>Delete</c> operation, invokes the specified function,
        /// then adds newly created handler to the data event handler collection.
        /// </summary>
        /// <typeparam name="TEntity">Type of data entity handled by the observer.</typeparam>
        /// <param name="handlers">Handler collection.</param>
        /// <param name="observer">Observer function.</param>
        /// <returns>Handler collection.</returns>
        public static IDataEventHandlers ObserveDelete<TEntity>(this IDataEventHandlers handlers, Action<TEntity> observer)
            where TEntity : class
            => handlers.ObserveDelete(observer, out var _);


        /// <summary>
        /// Creates data event handler that when invoked on <c>Update</c> operation, invokes the specified function,
        /// then adds newly created handler to the data event handler collection. The created handler is returned into
        /// variable specified by <paramref name="handler" /> parameter.
        /// </summary>
        /// <typeparam name="TEntity">Type of data entity handled by the observer.</typeparam>
        /// <param name="handlers">Handler collection.</param>
        /// <param name="observer">Observer function.</param>
        /// <param name="handler">Created handler.</param>
        /// <returns>Handler collection.</returns>
        public static IDataEventHandlers ObserveUpdate<TEntity>(this IDataEventHandlers handlers, Func<TEntity, CancellationToken, ValueTask> observer, out IDataEventHandler handler)
            where TEntity : class
        {
            handler = DataEventHandler.CreateUpdateObserverFrom<TEntity>((_, entity, cancellationToken) => observer(entity, cancellationToken));
            handlers.Add(handler);
            return handlers;
        }

        /// <summary>
        /// Creates data event handler that when invoked on <c>Update</c> operation, invokes the specified function,
        /// then adds newly created handler to the data event handler collection.
        /// </summary>
        /// <typeparam name="TEntity">Type of data entity handled by the observer.</typeparam>
        /// <param name="handlers">Handler collection.</param>
        /// <param name="observer">Observer function.</param>
        /// <returns>Handler collection.</returns>
        public static IDataEventHandlers ObserveUpdate<TEntity>(this IDataEventHandlers handlers, Func<TEntity, CancellationToken, ValueTask> observer)
            where TEntity : class
            => handlers.ObserveUpdate(observer, out var _);

        /// <summary>
        /// Creates data event handler that when invoked on <c>Insert</c> operation, invokes the specified function,
        /// then adds newly created handler to the data event handler collection. The created handler is returned into
        /// variable specified by <paramref name="handler" /> parameter.
        /// </summary>
        /// <typeparam name="TEntity">Type of data entity handled by the observer.</typeparam>
        /// <param name="handlers">Handler collection.</param>
        /// <param name="observer">Observer function.</param>
        /// <param name="handler">Created handler.</param>
        /// <returns>Handler collection.</returns>
        public static IDataEventHandlers ObserveInsert<TEntity>(this IDataEventHandlers handlers, Func<TEntity, CancellationToken, ValueTask> observer, out IDataEventHandler handler)
            where TEntity : class
        {
            handler = DataEventHandler.CreateInsertObserverFrom<TEntity>((_, entity, cancellationToken) => observer(entity, cancellationToken));
            handlers.Add(handler);
            return handlers;
        }

        /// <summary>
        /// Creates data event handler that when invoked on <c>Insert</c> operation, invokes the specified function,
        /// then adds newly created handler to the data event handler collection.
        /// </summary>
        /// <typeparam name="TEntity">Type of data entity handled by the observer.</typeparam>
        /// <param name="handlers">Handler collection.</param>
        /// <param name="observer">Observer function.</param>
        /// <returns>Handler collection.</returns>
        public static IDataEventHandlers ObserveInsert<TEntity>(this IDataEventHandlers handlers, Func<TEntity, CancellationToken, ValueTask> observer)
            where TEntity : class
            => handlers.ObserveInsert(observer, out var _);

        /// <summary>
        /// Creates data event handler that when invoked on <c>Delete</c> operation, invokes the specified function,
        /// then adds newly created handler to the data event handler collection. The created handler is returned into
        /// variable specified by <paramref name="handler" /> parameter.
        /// </summary>
        /// <typeparam name="TEntity">Type of data entity handled by the observer.</typeparam>
        /// <param name="handlers">Handler collection.</param>
        /// <param name="observer">Observer function.</param>
        /// <param name="handler">Created handler.</param>
        /// <returns>Handler collection.</returns>
        public static IDataEventHandlers ObserveDelete<TEntity>(this IDataEventHandlers handlers, Func<TEntity, CancellationToken, ValueTask> observer, out IDataEventHandler handler)
            where TEntity : class
        {
            handler = DataEventHandler.CreateDeleteObserverFrom<TEntity>((_, entity, cancellationToken) => observer(entity, cancellationToken));
            handlers.Add(handler);
            return handlers;
        }

        /// <summary>
        /// Creates data event handler that when invoked on <c>Delete</c> operation, invokes the specified function,
        /// then adds newly created handler to the data event handler collection.
        /// </summary>
        /// <typeparam name="TEntity">Type of data entity handled by the observer.</typeparam>
        /// <param name="handlers">Handler collection.</param>
        /// <param name="observer">Observer function.</param>
        /// <returns>Handler collection.</returns>
        public static IDataEventHandlers ObserveDelete<TEntity>(this IDataEventHandlers handlers, Func<TEntity, CancellationToken, ValueTask> observer)
            where TEntity : class
            => handlers.ObserveDelete(observer, out var _);

        /// <summary>
        /// Registered all observers found in all currently loaded assemblies which are marked as implcit observers.
        /// </summary>
        /// <param name="handlers"></param>
        /// <returns></returns>
        public static IDataEventHandlers AddImplicitObservers(this IDataEventHandlers handlers)
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes()
                            .Where(ty => typeof(IDataEventHandler).IsAssignableFrom(ty)
                                    && ty.GetCustomAttribute<ImplicitDataEventObserverAttribute>() != null);
                    }
                    catch
                    {
                        // TODO: log
                        return Enumerable.Empty<Type>();
                    }
                })
                .ToList();
            foreach (var type in types)
            {
                handlers.Add(new DataEventHandlerFactory(type));
            }
            return handlers;
        }
    }
}