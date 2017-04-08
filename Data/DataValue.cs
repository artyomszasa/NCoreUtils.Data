using System.Collections.Generic;
using NCoreUtils.Collections;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Partial data that consists of a single key and value.
    /// </summary>
    public abstract class DataValue<T> : IPartialData
    {
        /// <summary>
        /// The value.
        /// </summary>
        public T Value { get; private set; }
        /// <summary>
        /// The key.
        /// </summary>
        public CaseInsensitive Key { get; private set; }
        /// <summary>
        /// Initializes new instance of <see cref="T:NCoreUtils.Data.DataValue`1" />.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="dataKey">Key.</param>
        public DataValue(T value, CaseInsensitive dataKey)
        {
            Value = value;
            Key = dataKey;
        }
        /// <summary>
        /// Returns the single key-value pair represented by the instance.
        /// </summary>
        /// <returns>Sequence containing single value.</returns>
        public IEnumerable<KeyValuePair<CaseInsensitive, object>> GetRawData()
        {
            yield return KeyValuePair.Create<CaseInsensitive, object>(Key, Value);
        }
        /// <summary>
        /// Returns value of the actual instance if the specified key equals to the one stored in the actual instance.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Variable to return the value to.</param>
        /// <returns>
        /// <c>true</c> if value has been found, <c>false</c> otherwise.
        /// </returns>
        public bool TryGetValue(CaseInsensitive key, out object value)
        {
            if (Key == key)
            {
                value = Value;
                return true;
            }
            value = null;
            return false;
        }
    }
}