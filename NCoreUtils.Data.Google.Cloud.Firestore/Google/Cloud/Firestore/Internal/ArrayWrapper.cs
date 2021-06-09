using System;
using System.Collections.Generic;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal
{
    public class ArrayWrapper<T> : CollectionWrapper
    {
        public T[] Source { get; }

        public override int Count => Source.Length;

        public ArrayWrapper(T[] source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public override void SplitIntoChunks(int chunkSize, List<object> results)
        {
            for (var offset = 0; offset < Source.Length; offset += chunkSize)
            {
                var size = Math.Min(Source.Length - offset, chunkSize);
                var chunk = new T[size];
                Array.Copy(Source, offset, chunk, 0, size);
                results.Add(chunk);
            }
        }
    }
}