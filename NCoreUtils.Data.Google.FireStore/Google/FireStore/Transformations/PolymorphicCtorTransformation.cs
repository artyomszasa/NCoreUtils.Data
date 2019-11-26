using System;
using System.Collections.Generic;
using System.Linq;

namespace NCoreUtils.Data.Google.FireStore.Transformations
{
    public abstract class PolymorphicCtorTransformation : ITransformation
    {
        IEnumerable<IValueSource> ITransformation.Sources => Derivates.Values.SelectMany(v => v.Sources).Distinct();

        public IValueSource<string> TypeExtractor { get; }

        public IReadOnlyDictionary<string, ITransformation> Derivates { get; }

        public abstract Type Type { get; }

        protected PolymorphicCtorTransformation(IValueSource<string> typeExtractor, IReadOnlyDictionary<string, ITransformation> derivates)
        {
            TypeExtractor = typeExtractor;
            Derivates = derivates;
        }

        public object GetValue(object instance)
        {
            var type = TypeExtractor.GetValue(instance);
            if (!Derivates.TryGetValue(type, out var transformation))
            {
                throw new InvalidOperationException($"Invalid discriminator {type}.");
            }
            return transformation.GetValue(instance);
        }
    }
}