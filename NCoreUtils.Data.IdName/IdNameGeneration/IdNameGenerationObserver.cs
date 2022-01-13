using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data.Events;

namespace NCoreUtils.Data.IdNameGeneration
{
    [ImplicitDataEventObserver]
    public class IdNameGenerationObserver : IDataEventHandler
    {
        private abstract class Invoker
        {
            private static readonly ConcurrentDictionary<Type, Invoker> _cache = new();

            private static readonly Func<Type, Invoker> _factory = DoCreate;

            [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Both type and type parameter are preserved.")]
            private static Invoker DoCreate([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
                => (Invoker)Activator.CreateInstance(typeof(Invoker<>).MakeGenericType(type), true)!;

            protected abstract ValueTask DoInvoke(IdNameGenerationObserver observer, IDataEvent dataEvent, CancellationToken cancellationToken);

            public static ValueTask Invoke(IdNameGenerationObserver observer, IDataEvent dataEvent, CancellationToken cancellationToken)
                => _cache.GetOrAdd(dataEvent.EntityType, _factory).DoInvoke(observer, dataEvent, cancellationToken);
        }

        private sealed class Invoker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T> : Invoker
            where T : class, IHasIdName
        {
            protected override ValueTask DoInvoke(IdNameGenerationObserver observer, IDataEvent dataEvent, CancellationToken cancellationToken)
            {
                return observer.HandleAsync((IDataEvent<T>)dataEvent, cancellationToken);
            }
        }

        [SuppressMessage("Performance", "CA1822", Justification = "Potential inteface implementation/virtual method.")]
        public async ValueTask HandleAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
            IDataEvent<T> @event,
            CancellationToken cancellationToken)
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