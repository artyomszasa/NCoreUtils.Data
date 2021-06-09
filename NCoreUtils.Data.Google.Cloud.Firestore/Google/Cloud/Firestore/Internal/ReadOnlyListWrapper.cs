using System;
using System.Collections.Generic;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal
{
    public class ReadOnlyListWrapper<T> : CollectionWrapper
    {
        public IReadOnlyList<T> Source { get; }

        public override int Count => Source.Count;

        public ReadOnlyListWrapper(IReadOnlyList<T> source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public override void SplitIntoChunks(int chunkSize, List<object> results)
        {
            for (var offset = 0; offset < Source.Count; offset += chunkSize)
            {
                var size = Math.Min(Source.Count - offset, chunkSize);
                var chunk = new T[size];
                for (var i = 0; i < size; ++i)
                {
                    chunk[i] = Source[i + offset];
                }
                results.Add(chunk);
            }
        }
    }
}