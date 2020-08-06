using System.Collections.Immutable;
using System.Threading;

namespace NCoreUtils.Data
{
    internal sealed class DataEventHandlers : IDataEventHandlers
    {
        ImmutableList<IDataEventHandler> _handlers = ImmutableList<IDataEventHandler>.Empty;

        public IDataEventHandler this[int index]
        {
            get => _handlers[index];
            set
            {
                bool success;
                do
                {
                    var handlers = _handlers;
                    var newHandlers = handlers.SetItem(index, value);
                    success = handlers == Interlocked.CompareExchange(ref _handlers, newHandlers, handlers);
                }
                while (!success);
            }
        }

        public int Count => _handlers.Count;

        public ImmutableList<IDataEventHandler> Handlers => _handlers;

        public void Add(IDataEventHandler handler)
        {
            bool success;
            do
            {
                var handlers = _handlers;
                var newHandlers = handlers.Add(handler);
                success = handlers == Interlocked.CompareExchange(ref _handlers, newHandlers, handlers);
            }
            while (!success);
        }

        public void Insert(int index, IDataEventHandler handler)
        {
            bool success;
            do
            {
                var handlers = _handlers;
                var newHandlers = handlers.Insert(index, handler);
                success = handlers == Interlocked.CompareExchange(ref _handlers, newHandlers, handlers);
            }
            while (!success);
        }

        public bool Remove(IDataEventHandler handler)
        {
            bool success;
            bool removed;
            do
            {
                var handlers = _handlers;
                var newHandlers = handlers.Remove(handler);
                removed = handlers.Count != newHandlers.Count;
                success = handlers == Interlocked.CompareExchange(ref _handlers, newHandlers, handlers);
            }
            while (!success);
            return removed;
        }

        public void RemoveAt(int index)
        {
            bool success;
            do
            {
                var handlers = _handlers;
                var newHandlers = handlers.RemoveAt(index);
                success = handlers == Interlocked.CompareExchange(ref _handlers, newHandlers, handlers);
            }
            while (!success);
        }
    }
}