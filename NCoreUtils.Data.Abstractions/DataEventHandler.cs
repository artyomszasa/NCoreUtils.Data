using System;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data.Events;

namespace NCoreUtils.Data
{
    public static class DataEventHandler
    {
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
            public Func<IDataEvent, CancellationToken, Task> Observer { get; }

            public GenericObserver(Func<IDataEvent, CancellationToken, Task> observer) => Observer = observer;

            public Task HandleAsync(IDataEvent @event, CancellationToken cancellationToken = default(CancellationToken))
                => Observer(@event, cancellationToken);
        }

        sealed class TypeObserver : IDataEventHandler
        {
            public TargetOperation ObservedOperations { get; }

            public Type ObservedType { get; }

            public Func<IDataEvent, CancellationToken, Task> Observer { get; }

            public TypeObserver(Type observedType, TargetOperation observedOperations, Func<IDataEvent, CancellationToken, Task> observer)
            {
                ObservedOperations = observedOperations;
                ObservedType = observedType;
                Observer = observer;
            }

            public Task HandleAsync(IDataEvent @event, CancellationToken cancellationToken = default(CancellationToken))
            {
                if (ObservedOperations.HasFlag(@event.Operation) && @event.EntityType.Equals(ObservedType))
                {
                   return Observer(@event, cancellationToken);
                }
                return Task.CompletedTask;
            }
        }

        sealed class TypeObserver<T> : IDataEventHandler
            where T : class
        {
            public TargetOperation ObservedOperations { get; }

            public Func<IDataEvent<T>, CancellationToken, Task> Observer { get; }

            public TypeObserver(TargetOperation observedOperations, Func<IDataEvent<T>, CancellationToken, Task> observer)
            {
                ObservedOperations = observedOperations;
                Observer = observer;
            }

            public Task HandleAsync(IDataEvent @event, CancellationToken cancellationToken = default(CancellationToken))
            {
                if (ObservedOperations.HasFlag(@event.Operation) && @event is IDataEvent<T> e)
                {
                   return Observer(e, cancellationToken);
                }
                return Task.CompletedTask;
            }
        }

        sealed class OperationObserver : IDataEventHandler
        {
            public DataOperation Operation { get; }

            public Func<IDataEvent, CancellationToken, Task> Observer { get; }

            public OperationObserver(DataOperation operation, Func<IDataEvent, CancellationToken, Task> observer)
            {
                Operation = operation;
                Observer = observer;
            }

            public Task HandleAsync(IDataEvent @event, CancellationToken cancellationToken = default(CancellationToken))
            {
                if (@event.Operation == Operation)
                {
                   return Observer(@event, cancellationToken);
                }
                return Task.CompletedTask;
            }
        }

        sealed class OperationObserver<TData, TEvent> : IDataEventHandler
            where TData : class
            where TEvent : IDataEvent
        {
            public Func<TEvent, CancellationToken, Task> Observer { get; }

            public OperationObserver(Func<TEvent, CancellationToken, Task> observer)
            {
                Observer = observer;
            }

            public Task HandleAsync(IDataEvent @event, CancellationToken cancellationToken = default(CancellationToken))
            {
                if (@event is TEvent e)
                {
                   return Observer(e, cancellationToken);
                }
                return Task.CompletedTask;
            }
        }

        public static IDataEventHandler Create(Func<IDataEvent, CancellationToken, Task> observer)
            => new GenericObserver(observer);

        public static IDataEventHandler CreateObserver(Type entityType, TargetOperation operations, Func<IDataEvent, CancellationToken, Task> observer)
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

        public static IDataEventHandler CreateObserver(Type entityType, Func<IDataEvent, CancellationToken, Task> observer)
            => CreateObserver(entityType, TargetOperation.Insert | TargetOperation.Update | TargetOperation.Delete, observer);

        public static IDataEventHandler CreateObserver<T>(TargetOperation operations, Func<IDataEvent<T>, CancellationToken, Task> observer)
            where T : class
        {
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }
            return new TypeObserver<T>(operations, observer);
        }

        public static IDataEventHandler CreateObserver<T>(Func<IDataEvent<T>, CancellationToken, Task> observer)
            where T : class
            => CreateObserver(TargetOperation.Insert | TargetOperation.Update | TargetOperation.Delete, observer);

        public static IDataEventHandler CreateOperationObserver(DataOperation operation, Func<IDataEvent, CancellationToken, Task> observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }
            return new OperationObserver(operation, observer);
        }

        public static IDataEventHandler CreateUpdateObserver(Func<IDataEvent, CancellationToken, Task> observer)
            => CreateOperationObserver(DataOperation.Update, observer);

        public static IDataEventHandler CreateInsertObserver(Func<IDataEvent, CancellationToken, Task> observer)
            => CreateOperationObserver(DataOperation.Insert, observer);

        public static IDataEventHandler CreateDeleteObserver(Func<IDataEvent, CancellationToken, Task> observer)
            => CreateOperationObserver(DataOperation.Delete, observer);

        public static IDataEventHandler CreateUpdateObserver<T>(Func<DataUpdateEvent<T>, CancellationToken, Task> observer)
            where T : class
            => new OperationObserver<T, DataUpdateEvent<T>>(observer);

        public static IDataEventHandler CreateInsertObserver<T>(Func<DataUpdateEvent<T>, CancellationToken, Task> observer)
            where T : class
            => new OperationObserver<T, DataUpdateEvent<T>>(observer);

        public static IDataEventHandler CreateDeleteObserver<T>(Func<DataUpdateEvent<T>, CancellationToken, Task> observer)
            where T : class
            => new OperationObserver<T, DataUpdateEvent<T>>(observer);

        public static IDataEventHandler CreateObserverFrom(Func<IServiceProvider, DataOperation, object, CancellationToken, Task> func)
            => new GenericObserver((e, token) => func(e.ServiceProvider, e.Operation, e.Entity, token));

        public static IDataEventHandler CreateObserverFrom(Type entityType, Func<IServiceProvider, DataOperation, object, CancellationToken, Task> func)
            => CreateObserver(entityType, (e, token) => func(e.ServiceProvider, e.Operation, e.Entity, token));

        public static IDataEventHandler CreateObserverFrom<T>(Func<IServiceProvider, DataOperation, T, CancellationToken, Task> func)
            where T : class
            => CreateObserver<T>((e, token) => func(e.ServiceProvider, e.Operation, e.Entity, token));

        public static IDataEventHandler CreateUpdateObserverFrom<T>(Func<IServiceProvider, T, CancellationToken, Task> func)
            where T : class
            => CreateUpdateObserver<T>((e, token) => func(e.ServiceProvider, e.Entity, token));

        public static IDataEventHandler CreateInsertObserverFrom<T>(Func<IServiceProvider, T, CancellationToken, Task> func)
            where T : class
            => CreateInsertObserver<T>((e, token) => func(e.ServiceProvider, e.Entity, token));

        public static IDataEventHandler CreateDeleteObserverFrom<T>(Func<IServiceProvider, T, CancellationToken, Task> func)
            where T : class
            => CreateDeleteObserver<T>((e, token) => func(e.ServiceProvider, e.Entity, token));

    }
}