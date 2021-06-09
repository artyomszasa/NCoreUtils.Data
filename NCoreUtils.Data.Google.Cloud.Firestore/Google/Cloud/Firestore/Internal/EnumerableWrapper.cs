using System;
using System.Collections.Generic;
using System.Linq;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal
{
    public class EnumerableWrapper<T> : CollectionWrapper
    {
        private IReadOnlyList<T>? _values;

        public IEnumerable<T> Source { get; }

        public override int Count => Source switch
        {
            ICollection<T> collection => collection.Count,
            IReadOnlyCollection<T> collection => collection.Count,
            _ => Cached().Count
        };

        public EnumerableWrapper(IEnumerable<T> source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        private IReadOnlyList<T> Cached()
        {
            _values ??= Source.ToList();
            return _values;
        }

        public override void SplitIntoChunks(int chunkSize, List<object> results)
        {
            var cached = Cached();
            for (var offset = 0; offset < cached.Count; offset += chunkSize)
            {
                var size = Math.Min(cached.Count - offset, chunkSize);
                var chunk = new T[size];
                for (var i = 0; i < size; ++i)
                {
                    chunk[i] = cached[i + offset];
                }
                results.Add(chunk);
            }
        }
    }
}