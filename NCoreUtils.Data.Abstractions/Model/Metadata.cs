using System;
using System.Collections;
using System.Collections.Generic;

namespace NCoreUtils.Data.Model
{
    public class Metadata : IReadOnlyDictionary<string, object?>
    {
        public IReadOnlyDictionary<string, object?> Data { get; }

        public IEnumerable<string> Keys => Data.Keys;

        public IEnumerable<object?> Values => Data.Values;

        public int Count => Data.Count;

        public object? this[string key] => Data[key];

        public Metadata(IReadOnlyDictionary<string, object?> data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public bool ContainsKey(string key)
            => Data.ContainsKey(key);

        public bool TryGetValue(string key, out object? value)
            => Data.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
            => Data.GetEnumerator();
    }
}