using System;
using System.Collections.Concurrent;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Default implementation of composite data builder.
    /// </summary>
    public class CompositeDataBuilder : ICompositeDataBuilder
    {
        /// <summary>
        /// Concurrent dictionary containing partial data factories added to the actual instance.
        /// </summary>
        /// <returns></returns>
        protected ConcurrentDictionary<Type, Func<ICompositeData, IPartialData>> Factories { get; } = new ConcurrentDictionary<Type, Func<ICompositeData, IPartialData>>();
        /// <summary>
        /// Adds partial data factory to the builder if not present.
        /// </summary>
        /// <param name="dataType">Partial data type.</param>
        /// <param name="factory">Partial data factory.</param>
        /// <returns>
        /// <c>true</c> if partial data has been added, <c>false</c> otherwise.
        /// </returns>
        public bool TryAdd(Type dataType, Func<ICompositeData, IPartialData> factory)
            => Factories.TryAdd(dataType, factory);
        /// <summary>
        /// Replaces partial data factory to the builder if present.
        /// </summary>
        /// <param name="dataType">Partial data type.</param>
        /// <param name="factory">Partial data factory.</param>
        /// <returns>
        /// <c>true</c> if partial data has been replaced, <c>false</c> otherwise.
        /// </returns>
        public bool TryReplace(Type dataType, Func<ICompositeData, IPartialData> factory)
            => Factories.TryGetValue(dataType, out var f) && Factories.TryUpdate(dataType, factory, f);
    }
}