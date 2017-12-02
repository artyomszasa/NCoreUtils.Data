namespace NCoreUtils.Data
{
    /// <summary>
    /// Defines functionality to retrieve business key of the data entity.
    /// </summary>
    public interface IHasId<T>
    {
        /// <summary>
        /// Gets business key of the actual data entity instance.
        /// </summary>
        T Id { get; }
    }
}