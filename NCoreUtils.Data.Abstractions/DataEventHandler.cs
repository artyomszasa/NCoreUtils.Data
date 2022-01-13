using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data.Events;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Defines methods for creating data event handlers.
    /// </summary>
    public static class DataEventHandler
    {
        /// <summary>
        /// Used to mask operations when creating data event handlers.
        /// </summary>
        [Flags]
        public enum TargetOperation
        {
            /// Data entity is being inserted into data repository.
            Insert = 0,
            /// Data entity is being update in data repository.
            Update = 1,
            /// Data entity is being deleted from data repository.
            Delete = 2
        }

        sealed class GenericObserver : IDataEventHandler
        {
            public Func<IDataEvent, CancellationToken, ValueTask> Observer { get; }

            public GenericObserver(Func<IDataEvent, CancellationToken, ValueTask> observer) => Observer = observer;

            public ValueTask HandleAsync(IDataEvent @event, CancellationToken cancellationToken = default)
                => Observer(@event, cancellationToken);
        }

        private sealed class TypeObserver : IDataEventHandler
        {
            public TargetOperation ObservedOperations { get; }

            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
            public Type ObservedType { get; }

            public Func<IDataEvent, CancellationToken, ValueTask> Observer { get; }

            public TypeObserver(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type observedType,
                TargetOperation observedOperations,
                Func<IDataEvent, CancellationToken, ValueTask> observer)
            {
                ObservedOperations = observedOperations;
                ObservedType = observedType;
                Observer = observer;
            }

            public ValueTask HandleAsync(IDataEvent @event, CancellationToken cancellationToken = default)
            {
                if (ObservedOperations.HasFlag((TargetOperation)@event.Operation) && @event.EntityType.Equals(ObservedType))
                {
                   return Observer(@event, cancellationToken);
                }
                return default;
            }
        }

        private sealed class TypeObserver<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T> : IDataEventHandler
            where T : class
        {
            public TargetOperation ObservedOperations { get; }

            public Func<IDataEvent<T>, CancellationToken, ValueTask> Observer { get; }

            public TypeObserver(TargetOperation observedOperations, Func<IDataEvent<T>, CancellationToken, ValueTask> observer)
            {
                ObservedOperations = observedOperations;
                Observer = observer;
            }

            public ValueTask HandleAsync(IDataEvent @event, CancellationToken cancellationToken = default)
            {
                if (ObservedOperations.HasFlag((TargetOperation)@event.Operation) && @event is IDataEvent<T> e)
                {
                   return Observer(e, cancellationToken);
                }
                return default;
            }
        }

        private sealed class OperationObserver : IDataEventHandler
        {
            public DataOperation Operation { get; }

            public Func<IDataEvent, CancellationToken, ValueTask> Observer { get; }

            public OperationObserver(DataOperation operation, Func<IDataEvent, CancellationToken, ValueTask> observer)
            {
                Operation = operation;
                Observer = observer;
            }

            public ValueTask HandleAsync(IDataEvent @event, CancellationToken cancellationToken = default)
            {
                if (@event.Operation == Operation)
                {
                   return Observer(@event, cancellationToken);
                }
                return default;
            }
        }

        private sealed class OperationObserver<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData, TEvent> : IDataEventHandler
            where TData : class
            where TEvent : IDataEvent
        {
            public Func<TEvent, CancellationToken, ValueTask> Observer { get; }

            public OperationObserver(Func<TEvent, CancellationToken, ValueTask> observer)
            {
                Observer = observer;
            }

            public ValueTask HandleAsync(IDataEvent @event, CancellationToken cancellationToken = default)
            {
                if (@event is TEvent e)
                {
                   return Observer(e, cancellationToken);
                }
                return default;
            }
        }

        /// <summary>
        /// Creates data event handler from the specified function.
        /// </summary>
        /// <param name="observer">Data event pbserver function.</param>
        /// <returns>Newly created data event handler.</returns>
        public static IDataEventHandler Create(Func<IDataEvent, CancellationToken, ValueTask> observer)
            => new GenericObserver(observer);

        /// <summary>
        /// Creates data event handler from the specified function that will be invoked only for the specified
        /// operations and entity type.
        /// </summary>
        /// <param name="entityType">Type of the data entity being handled by the observer function.</param>
        /// <param name="operations">Operations being handled by the observer function.</param>
        /// <param name="observer">Data event observer function.</param>
        /// <returns>Newly created data event handler.</returns>
        public static IDataEventHandler CreateObserver(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type entityType,
            TargetOperation operations,
            Func<IDataEvent, CancellationToken, ValueTask> observer)
        {
            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }
            return new TypeObserver(entityType, operations, observer);
        }

        /// <summary>
        /// Creates data event handler from the specified function that will be invoked only for the specified entity
        /// type (all operations).
        /// </summary>
        /// <param name="entityType">Type of the data entity being handled by the observer function.</param>
        /// <param name="observer">Data event observer function.</param>
        /// <returns>Newly created data event handler.</returns>
        public static IDataEventHandler CreateObserver(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type entityType,
            Func<IDataEvent, CancellationToken, ValueTask> observer)
            => CreateObserver(entityType, TargetOperation.Insert | TargetOperation.Update | TargetOperation.Delete, observer);

        /// <summary>
        /// Creates data event handler from the specified function that will be invoked only for the specified
        /// operations and entity type.
        /// </summary>
        /// <typeparam name="T">Type of the data entity being handled by the observer function.</typeparam>
        /// <param name="operations">Operations being handled by the observer function.</param>
        /// <param name="observer">Data event observer function.</param>
        /// <returns>Newly created data event handler.</returns>
        public static IDataEventHandler CreateObserver<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
            TargetOperation operations,
            Func<IDataEvent<T>, CancellationToken, ValueTask> observer)
            where T : class
        {
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }
            return new TypeObserver<T>(operations, observer);
        }

        /// <summary>
        /// Creates data event handler from the specified function that will be invoked only for the specified entity
        /// type (all operations).
        /// </summary>
        /// <typeparam name="T">Type of the data entity being handled by the observer function.</typeparam>
        /// <param name="observer">Data event observer function.</param>
        /// <returns>Newly created data event handler.</returns>
        public static IDataEventHandler CreateObserver<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
            Func<IDataEvent<T>, CancellationToken, ValueTask> observer)
            where T : class
            => CreateObserver(TargetOperation.Insert | TargetOperation.Update | TargetOperation.Delete, observer);

        /// <summary>
        /// Creates data event handler from the specified function that will be invoked only for the specified
        /// operation.
        /// </summary>
        /// <param name="operation">Operation being handled by the observer function.</param>
        /// <param name="observer">Data event observer function.</param>
        /// <returns>Newly created data event handler.</returns>
        public static IDataEventHandler CreateOperationObserver(DataOperation operation, Func<IDataEvent, CancellationToken, ValueTask> observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }
            return new OperationObserver(operation, observer);
        }

        /// <summary>
        /// Creates data event handler from the specified function that will be invoked only for the <c>Update</c>
        /// operation.
        /// </summary>
        /// <param name="observer">Data event observer function.</param>
        /// <returns>Newly created data event handler.</returns>
        public static IDataEventHandler CreateUpdateObserver(Func<IDataEvent, CancellationToken, ValueTask> observer)
            => CreateOperationObserver(DataOperation.Update, observer);

        /// <summary>
        /// Creates data event handler from the specified function that will be invoked only for the <c>Insert</c>
        /// operation.
        /// </summary>
        /// <param name="observer">Data event observer function.</param>
        /// <returns>Newly created data event handler.</returns>
        public static IDataEventHandler CreateInsertObserver(Func<IDataEvent, CancellationToken, ValueTask> observer)
            => CreateOperationObserver(DataOperation.Insert, observer);

        /// <summary>
        /// Creates data event handler from the specified function that will be invoked only for the <c>Delete</c>
        /// operation.
        /// </summary>
        /// <param name="observer">Data event observer function.</param>
        /// <returns>Newly created data event handler.</returns>
        public static IDataEventHandler CreateDeleteObserver(Func<IDataEvent, CancellationToken, ValueTask> observer)
            => CreateOperationObserver(DataOperation.Delete, observer);

        /// <summary>
        /// Creates data event handler from the specified function that will be invoked only for the <c>Update</c>
        /// operation being performed over the entity of the spcified type.
        /// </summary>
        /// <typeparam name="T">Type of the data entity being handled by the observer function.</typeparam>
        /// <param name="observer">Data event observer function.</param>
        /// <returns>Newly created data event handler.</returns>
        public static IDataEventHandler CreateUpdateObserver<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
            Func<DataUpdateEvent<T>, CancellationToken, ValueTask> observer)
            where T : class
            => new OperationObserver<T, DataUpdateEvent<T>>(observer);

        /// <summary>
        /// Creates data event handler from the specified function that will be invoked only for the <c>Insert</c>
        /// operation being performed over the entity of the spcified type.
        /// </summary>
        /// <typeparam name="T">Type of the data entity being handled by the observer function.</typeparam>
        /// <param name="observer">Data event observer function.</param>
        /// <returns>Newly created data event handler.</returns>
        public static IDataEventHandler CreateInsertObserver<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
            Func<DataInsertEvent<T>, CancellationToken, ValueTask> observer)
            where T : class
            => new OperationObserver<T, DataInsertEvent<T>>(observer);

        /// <summary>
        /// Creates data event handler from the specified function that will be invoked only for the <c>Delete</c>
        /// operation being performed over the entity of the spcified type.
        /// </summary>
        /// <typeparam name="T">Type of the data entity being handled by the observer function.</typeparam>
        /// <param name="observer">Data event observer function.</param>
        /// <returns>Newly created data event handler.</returns>
        public static IDataEventHandler CreateDeleteObserver<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
            Func<DataUpdateEvent<T>, CancellationToken, ValueTask> observer)
            where T : class
            => new OperationObserver<T, DataUpdateEvent<T>>(observer);

        /// <summary>
        /// Creates data event handler from the specified function.
        /// </summary>
        /// <param name="func">Data event observer function.</param>
        /// <returns>Newly created data event handler.</returns>
        public static IDataEventHandler CreateObserverFrom(Func<IServiceProvider, DataOperation, object, CancellationToken, ValueTask> func)
            => new GenericObserver((e, token) => func(e.ServiceProvider, e.Operation, e.Entity, token));

        /// <summary>
        /// Creates data event handler from the specified function that will be invoked only for the specified entity
        /// type (all operations).
        /// </summary>
        /// <param name="entityType">Type of the data entity being handled by the observer function.</param>
        /// <param name="func">Data event observer function.</param>
        /// <returns>Newly created data event handler.</returns>
        public static IDataEventHandler CreateObserverFrom(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type entityType,
            Func<IServiceProvider, DataOperation, object, CancellationToken, ValueTask> func)
            => CreateObserver(entityType, (e, token) => func(e.ServiceProvider, e.Operation, e.Entity, token));


        private sealed class CreateObserverFromTInvoker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
            where T : class
        {
            private Func<IServiceProvider, DataOperation, T, CancellationToken, ValueTask> Func { get; }

            public CreateObserverFromTInvoker(Func<IServiceProvider, DataOperation, T, CancellationToken, ValueTask> func)
                => Func = func ?? throw new ArgumentNullException(nameof(func));

            public ValueTask Invoke(IDataEvent<T> e, CancellationToken cancellationToken)
                => Func(e.ServiceProvider, e.Operation, e.Entity, cancellationToken);
        }

        /// <summary>
        /// Creates data event handler from the specified function that will be invoked only for the specified entity
        /// type (all operations).
        /// </summary>
        /// <typeparam name="T">Type of the data entity being handled by the observer function.</typeparam>
        /// <param name="func">Data event observer function.</param>
        /// <returns>Newly created data event handler.</returns>
        public static IDataEventHandler CreateObserverFrom<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
            Func<IServiceProvider, DataOperation, T, CancellationToken, ValueTask> func)
            where T : class
            => CreateObserver<T>(new CreateObserverFromTInvoker<T>(func).Invoke);

        private sealed class CreateUpdateObserverFromTInvoker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
            where T : class
        {
            private Func<IServiceProvider, T, CancellationToken, ValueTask> Func { get; }

            public CreateUpdateObserverFromTInvoker(Func<IServiceProvider, T, CancellationToken, ValueTask> func)
                => Func = func ?? throw new ArgumentNullException(nameof(func));

            public ValueTask Invoke(IDataEvent<T> e, CancellationToken cancellationToken)
                => Func(e.ServiceProvider, e.Entity, cancellationToken);
        }

        /// <summary>
        /// Creates data event handler from the specified function that will be invoked only for the specified entity
        /// type (only <c>Update</c> operation).
        /// </summary>
        /// <typeparam name="T">Type of the data entity being handled by the observer function.</typeparam>
        /// <param name="func">Data event observer function.</param>
        /// <returns>Newly created data event handler.</returns>
        public static IDataEventHandler CreateUpdateObserverFrom<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
            Func<IServiceProvider, T, CancellationToken, ValueTask> func)
            where T : class
            => CreateUpdateObserver<T>(new CreateUpdateObserverFromTInvoker<T>(func).Invoke);

        private sealed class CreateInsertObserverFromTInvoker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
            where T : class
        {
            private Func<IServiceProvider, T, CancellationToken, ValueTask> Func { get; }

            public CreateInsertObserverFromTInvoker(Func<IServiceProvider, T, CancellationToken, ValueTask> func)
                => Func = func ?? throw new ArgumentNullException(nameof(func));

            public ValueTask Invoke(IDataEvent<T> e, CancellationToken cancellationToken)
                => Func(e.ServiceProvider, e.Entity, cancellationToken);
        }

        /// <summary>
        /// Creates data event handler from the specified function that will be invoked only for the specified entity
        /// type (only <c>Insert</c> operation).
        /// </summary>
        /// <typeparam name="T">Type of the data entity being handled by the observer function.</typeparam>
        /// <param name="func">Data event observer function.</param>
        /// <returns>Newly created data event handler.</returns>
        public static IDataEventHandler CreateInsertObserverFrom<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
            Func<IServiceProvider, T, CancellationToken, ValueTask> func)
            where T : class
            => CreateInsertObserver<T>(new CreateInsertObserverFromTInvoker<T>(func).Invoke);

        private sealed class CreateDeleteObserverFromTInvoker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
            where T : class
        {
            private Func<IServiceProvider, T, CancellationToken, ValueTask> Func { get; }

            public CreateDeleteObserverFromTInvoker(Func<IServiceProvider, T, CancellationToken, ValueTask> func)
                => Func = func ?? throw new ArgumentNullException(nameof(func));

            public ValueTask Invoke(IDataEvent<T> e, CancellationToken cancellationToken)
                => Func(e.ServiceProvider, e.Entity, cancellationToken);
        }

        /// <summary>
        /// Creates data event handler from the specified function that will be invoked only for the specified entity
        /// type (only <c>Delete</c> operation).
        /// </summary>
        /// <typeparam name="T">Type of the data entity being handled by the observer function.</typeparam>
        /// <param name="func">Data event observer function.</param>
        /// <returns>Newly created data event handler.</returns>
        public static IDataEventHandler CreateDeleteObserverFrom<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
            Func<IServiceProvider, T, CancellationToken, ValueTask> func)
            where T : class
            => CreateDeleteObserver<T>(new CreateDeleteObserverFromTInvoker<T>(func).Invoke);

    }
}