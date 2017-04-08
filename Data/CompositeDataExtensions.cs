namespace NCoreUtils.Data
{
    /// <summary>
    /// Contains extensions for <see cref="T:NCoreUtils.Data.ICompositeData" />.
    /// </summary>
    public static class CompositeDataExtensions
    {
        /// <summary>
        /// Returns typed partial data if present.
        /// </summary>
        /// <param name="dataSource">Composite data source.</param>
        /// <param name="data">Variable to return partial data.</param>
        /// <returns>
        /// <c>true</c> if partial data of the specified type was present, <c>false</c> otherwise.
        /// </returns>
        public static bool TryGetPartialData<TPartialData>(this ICompositeData dataSource, out TPartialData data) where TPartialData : IPartialData
        {
            if (dataSource.TryGetPartialData(typeof(TPartialData), out var obj))
            {
                data = (TPartialData)obj;
                return true;
            }
            data = default(TPartialData);
            return false;
        }
    }
}