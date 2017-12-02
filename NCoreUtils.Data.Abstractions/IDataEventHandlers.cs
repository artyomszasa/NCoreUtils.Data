using System.Collections.Immutable;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Defines functionality to access and manipulate data being processed by data repositories. All method
    /// implementation must be thread-safe.
    /// </summary>
    public interface IDataEventHandlers
    {
        /// <summary>
        /// Gets overall count of data event handlers.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets all data event handlers as immutable list.
        /// </summary>
        ImmutableList<IDataEventHandler> Handlers { get; }

        /// <summary>
        /// Gets ot sets handler at specified index.
        /// </summary>
        IDataEventHandler this[int index] { get; set; }

        /// <summary>
        /// Inserts handler as last item.
        /// </summary>
        /// <param name="handler">Handler to insert.</param>
        void Add(IDataEventHandler handler);

        /// <summary>
        /// Inserts handler at specified index.
        /// </summary>
        /// <param name="index">Index to insert handler at.</param>
        /// <param name="handler">Handler to insert.</param>
        void Insert(int index, IDataEventHandler handler);

        /// <summary>
        /// Removes specified handler.
        /// </summary>
        /// <param name="handler">Handler to remove.</param>
        /// <returns>
        /// <c>true</c> if handler was present and has been removed, <c>false</c> otherwise.
        /// </returns>
        bool Remove(IDataEventHandler handler);

        /// <summary>
        /// Removes handler at specified index.
        /// </summary>
        /// <param name="index">Index to remove handler at.</param>
        void RemoveAt(int index);
    }
}