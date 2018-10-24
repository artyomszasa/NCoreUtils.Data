using System.Linq;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Defines functionality to access direct object query (i.e. bypassing any repository defined decorations).
    /// </summary>
    /// <typeparam name="T">Data entity type.</typeparam>
    public interface IDirectDataRepository<T>
    {
        /// <summary>
        /// Gets or creates queryable object without any repository defined decorations.
        /// </summary>
        /// <returns>Queryable object.</returns>
        IQueryable<T> GetDirectQuery();
    }
}