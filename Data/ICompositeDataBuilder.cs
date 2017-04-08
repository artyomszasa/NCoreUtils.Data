using System;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Provides sufficient functionality to build composite data.
    /// </summary>
    public interface ICompositeDataBuilder
    {
        /// <summary>
        /// Adds partial data factory to the builder if not present.
        /// </summary>
        /// <param name="dataType">Partial data type.</param>
        /// <param name="factory">Partial data factory.</param>
        /// <returns>
        /// <c>true</c> if partial data has been added, <c>false</c> otherwise.
        /// </returns>
        bool TryAdd(Type dataType, Func<ICompositeData, IPartialData> factory);
        /// <summary>
        /// Replaces partial data factory to the builder if present.
        /// </summary>
        /// <param name="dataType">Partial data type.</param>
        /// <param name="factory">Partial data factory.</param>
        /// <returns>
        /// <c>true</c> if partial data has been replaced, <c>false</c> otherwise.
        /// </returns>
        bool TryReplace(Type dataType, Func<ICompositeData, IPartialData> factory);
    }
}