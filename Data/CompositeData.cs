using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Default composite data implementation.
    /// </summary>
    public class CompositeData : ICompositeData
    {
        static Lazy<T> L<T>(Func<T> factory) => new Lazy<T>(factory);
        readonly object _lock = new object();
        readonly Lazy<ImmutableDictionary<CaseInsensitive, object>> _allValues;
        readonly ImmutableDictionary<Type, Func<ICompositeData, IPartialData>> _factories;
        readonly ConcurrentDictionary<Type, IPartialData> _instances = new ConcurrentDictionary<Type, IPartialData>();
        /// <summary>
        /// Initializes new instance of <see cref="T:NCoreUtils.Data.CompositeData" />.
        /// </summary>
        /// <param name="factories">Partial data factories.</param>
        public CompositeData(ImmutableDictionary<Type, Func<ICompositeData, IPartialData>> factories)
        {
            RuntimeAssert.ArgumentNotNull(factories, nameof(factories));
            _factories = factories;
            _allValues = L(() => GetRawData().ToImmutableDictionary());
        }
        /// <summary>
        /// Initializes new instance of <see cref="T:NCoreUtils.Data.CompositeData" />.
        /// </summary>
        /// <param name="factories">Partial data factories.</param>
        public CompositeData(IEnumerable<KeyValuePair<Type, Func<ICompositeData, IPartialData>>> factories)
            : this(factories.ToImmutableDictionary())
        { }
        bool TryGetOrCreateInstance(Type type, out IPartialData @object)
        {
            if (_instances.TryGetValue(type, out var instance))
            {
                @object = instance;
                return true;
            }
            lock (_lock)
            {
                if (_instances.TryGetValue(type, out var inst))
                {
                    @object = inst;
                    return true;
                }
                if (_factories.TryGetValue(type, out var factory))
                {
                    var newInstance = factory(this);
                    _instances[type] = newInstance;
                    @object = newInstance;
                    return true;
                }
            }
            @object = null;
            return false;
        }
        /// <summary>
        /// Returns all data represented by the actual instance as key-value pairs.
        /// </summary>
        /// <returns>
        /// All data represented by the actual instance as key-value pairs.
        /// </returns>
        public IEnumerable<KeyValuePair<CaseInsensitive, object>> GetRawData()
        {
            foreach (var type in _factories.Keys)
            {
                if (TryGetOrCreateInstance(type, out var partialData))
                {
                    foreach (var kv in partialData.GetRawData())
                    {
                        yield return kv;
                    }
                }
            }
        }
        /// <summary>
        /// Returns typed partial data if present.
        /// </summary>
        /// <param name="dataType">Partial data type.</param>
        /// <param name="data">Variable to return partial data.</param>
        /// <returns>
        /// <c>true</c> if partial data of the specified type was present, <c>false</c> otherwise.
        /// </returns>
        public bool TryGetPartialData(Type dataType, out IPartialData data)
            => TryGetOrCreateInstance(dataType, out data);
        /// <summary>
        /// Searches for the specified data key and if found returns value associated.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Variable to return the value to.</param>
        /// <returns>
        /// <c>true</c> if value has been found, <c>false</c> otherwise.
        /// </returns>
        public bool TryGetValue(CaseInsensitive key, out object value)
            => _allValues.Value.TryGetValue(key, out value);
    }
}