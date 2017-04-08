namespace NCoreUtils.Data
{
    /// <summary>
    /// Partial data that consists of a single key and boolean value.
    /// </summary>
    public abstract class DataFlag : DataValue<bool>
    {
        /// <summary>
        /// Initializes new instance of <see cref="T:NCoreUtils.Data.DataFlag" />.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="dataKey">Key.</param>
        public DataFlag(bool value, CaseInsensitive dataKey)
            : base(value, dataKey)
        { }
    }
}