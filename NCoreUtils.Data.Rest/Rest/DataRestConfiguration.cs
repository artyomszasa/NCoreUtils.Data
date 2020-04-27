using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NCoreUtils.Rest;

namespace NCoreUtils.Data.Rest
{
    public class DataRestConfiguration : IReadOnlyDictionary<Type, (Type IdType, IRestClientConfiguration Configuration)>
    {
        readonly IReadOnlyDictionary<Type, (Type IdType, IRestClientConfiguration Configuration)> _source;

        public (Type IdType, IRestClientConfiguration Configuration) this[Type key] => _source[key];

        public IEnumerable<Type> Keys => _source.Keys;

        public IEnumerable<(Type IdType, IRestClientConfiguration Configuration)> Values => _source.Values;

        public int Count => _source.Count;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal DataRestConfiguration(DataRestConfigurationBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            _source = builder.ToDictionary(e => e.EntityType, e => (e.IdType, e.Configuration));
        }

        public bool ContainsKey(Type key)
            => _source.ContainsKey(key);

        public IEnumerator<KeyValuePair<Type, (Type IdType, IRestClientConfiguration Configuration)>> GetEnumerator()
            => _source.GetEnumerator();

        public bool TryGetValue(Type key, out (Type IdType, IRestClientConfiguration Configuration) value)
            => _source.TryGetValue(key, out value);
    }
}