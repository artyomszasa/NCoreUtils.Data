using System.Collections.Generic;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Represents functionality which can be composed into composite data.
    /// </summary>
    public interface IPartialData
    {
        /// <summary>
        /// Searches for the specified data key and if found returns value associated.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Variable to return the value to.</param>
        /// <returns>
        /// <c>true</c> if value has been found, <c>false</c> otherwise.
        /// </returns>
        bool TryGetValue(CaseInsensitive key, out object value);
        /// <summary>
        /// Returns all data represented by the actual instance as key-value pairs.
        /// </summary>
        /// <returns>
        /// All data represented by the actual instance as key-value pairs.
        /// </returns>
        IEnumerable<KeyValuePair<CaseInsensitive, object>> GetRawData();
    }
}