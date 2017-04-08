using System;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Provides composite data functionality.
    /// </summary>
    public interface ICompositeData : IPartialData
    {
        /// <summary>
        /// Returns typed partial data if present.
        /// </summary>
        /// <param name="dataType">Partial data type.</param>
        /// <param name="data">Variable to return partial data.</param>
        /// <returns>
        /// <c>true</c> if partial data of the specified type was present, <c>false</c> otherwise.
        /// </returns>
        bool TryGetPartialData(Type dataType, out IPartialData data);
    }
}