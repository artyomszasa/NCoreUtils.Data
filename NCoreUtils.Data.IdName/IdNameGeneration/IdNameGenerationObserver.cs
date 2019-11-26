using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Data.Events;

namespace NCoreUtils.Data.IdNameGeneration
{
    [ImplicitDataEventObserver]
    public class IdNameGenerationObserver : IDataEventHandler
    {
        abstract class Invoker
        {
            static readonly ConcurrentDictionary<Type, Invoker> _cache = new ConcurrentDictionary<Type, Invoker>();

            static readonly Func<Type, Invoker> _factory = type => (Invoker)Activator.CreateInstance(typeof(Invoker<>).MakeGenericType(type), true);

            protected abstract ValueTask DoInvoke(IdNameGenerationObserver observer, IDataEvent dataEvent, CancellationToken cancellationToken);

            public static ValueTask Invoke(IdNameGenerationObserver observer, IDataEvent dataEvent, CancellationToken cancellationToken)
                => _cache.GetOrAdd(dataEvent.EntityType, _factory).DoInvoke(observer, dataEvent, cancellationToken);
        }

        sealed class Invoker<T> : Invoker
            where T : class, IHasIdName
        {
            protected override ValueTask DoInvoke(IdNameGenerationObserver observer, IDataEvent dataEvent, CancellationToken cancellationToken)
            {
                return observer.HandleAsync((IDataEvent<T>)dataEvent, cancellationToken);
            }
        }

        public async ValueTask HandleAsync<T>(IDataEvent<T> @event, CancellationToken cancellationToken)
            where T : class, IHasIdName
        {
            if (@event.Repository is ISupportsIdNameGeneration generationInfo && generationInfo.GenerateIdNameOnInsert)
            {
                @event.Entity.IdName = await @event.Repository.Items.GenerateIdNameAsync(
                    serviceProvider: @event.ServiceProvider,
                    idNameDescription: generationInfo.IdNameDescription,
                    entity: @event.Entity,
                    cancellationToken: cancellationToken);
            }
        }

        public ValueTask HandleAsync(IDataEvent @event, CancellationToken cancellationToken = default)
        {
            if (DataOperation.Insert == @event.Operation)
            {
                if (@event.Entity is IHasIdName)
                {
                    return Invoker.Invoke(this, @event, cancellationToken);
                }
            }
            return default;
        }
    }
}