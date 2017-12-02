namespace NCoreUtils.Data
{
    /// <summary>
    /// Specifies which data operation is being performed.
    /// </summary>
    public enum DataOperation
    {
        /// Data entity is being inserted into data repository.
        Insert = 0,
        /// Data entity is being update in data repository.
        Update = 1,
        /// Data entity is being deleted from data repository.
        Delete = 2
    }
}